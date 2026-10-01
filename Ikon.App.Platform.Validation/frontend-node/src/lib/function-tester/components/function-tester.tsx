import { memo, useCallback, useState } from 'react';
import { type IkonUiComponentResolver, type UiComponentRendererProps, useUiNode } from '@ikonai/sdk-react-ui';
import type { IkonClient } from '@ikonai/sdk';
import { describeError, panelStyles, resultStyle } from '../../identity/panel-styles';

// Calling C# functions from the browser needs browser code, which is why this one check is React.
// It calls the app's two ValidationFunctions and compares what comes back with what went in.
const ALL_TYPES_ARGS = ['hello', 3.14, true, 42, ['alpha', 'beta', 'gamma'], { min: 1.5, max: 9.9 }, '12345678-1234-1234-1234-123456789abc', '2024-06-15T12:30:00Z'];
const ECHO_BYTES = new Uint8Array([0x01, 0x02, 0x03, 0xff, 0xfe]);

async function checkAllTypes(client: IkonClient): Promise<string> {
  const value = await client.functionRegistry.call('AllTypes', ALL_TYPES_ARGS);
  const echoed = JSON.parse(String(value)) as Record<string, unknown>;
  const mismatches = [
    echoed.s === 'hello' ? null : 's',
    echoed.d === 3.14 ? null : 'd',
    echoed.b === true ? null : 'b',
    echoed.i === 42 ? null : 'i',
    JSON.stringify(echoed.list) === JSON.stringify(['alpha', 'beta', 'gamma']) ? null : 'list',
    JSON.stringify(echoed.dict) === JSON.stringify({ min: 1.5, max: 9.9 }) ? null : 'dict',
    echoed.guid === '12345678-1234-1234-1234-123456789abc' ? null : 'guid',
    Date.parse(String(echoed.dateTime)) === Date.parse('2024-06-15T12:30:00Z') ? null : 'dateTime',
  ].filter((name) => name !== null);

  return mismatches.length === 0 ? 'PASS all eight types round-tripped' : `FAIL ${mismatches.join(', ')} came back different: ${String(value)}`;
}

async function checkEchoBytes(client: IkonClient): Promise<string> {
  const value = await client.functionRegistry.call('EchoBytes', [ECHO_BYTES]);
  const bytes = value instanceof Uint8Array ? Array.from(value) : null;
  const sent = Array.from(ECHO_BYTES);

  return bytes !== null && bytes.length === sent.length && bytes.every((b, index) => b === sent[index])
    ? `PASS ${sent.length} bytes echoed`
    : `FAIL got ${bytes === null ? typeof value : bytes.join(',')}`;
}

async function runCheck(check: (client: IkonClient) => Promise<string>, client: IkonClient): Promise<string> {
  try {
    return await check(client);
  } catch (error) {
    return `FAIL ${describeError(error)}`;
  }
}

const FunctionTesterRenderer = memo(function FunctionTesterRenderer({ nodeId, context, className }: UiComponentRendererProps) {
  const node = useUiNode(context.store, nodeId);
  const client = context.client;
  const [allTypesResult, setAllTypesResult] = useState('');
  const [echoBytesResult, setEchoBytesResult] = useState('');
  const [running, setRunning] = useState(false);

  const run = useCallback(async () => {
    if (!client) {
      return;
    }

    setRunning(true);
    setAllTypesResult('');
    setEchoBytesResult('');
    setAllTypesResult(await runCheck(checkAllTypes, client));
    setEchoBytesResult(await runCheck(checkEchoBytes, client));
    setRunning(false);
  }, [client]);

  if (!node) {
    return null;
  }

  return (
    <div className={className} style={panelStyles.container}>
      <div style={panelStyles.actions}>
        <button type="button" style={panelStyles.button} onClick={run} disabled={running || !client} data-testid="functions-run">
          {running ? 'Calling…' : 'Call from the browser'}
        </button>
      </div>
      <div style={panelStyles.row}>
        <span style={panelStyles.value}>AllTypes</span>
        <span style={resultStyle(allTypesResult)} data-testid="functions-alltypes-result">{allTypesResult}</span>
      </div>
      <div style={panelStyles.row}>
        <span style={panelStyles.value}>EchoBytes</span>
        <span style={resultStyle(echoBytesResult)} data-testid="functions-echobytes-result">{echoBytesResult}</span>
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
