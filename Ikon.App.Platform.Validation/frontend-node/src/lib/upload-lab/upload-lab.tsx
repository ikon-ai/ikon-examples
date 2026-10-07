import { memo, useEffect, useState } from 'react';
import { type IkonUiComponentResolver, type UiComponentRendererProps, useUiNode } from '@ikonai/sdk-react-ui';
import { uploadFile, type IkonClient } from '@ikonai/sdk';
import type { UiNode } from '@ikonai/sdk-ui';
import { describeError, panelStyles, resultStyle } from '../identity/panel-styles';

// The browser half of the Files tab's upload lab (Validation.Files.Lifecycle.cs). The C# side
// publishes a plan; this sends each planned file through the SDK's own uploadFile into the lab's
// FileUploadZone and reports what the client saw back through the UploadLabReport function. The
// verdict is the server's, which also knows what the hooks saw and what was stored.

const CHUNK_SIZE = 1024 * 1024;

interface PlanItem {
  id: string;
  name: string;
  size: number;
  content: 'random' | 'json';
  corrupt: boolean;
  abandonAfterChunks: number;
  reconnectAfterChunks: number;
  delayMs: number;
  expectStop: boolean;
}

interface Plan {
  run: string;
  items: PlanItem[];
}

type LabFile = Blob & { readonly name: string };

// The verdict of an upload the app is stopped under. Kept outside React: the stop takes the server's
// instance with it, a hot reload remounts this component, and the upload's promise outlives both.
let stopResult = '';

// Every plan this page has started. The plan prop outlives the component (leaving the tab and coming
// back remounts it, and a hot reload restores the prop), and a plan must run once.
const startedRuns = new Set<string>();
const stopResultListeners = new Set<(result: string) => void>();

function setStopResult(result: string): void {
  stopResult = result;
  stopResultListeners.forEach((listener) => listener(result));
}

function parsePlan(value: unknown): Plan | null {
  if (typeof value !== 'string' || value.length === 0) {
    return null;
  }

  return JSON.parse(value) as Plan;
}

function randomBytes(size: number): Uint8Array<ArrayBuffer> {
  const bytes = new Uint8Array(size);
  const step = 65536;

  for (let offset = 0; offset < size; offset += step) {
    crypto.getRandomValues(bytes.subarray(offset, Math.min(offset + step, size)));
  }

  return bytes;
}

// The same document Validation.LabJsonDocument builds for this size, so the server can compare.
function jsonDocument(size: number): Uint8Array<ArrayBuffer> {
  const parts = ['{"lab":"upload","rows":['];
  let length = parts[0].length;
  let row = 0;

  while (length < size - 64) {
    const part = `${row === 0 ? '' : ','}{"i":${row},"v":"row-${row}"}`;
    parts.push(part);
    length += part.length;
    row++;
  }

  parts.push(']}');
  return new TextEncoder().encode(parts.join(''));
}

// A file whose behaviour the plan bends: `stream()` (what the SDK hashes) can disagree with
// `slice()` (what it sends), a slice can hang forever so the client stops sending, and a slice can
// be the moment to drop the connection.
function labFile(item: PlanItem, bytes: Uint8Array<ArrayBuffer>, onSlice: (chunkIndex: number) => Promise<void>): LabFile {
  const file = new File([bytes], item.name, { type: item.content === 'json' ? 'application/json' : 'application/octet-stream' });
  let hashed: Blob = file;

  if (item.corrupt) {
    const corrupted = bytes.slice();
    corrupted[corrupted.length >> 1] ^= 0xff;
    hashed = new Blob([corrupted]);
  }

  return {
    name: file.name,
    type: file.type,
    size: file.size,
    stream: () => hashed.stream(),
    slice: (start?: number, end?: number) => {
      const piece = file.slice(start, end);
      const chunkIndex = Math.floor((start ?? 0) / CHUNK_SIZE);

      return {
        arrayBuffer: async () => {
          await onSlice(chunkIndex);
          return piece.arrayBuffer();
        },
      } as Blob;
    },
  } as unknown as LabFile;
}

// The SDK has no public way to drop its link; the reconnect a missed action ack forces is the same
// path a real drop takes, so the lab reaches for it.
function dropConnection(client: IkonClient): void {
  const internals = client as unknown as {
    protocolWorker?: { postMessage(message: unknown): void } | null;
    channelManager?: { triggerReconnect(reason: string): void } | null;
  };

  if (internals.protocolWorker) {
    internals.protocolWorker.postMessage({ type: 'triggerReconnect', reason: 'Upload lab dropped the connection' });
  } else {
    internals.channelManager?.triggerReconnect('Upload lab dropped the connection');
  }
}

function findUploadActionId(nodes: Iterable<UiNode>, zoneTestId: string): string | null {
  for (const node of nodes) {
    const actionId = node.props?.['uploadActionId'];

    if (node.props?.['data-testid'] === zoneTestId && typeof actionId === 'string') {
      return actionId;
    }

    const found = findUploadActionId(node.children, zoneTestId);

    if (found) {
      return found;
    }
  }

  return null;
}

async function runItem(client: IkonClient, uploadActionId: string, item: PlanItem): Promise<void> {
  if (item.delayMs > 0) {
    await new Promise((resolve) => setTimeout(resolve, item.delayMs));
  }

  const bytes = item.content === 'json' ? jsonDocument(item.size) : randomBytes(item.size);
  let reconnected = false;
  let abandon: () => void = () => undefined;
  const abandoned = new Promise<void>((resolve) => {
    abandon = resolve;
  });

  const file = labFile(item, bytes, async (chunkIndex) => {
    if (item.reconnectAfterChunks > 0 && chunkIndex === item.reconnectAfterChunks && !reconnected) {
      reconnected = true;
      dropConnection(client);
    }

    if (item.abandonAfterChunks > 0 && chunkIndex >= item.abandonAfterChunks) {
      abandon();
      // Never settles: the client stops sending mid-transfer, the way a closed tab does
      await new Promise(() => undefined);
    }
  });

  const started = performance.now();
  let error: string;

  try {
    const outcome = await Promise.race([uploadFile(client, { file, uploadId: item.id, uploadActionId }).then(() => ''), abandoned.then(() => 'abandoned')]);
    error = outcome;
  } catch (caught) {
    error = describeError(caught) || 'failed';
  }

  if (item.expectStop) {
    setStopResult(
      /stopping/i.test(error)
        ? `PASS the client was told '${error}' ${((performance.now() - started) / 1000).toFixed(1)} s in`
        : `FAIL the upload ended with '${error || 'success'}', not the app's stop`,
    );
  }

  // An empty error is a delivered upload
  await client.functionRegistry.call('UploadLabReport', [item.id, error, performance.now() - started, reconnected]);
}

function describeInFlight(remaining: number): string {
  return remaining === 0 ? 'Idle' : `Uploading ${remaining} file${remaining === 1 ? '' : 's'}`;
}

async function runPlan(client: IkonClient, plan: Plan, uploadActionId: string | null, setStatus: (status: string) => void): Promise<void> {
  if (!uploadActionId) {
    setStatus('FAIL the lab zone has no upload action id');
    return;
  }

  let remaining = plan.items.length;
  setStatus(describeInFlight(remaining));

  await Promise.all(
    plan.items.map((item) =>
      runItem(client, uploadActionId, item)
        .catch((caught: unknown) => {
          // The report itself failed; the server times the run out and names the silent upload
          console.warn(`Upload lab could not report ${item.name}:`, caught);
        })
        .finally(() => {
          remaining--;
          setStatus(describeInFlight(remaining));
        }),
    ),
  );
}

const UploadLabRenderer = memo(function UploadLabRenderer({ nodeId, context, className }: UiComponentRendererProps) {
  const node = useUiNode(context.store, nodeId);
  const client = context.client;
  const planText = typeof node?.props?.['plan'] === 'string' ? node.props['plan'] : '';
  const zoneTestId = typeof node?.props?.['zoneTestId'] === 'string' ? node.props['zoneTestId'] : '';
  const [status, setStatus] = useState('Idle');
  const [appStopResult, setAppStopResult] = useState(stopResult);

  useEffect(() => {
    stopResultListeners.add(setAppStopResult);
    return () => {
      stopResultListeners.delete(setAppStopResult);
    };
  }, []);

  useEffect(() => {
    const plan = parsePlan(planText);

    if (!client || !plan || startedRuns.has(plan.run)) {
      return;
    }

    startedRuns.add(plan.run);
    void runPlan(client, plan, findUploadActionId(context.store.getSnapshot().views.values(), zoneTestId), setStatus);
  }, [client, planText, zoneTestId, context.store]);

  if (!node) {
    return null;
  }

  return (
    <div className={className} style={panelStyles.container}>
      <span style={panelStyles.caption} data-testid="upl-lab-status">
        {status}
      </span>
      {appStopResult && (
        <span style={panelStyles.caption}>
          App stops mid-upload:{' '}
          <span style={resultStyle(appStopResult)} data-testid="upl-lab-stop-result">
            {appStopResult}
          </span>
        </span>
      )}
    </div>
  );
});

export function createUploadLabResolver(): IkonUiComponentResolver {
  return (initialNode) => (initialNode.type !== 'upload-lab' ? undefined : UploadLabRenderer);
}
