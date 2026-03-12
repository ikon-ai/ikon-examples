import { memo, useCallback, useState } from 'react';
import { type IkonUiComponentResolver, type UiComponentRendererProps, useUiNode } from '@ikonai/sdk-react-ui';
import type { FunctionRegistry } from '@ikonai/sdk';

type RemoteFunction = ReturnType<FunctionRegistry['getRemoteFunctions']>[number];

interface FunctionCallResult {
  value: unknown;
  error: string | null;
  durationMs: number;
}

const TEST_ARGS: Record<string, unknown[]> = {
  AllTypes: [
    'hello',
    3.14,
    true,
    42,
    ['alpha', 'beta', 'gamma'],
    { min: 1.5, max: 9.9 },
    '12345678-1234-1234-1234-123456789abc',
    '2024-06-15T12:30:00Z',
  ],
  EchoBytes: [new Uint8Array([0x01, 0x02, 0x03, 0xff, 0xfe])],
};

function formatArg(a: unknown): string {
  if (a instanceof Uint8Array) {
    return `Uint8Array[${Array.from(a).map((b) => '0x' + b.toString(16).padStart(2, '0')).join(', ')}]`;
  }
  return JSON.stringify(a);
}

function formatArgs(args: unknown[]): string {
  return args.map(formatArg).join(', ');
}

function formatResult(value: unknown): string {
  if (value instanceof Uint8Array) {
    return `Uint8Array[${Array.from(value).map((b) => '0x' + b.toString(16).padStart(2, '0')).join(', ')}]`;
  }
  return JSON.stringify(value, null, 2);
}

function FunctionCard({
  fn,
  onCall,
}: {
  fn: RemoteFunction;
  onCall: (name: string, args: unknown[]) => Promise<FunctionCallResult>;
}) {
  const [result, setResult] = useState<FunctionCallResult | null>(null);
  const [loading, setLoading] = useState(false);

  const args = TEST_ARGS[fn.name] ?? [];

  const handleCall = useCallback(async () => {
    setLoading(true);
    setResult(null);
    try {
      const r = await onCall(fn.name, args);
      setResult(r);
    } finally {
      setLoading(false);
    }
  }, [fn.name, args, onCall]);

  return (
    <div style={cardStyle}>
      <div style={cardHeaderStyle}>
        <span style={fnNameStyle}>{fn.name}</span>
        <span style={fnDescStyle}>{fn.description}</span>
      </div>

      <div style={cardBodyStyle}>
        <div style={argsRowStyle}>
          <span style={labelStyle}>Args:</span>
          <code style={codeStyle}>{formatArgs(args)}</code>
        </div>

        <div style={buttonRowStyle}>
          <button style={loading ? buttonDisabledStyle : buttonStyle} onClick={handleCall} disabled={loading}>
            {loading ? 'Calling...' : 'Call'}
          </button>
        </div>

        {result && (
          <div style={result.error ? errorResultStyle : successResultStyle}>
            <div style={resultHeaderStyle}>
              <span>{result.error ? 'Error' : 'Result'}</span>
              <span style={timingStyle}>{result.durationMs.toFixed(0)}ms</span>
            </div>
            <code style={resultCodeStyle}>{result.error ?? formatResult(result.value)}</code>
          </div>
        )}
      </div>
    </div>
  );
}

const FunctionTesterRenderer = memo(function FunctionTesterRenderer({ nodeId, context, className }: UiComponentRendererProps) {
  const node = useUiNode(context.store, nodeId);
  const client = context.client;

  const [functions, setFunctions] = useState<RemoteFunction[]>([]);
  const [initialized, setInitialized] = useState(false);

  if (!node) return null;

  if (!initialized && client) {
    const remoteFns = client.functionRegistry.getRemoteFunctions();
    if (remoteFns.length > 0) {
      setFunctions(remoteFns);
      setInitialized(true);
    }
  }

  const handleCall = useCallback(
    async (name: string, args: unknown[]): Promise<FunctionCallResult> => {
      if (!client) return { value: null, error: 'Not connected', durationMs: 0 };
      const start = performance.now();
      try {
        const value = await client.functionRegistry.call(name, args);
        return { value, error: null, durationMs: performance.now() - start };
      } catch (e) {
        return { value: null, error: e instanceof Error ? e.message : String(e), durationMs: performance.now() - start };
      }
    },
    [client],
  );

  const handleRefresh = useCallback(() => {
    if (!client) return;
    setFunctions(client.functionRegistry.getRemoteFunctions());
    setInitialized(true);
  }, [client]);

  return (
    <div className={className}>
      <div style={toolbarStyle}>
        <span style={countStyle}>
          {functions.length} remote function{functions.length !== 1 ? 's' : ''} discovered
        </span>
        <button style={refreshButtonStyle} onClick={handleRefresh}>
          Refresh
        </button>
      </div>

      {functions.length === 0 && (
        <div style={emptyStyle}>
          No remote functions found.{' '}
          <button style={linkButtonStyle} onClick={handleRefresh}>
            Click to refresh
          </button>
        </div>
      )}

      <div style={gridStyle}>
        {functions.map((fn) => (
          <FunctionCard key={fn.id} fn={fn} onCall={handleCall} />
        ))}
      </div>
    </div>
  );
});

export function createFunctionTesterResolver(): IkonUiComponentResolver {
  return (initialNode) => {
    if (initialNode.type !== 'function-tester') return undefined;
    return FunctionTesterRenderer;
  };
}

const cardStyle: React.CSSProperties = {
  border: '1px solid hsl(var(--border))',
  borderRadius: '0.5rem',
  overflow: 'hidden',
};

const cardHeaderStyle: React.CSSProperties = {
  padding: '0.75rem 1rem',
  borderBottom: '1px solid hsl(var(--border))',
  background: 'hsl(var(--muted))',
  display: 'flex',
  flexDirection: 'column',
  gap: '0.25rem',
};

const fnNameStyle: React.CSSProperties = {
  fontWeight: 600,
  fontSize: '0.95rem',
  fontFamily: 'monospace',
};

const fnDescStyle: React.CSSProperties = {
  fontSize: '0.8rem',
  color: 'hsl(var(--muted-foreground))',
};

const cardBodyStyle: React.CSSProperties = {
  padding: '0.75rem 1rem',
  display: 'flex',
  flexDirection: 'column',
  gap: '0.5rem',
};

const argsRowStyle: React.CSSProperties = {
  display: 'flex',
  alignItems: 'center',
  gap: '0.5rem',
  fontSize: '0.85rem',
};

const labelStyle: React.CSSProperties = {
  fontWeight: 500,
  color: 'hsl(var(--muted-foreground))',
};

const codeStyle: React.CSSProperties = {
  fontSize: '0.8rem',
  background: 'hsl(var(--muted))',
  padding: '0.15rem 0.4rem',
  borderRadius: '0.25rem',
  fontFamily: 'monospace',
};

const buttonRowStyle: React.CSSProperties = {
  display: 'flex',
  gap: '0.5rem',
  flexWrap: 'wrap',
};

const buttonStyle: React.CSSProperties = {
  padding: '0.4rem 1rem',
  borderRadius: '0.375rem',
  border: 'none',
  background: 'hsl(var(--primary))',
  color: 'hsl(var(--primary-foreground))',
  cursor: 'pointer',
  fontSize: '0.85rem',
  fontWeight: 500,
};

const buttonDisabledStyle: React.CSSProperties = {
  ...buttonStyle,
  opacity: 0.6,
  cursor: 'not-allowed',
};

const successResultStyle: React.CSSProperties = {
  background: 'hsl(var(--muted))',
  borderRadius: '0.375rem',
  padding: '0.5rem 0.75rem',
  fontSize: '0.85rem',
};

const errorResultStyle: React.CSSProperties = {
  ...successResultStyle,
  background: 'hsl(var(--destructive) / 0.1)',
  border: '1px solid hsl(var(--destructive) / 0.3)',
};

const resultHeaderStyle: React.CSSProperties = {
  display: 'flex',
  justifyContent: 'space-between',
  marginBottom: '0.25rem',
  fontWeight: 500,
  fontSize: '0.8rem',
};

const timingStyle: React.CSSProperties = {
  color: 'hsl(var(--muted-foreground))',
  fontSize: '0.75rem',
};

const resultCodeStyle: React.CSSProperties = {
  fontFamily: 'monospace',
  fontSize: '0.8rem',
  whiteSpace: 'pre-wrap',
  wordBreak: 'break-all',
  display: 'block',
};

const toolbarStyle: React.CSSProperties = {
  display: 'flex',
  justifyContent: 'space-between',
  alignItems: 'center',
  marginBottom: '0.75rem',
};

const countStyle: React.CSSProperties = {
  fontSize: '0.85rem',
  color: 'hsl(var(--muted-foreground))',
};

const refreshButtonStyle: React.CSSProperties = {
  padding: '0.3rem 0.75rem',
  borderRadius: '0.375rem',
  border: '1px solid hsl(var(--border))',
  background: 'transparent',
  cursor: 'pointer',
  fontSize: '0.8rem',
};

const emptyStyle: React.CSSProperties = {
  textAlign: 'center',
  padding: '2rem',
  color: 'hsl(var(--muted-foreground))',
  fontSize: '0.9rem',
};

const linkButtonStyle: React.CSSProperties = {
  background: 'none',
  border: 'none',
  color: 'hsl(var(--primary))',
  cursor: 'pointer',
  textDecoration: 'underline',
  fontSize: 'inherit',
  padding: 0,
};

const gridStyle: React.CSSProperties = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fill, minmax(350px, 1fr))',
  gap: '0.75rem',
};
