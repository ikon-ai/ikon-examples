import { memo, useEffect, useReducer, useRef, type CSSProperties } from 'react';
import { type IkonUiComponentResolver, type UiComponentRendererProps, useUiNode } from '@ikonai/sdk-react-ui';
import { appMessaging, type AppMessageType } from '@ikonai/sdk';
import {
  PROBE_PING_OPCODE,
  toProtocolMessageProbePing,
  fromProtocolMessageProbePing,
  type ProbePing,
} from '../../generated/protocol/probe-ping';
import {
  PROBE_PING_UNRELIABLE_OPCODE,
  toProtocolMessageProbePingUnreliable,
  fromProtocolMessageProbePingUnreliable,
  type ProbePingUnreliable,
} from '../../generated/protocol/probe-ping-unreliable';

// The same rate the server streams at, so the two directions read alike.
const SEND_INTERVAL_MS = 100;

type Mode = 'reliable' | 'unreliable';

const MESSAGES: Record<Mode, AppMessageType<ProbePing> | AppMessageType<ProbePingUnreliable>> = {
  reliable: {
    opcode: PROBE_PING_OPCODE,
    toProtocolMessage: toProtocolMessageProbePing,
    fromProtocolMessage: fromProtocolMessageProbePing,
  },
  unreliable: {
    opcode: PROBE_PING_UNRELIABLE_OPCODE,
    toProtocolMessage: toProtocolMessageProbePingUnreliable,
    fromProtocolMessage: fromProtocolMessageProbePingUnreliable,
  },
};

interface ReceiveMetrics {
  streamId: string;
  received: number;
  gaps: number;
  outOfOrder: number;
  malformed: number;
  lastSeq: number;
  viaDataChannel: number;
}

function emptyMetrics(streamId = ''): ReceiveMetrics {
  return { streamId, received: 0, gaps: 0, outOfOrder: 0, malformed: 0, lastSeq: 0, viaDataChannel: 0 };
}

// A new stream id is a new stream, which is how a restart is told apart from reordering even when
// its first message was lost. Returns the metrics to keep, which are fresh for a new stream.
function applyMessage(m: ReceiveMetrics, p: ProbePing, mode: Mode, viaDataChannel: boolean): ReceiveMetrics {
  if (p.Origin !== 'server' || p.Mode !== mode || Number(p.SentAtMs) <= 0 || !p.Note) {
    m.malformed += 1;
    return m;
  }

  if (p.Note !== m.streamId) {
    m = { ...emptyMetrics(p.Note), malformed: m.malformed };
  }

  const seq = Number(p.Seq);

  if (m.lastSeq !== 0 && seq > m.lastSeq + 1) {
    m.gaps += seq - m.lastSeq - 1;
  }

  if (m.lastSeq !== 0 && seq <= m.lastSeq) {
    m.outOfOrder += 1;
  }

  m.lastSeq = Math.max(m.lastSeq, seq);
  m.received += 1;

  if (viaDataChannel) {
    m.viaDataChannel += 1;
  }

  return m;
}

const TpProbeRenderer = memo(function TpProbeRenderer({ nodeId, context }: UiComponentRendererProps) {
  const node = useUiNode(context.store, nodeId);
  const client = context.client;
  const role = node?.props?.['role'] === 'send' ? 'send' : 'receive';
  const mode: Mode = node?.props?.['mode'] === 'unreliable' ? 'unreliable' : 'reliable';
  const running = node?.props?.['running'] === true;

  const metricsRef = useRef<ReceiveMetrics>(emptyMetrics());
  const sentRef = useRef(0);
  const seqRef = useRef(0);
  const [, repaint] = useReducer((tick: number) => tick + 1, 0);

  useEffect(() => {
    if (!client || role !== 'receive') {
      return;
    }

    const subscription = appMessaging(client).on(MESSAGES[mode], (p, _senderId, delivery) => {
      metricsRef.current = applyMessage(metricsRef.current, p, mode, delivery.viaDataChannel);
      repaint();
    });

    return () => subscription.close();
  }, [client, role, mode]);

  useEffect(() => {
    if (!client || role !== 'send' || !running) {
      return;
    }

    const messaging = appMessaging(client);
    const streamId = crypto.randomUUID();
    seqRef.current = 0;
    sentRef.current = 0;
    repaint();

    const timer = setInterval(() => {
      seqRef.current += 1;
      messaging.send(MESSAGES[mode], {
        Seq: BigInt(seqRef.current),
        SentAtMs: BigInt(Date.now()),
        Origin: 'client',
        Mode: mode,
        Note: streamId,
      });
      sentRef.current += 1;
      repaint();
    }, SEND_INTERVAL_MS);

    return () => clearInterval(timer);
  }, [client, role, mode, running]);

  if (role === 'send') {
    return (
      <span style={rowStyle}>
        <Stat label="sent" value={sentRef.current} testid={`tp-c2s-${mode}-sent`} />
      </span>
    );
  }

  const m = metricsRef.current;

  return (
    <span style={rowStyle}>
      <Stat label="received" value={m.received} testid={`tp-s2c-${mode}-received`} />
      <Stat label="gaps" value={m.gaps} testid={`tp-s2c-${mode}-gaps`} />
      <Stat label="out of order" value={m.outOfOrder} testid={`tp-s2c-${mode}-ooo`} />
      <Stat label="malformed" value={m.malformed} testid={`tp-s2c-${mode}-malformed`} />
      <Stat label="data channel" value={m.viaDataChannel} testid={`tp-s2c-${mode}-datachannel`} />
    </span>
  );
});

function Stat({ label, value, testid }: { label: string; value: number; testid: string }) {
  return (
    <span style={statStyle}>
      <span style={{ opacity: 0.7 }}>{label}</span>
      <span style={{ fontVariantNumeric: 'tabular-nums', fontWeight: 600 }} data-testid={testid}>
        {value}
      </span>
    </span>
  );
}

const rowStyle: CSSProperties = { display: 'inline-flex', columnGap: 20, rowGap: 4, flexWrap: 'wrap', alignItems: 'baseline' };
const statStyle: CSSProperties = { display: 'inline-flex', gap: 6, alignItems: 'baseline' };

export function createTpProbeResolver(): IkonUiComponentResolver {
  return (initialNode) => (initialNode.type !== 'tp-probe' ? undefined : TpProbeRenderer);
}
