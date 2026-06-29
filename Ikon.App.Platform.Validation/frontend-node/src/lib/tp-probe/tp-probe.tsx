import { memo, useEffect, useRef, useState, type CSSProperties } from 'react';
import { type IkonUiComponentResolver, type UiComponentRendererProps } from '@ikonai/sdk-react-ui';
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

// Ties each app-local opcode to its generated encode/decode pair. The decode/encode
// run entirely on the raw GROUP_APP_LOCAL transport — this component never touches
// the reactive UI store, which is the whole point of a custom .tp surface.
const ProbePingMessage: AppMessageType<ProbePing> = {
  opcode: PROBE_PING_OPCODE,
  toProtocolMessage: toProtocolMessageProbePing,
  fromProtocolMessage: fromProtocolMessageProbePing,
};

const ProbePingUnreliableMessage: AppMessageType<ProbePingUnreliable> = {
  opcode: PROBE_PING_UNRELIABLE_OPCODE,
  toProtocolMessage: toProtocolMessageProbePingUnreliable,
  fromProtocolMessage: fromProtocolMessageProbePingUnreliable,
};

interface ModeMetrics {
  count: number;
  gaps: number; // summed (seq - lastSeq - 1) when seq jumps forward → dropped messages
  outOfOrder: number; // seq <= lastSeq → reorder/duplicate (or a server stream restart)
  lastSeq: number;
  lastLatencyMs: number;
}

function emptyMetrics(): ModeMetrics {
  return { count: 0, gaps: 0, outOfOrder: 0, lastSeq: 0, lastLatencyMs: 0 };
}

function applyMessage(m: ModeMetrics, seq: number, sentAtMs: number): void {
  if (m.lastSeq !== 0 && seq > m.lastSeq + 1) {
    m.gaps += seq - m.lastSeq - 1;
  }
  if (seq <= m.lastSeq) {
    m.outOfOrder += 1;
  }
  m.lastSeq = seq;
  m.count += 1;
  m.lastLatencyMs = Date.now() - sentAtMs;
}

const TpProbeRenderer = memo(function TpProbeRenderer({ context }: UiComponentRendererProps) {
  const client = context.client;

  // Metrics live in refs (mutated straight from the message callback, off the
  // reactive loop); a tick state forces a light re-render so the numbers paint.
  const reliable = useRef<ModeMetrics>(emptyMetrics());
  const unreliable = useRef<ModeMetrics>(emptyMetrics());
  const [, setTick] = useState(0);

  const sentReliable = useRef(0);
  const sentUnreliable = useRef(0);
  const clientSeq = useRef(0);

  const messagingRef = useRef<ReturnType<typeof appMessaging> | null>(null);

  useEffect(() => {
    if (!client) {
      return;
    }

    const messaging = appMessaging(client);
    messagingRef.current = messaging;

    const subReliable = messaging.on(ProbePingMessage, (p) => {
      applyMessage(reliable.current, Number(p.Seq), Number(p.SentAtMs));
      setTick((t) => t + 1);
    });
    const subUnreliable = messaging.on(ProbePingUnreliableMessage, (p) => {
      applyMessage(unreliable.current, Number(p.Seq), Number(p.SentAtMs));
      setTick((t) => t + 1);
    });

    return () => {
      subReliable.close();
      subUnreliable.close();
      messagingRef.current = null;
    };
  }, [client]);

  const sendReliable = () => {
    if (!messagingRef.current) {
      return;
    }
    clientSeq.current += 1;
    messagingRef.current.send(ProbePingMessage, {
      Seq: BigInt(clientSeq.current),
      SentAtMs: BigInt(Date.now()),
      Origin: 'client',
      Mode: 'reliable',
      Note: 'manual',
    });
    sentReliable.current += 1;
    setTick((t) => t + 1);
  };

  const sendUnreliable = () => {
    if (!messagingRef.current) {
      return;
    }
    clientSeq.current += 1;
    messagingRef.current.send(ProbePingUnreliableMessage, {
      Seq: BigInt(clientSeq.current),
      SentAtMs: BigInt(Date.now()),
      Origin: 'client',
      Mode: 'unreliable',
      Note: 'manual',
    });
    sentUnreliable.current += 1;
    setTick((t) => t + 1);
  };

  const resetMetrics = () => {
    reliable.current = emptyMetrics();
    unreliable.current = emptyMetrics();
    setTick((t) => t + 1);
  };

  return (
    <div style={containerStyle}>
      <div style={cardsRowStyle}>
        <MetricCard title="Reliable (ProbePing)" testid="reliable" m={reliable.current} />
        <MetricCard title="Unreliable (ProbePingUnreliable)" testid="unreliable" m={unreliable.current} />
      </div>
      <div style={controlsRowStyle}>
        <button type="button" style={buttonStyle} onClick={sendReliable} data-testid="tp-send-reliable">
          Send reliable → server
        </button>
        <button type="button" style={buttonStyle} onClick={sendUnreliable} data-testid="tp-send-unreliable">
          Send unreliable → server
        </button>
        <button type="button" style={ghostButtonStyle} onClick={resetMetrics} data-testid="tp-reset">
          Reset metrics
        </button>
        <span style={sentStyle} data-testid="tp-sent">
          sent: {sentReliable.current} reliable / {sentUnreliable.current} unreliable
        </span>
      </div>
    </div>
  );
});

function MetricCard({ title, testid, m }: { title: string; testid: string; m: ModeMetrics }) {
  return (
    <div style={cardStyle} data-testid={`tp-card-${testid}`}>
      <div style={cardTitleStyle}>{title}</div>
      <MetricRow label="received" value={m.count} testid={`tp-${testid}-count`} />
      <MetricRow label="gaps (drops)" value={m.gaps} testid={`tp-${testid}-gaps`} />
      <MetricRow label="out-of-order" value={m.outOfOrder} testid={`tp-${testid}-ooo`} />
      <MetricRow label="last seq" value={m.lastSeq} testid={`tp-${testid}-seq`} />
      <MetricRow label="last latency (ms)" value={m.lastLatencyMs} testid={`tp-${testid}-latency`} />
    </div>
  );
}

function MetricRow({ label, value, testid }: { label: string; value: number; testid: string }) {
  return (
    <div style={rowStyle}>
      <span style={{ opacity: 0.7 }}>{label}</span>
      <span style={{ fontVariantNumeric: 'tabular-nums', fontWeight: 600 }} data-testid={testid}>
        {value}
      </span>
    </div>
  );
}

const containerStyle: CSSProperties = { display: 'flex', flexDirection: 'column', gap: 12, width: '100%' };
const cardsRowStyle: CSSProperties = { display: 'flex', gap: 12, flexWrap: 'wrap' };
const controlsRowStyle: CSSProperties = { display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center' };
const cardStyle: CSSProperties = {
  border: '1px solid rgba(127,127,127,0.35)',
  borderRadius: 8,
  padding: 12,
  minWidth: 240,
  flex: '1 1 240px',
};
const cardTitleStyle: CSSProperties = { fontWeight: 600, marginBottom: 8 };
const rowStyle: CSSProperties = { display: 'flex', justifyContent: 'space-between', gap: 16, padding: '2px 0' };
const buttonStyle: CSSProperties = {
  border: '1px solid rgba(127,127,127,0.45)',
  borderRadius: 6,
  padding: '6px 12px',
  cursor: 'pointer',
  background: 'transparent',
  color: 'inherit',
};
const ghostButtonStyle: CSSProperties = { ...buttonStyle, opacity: 0.75 };
const sentStyle: CSSProperties = { opacity: 0.7, fontVariantNumeric: 'tabular-nums' };

export function createTpProbeResolver(): IkonUiComponentResolver {
  return (initialNode) => (initialNode.type !== 'tp-probe' ? undefined : TpProbeRenderer);
}
