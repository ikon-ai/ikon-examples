import { memo, useEffect, useRef, useState } from 'react';
import { ContextType, EntrypointType, FunctionCallError, type IkonClient } from '@ikonai/sdk';
import { type IkonUiComponentResolver, type UiComponentRendererProps, useUiNode } from '@ikonai/sdk-react-ui';
import { panelStyles } from '../identity/panel-styles';

export const SURFACE_PROBE_NODE_TYPE = 'validation-surface-probe';

// What a surface changes is seen from the client: whether its GlobalState lists other visitors
// (Presence), which entrypoint carries it, and whether a function scoped to some surfaces
// answers it. The server cannot see any of these, so the surfaces phase reads them here.

function peerCount(client: IkonClient | null): number | null {
  const clients = client?.globalState?.Clients;

  if (!clients) {
    return null;
  }

  return Object.entries(clients).filter(([sessionId, context]) => Number(sessionId) !== client.sessionId && context.ContextType === ContextType.Browser).length;
}

function endpointName(client: IkonClient | null): string {
  const endpoint = client?.activeEndpointType;
  return endpoint == null ? 'none' : (EntrypointType[endpoint] ?? String(endpoint));
}

async function callAndDescribe(client: IkonClient, name: string): Promise<string> {
  try {
    return `OK ${String(await client.functionRegistry.call(name, name === 'AllTypes' ? ['s', 1, true, 1, [], {}, '12345678-1234-1234-1234-123456789abc', '2024-06-15T12:30:00Z'] : []))}`;
  } catch (error) {
    return error instanceof FunctionCallError && error.code ? `REFUSED ${error.code}` : `ERROR ${error instanceof Error ? error.message : String(error)}`;
  }
}

const SurfaceProbeRenderer = memo(function SurfaceProbeRenderer({ nodeId, context, className }: UiComponentRendererProps) {
  const node = useUiNode(context.store, nodeId);
  const client = context.client ?? null;
  const [peers, setPeers] = useState<number | null>(null);
  const [endpoint, setEndpoint] = useState('none');
  const [callResult, setCallResult] = useState('');

  // A page that connects on its first action draws the probe before it has a client, so the timer
  // reads the latest one rather than the one it started with
  const clientRef = useRef(client);

  useEffect(() => {
    clientRef.current = client;
  });

  // The client raises no event when its GlobalState changes, so the probe reads it twice a second
  useEffect(() => {
    const timer = window.setInterval(() => {
      setPeers(peerCount(clientRef.current));
      setEndpoint(endpointName(clientRef.current));
    }, 500);
    return () => window.clearInterval(timer);
  }, []);

  if (!node) {
    return null;
  }

  const call = async (name: string) => {
    setCallResult('');
    const current = clientRef.current;
    setCallResult(current ? await callAndDescribe(current, name) : 'ERROR not connected');
  };

  return (
    <div className={className} style={panelStyles.container}>
      <div style={panelStyles.row}>
        <span>Other visitors this client sees</span>
        <span style={panelStyles.value} data-testid="probe-peer-count">
          {peers ?? 'unknown'}
        </span>
      </div>
      <div style={panelStyles.row}>
        <span>Entrypoint</span>
        <span style={panelStyles.value} data-testid="probe-endpoint">
          {endpoint}
        </span>
      </div>
      <div style={panelStyles.actions}>
        <button type="button" style={panelStyles.button} onClick={() => call('SurfaceEcho')} data-testid="probe-call-surface-echo">
          Call SurfaceEcho
        </button>
        <button type="button" style={panelStyles.button} onClick={() => call('AllTypes')} data-testid="probe-call-all-types">
          Call AllTypes
        </button>
      </div>
      <div style={panelStyles.row}>
        <span>Call result</span>
        <span style={panelStyles.value} data-testid="probe-call-result">
          {callResult}
        </span>
      </div>
    </div>
  );
});

export function createSurfaceProbeResolver(): IkonUiComponentResolver {
  return (initialNode) => (initialNode.type === SURFACE_PROBE_NODE_TYPE ? SurfaceProbeRenderer : undefined);
}
