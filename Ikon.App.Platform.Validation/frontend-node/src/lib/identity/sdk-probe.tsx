import { memo, useMemo, useState } from 'react';
import {
  type IkonUiComponentResolver,
  type UiComponentRendererProps,
  type UseMediaCaptureResult,
  VideoStreamView,
  useCameraCapture,
  useIkonFeedback,
  useMicrophoneCapture,
  useScreenCapture,
  useSiblingClient,
  useUiNode,
} from '@ikonai/sdk-react-ui';
import { panelStyles, resultStyle } from './panel-styles';

export const SDK_PROBE_NODE_TYPE = 'validation-sdk-probe';

// The sibling carries the owner's own query parameters so it resolves to the same session identity,
// with only Test replaced — which is what lets the server count it apart from the page's connection.
function buildSiblingParameters(siblingTest: string): Record<string, string> {
  const parameters: Record<string, string> = {};

  new URLSearchParams(window.location.search).forEach((value, key) => {
    if (!key.startsWith('ikon-')) {
      parameters[key] = value;
    }
  });

  parameters['test'] = siblingTest;
  return parameters;
}

function stringProp(value: unknown): string | null {
  return typeof value === 'string' && value.length > 0 ? value : null;
}

function CaptureRow({ label, testid, active, onToggle, capture }: { label: string; testid: string; active: boolean; onToggle: () => void; capture: UseMediaCaptureResult }) {
  return (
    <div style={panelStyles.row}>
      <span>{label}</span>
      <span style={panelStyles.actions}>
        <span style={panelStyles.value} data-testid={`sdk-${testid}-state`}>
          {capture.error ? `Error: ${capture.error.message}` : active ? 'capturing' : 'off'}
        </span>
        <button type="button" style={panelStyles.button} onClick={onToggle} data-testid={`sdk-${testid}-toggle`}>
          {active ? 'Stop' : 'Start'}
        </button>
      </span>
    </div>
  );
}

const SdkProbeRenderer = memo(function SdkProbeRenderer({ nodeId, context, className }: UiComponentRendererProps) {
  const node = useUiNode(context.store, nodeId);
  const client = context.client ?? null;
  const siblingTest = stringProp(node?.props?.['siblingTest']) ?? 'validation-sibling';
  const cameraEchoStreamId = stringProp(node?.props?.['cameraEchoStreamId']);
  const screenEchoStreamId = stringProp(node?.props?.['screenEchoStreamId']);

  const feedback = useIkonFeedback(client);
  const [feedbackResult, setFeedbackResult] = useState<string | null>(null);

  const [siblingWanted, setSiblingWanted] = useState(false);
  const siblingParameters = useMemo(() => (siblingWanted ? buildSiblingParameters(siblingTest) : null), [siblingWanted, siblingTest]);
  const sibling = useSiblingClient(client, siblingParameters);

  const [micActive, setMicActive] = useState(false);
  const [cameraActive, setCameraActive] = useState(false);
  const [screenActive, setScreenActive] = useState(false);
  const mic = useMicrophoneCapture(client, micActive);
  const camera = useCameraCapture(client, cameraActive);
  const screen = useScreenCapture(client, screenActive);

  const openFeedback = () => {
    if (!feedback.available) {
      setFeedbackResult('SKIP feedback is not offered to this person in this session');
      return;
    }

    setFeedbackResult(feedback.open() ? 'PASS feedback sheet opened' : 'FAIL requestFeedback returned false although feedback is offered');
  };

  const siblingStatus = sibling.error
    ? `Error: ${sibling.error.message}`
    : sibling.client
      ? `PASS sibling connected as session ${sibling.client.sessionId ?? '?'} beside session ${client?.sessionId ?? '?'}`
      : siblingWanted
        ? 'connecting'
        : 'closed';

  if (!node) {
    return null;
  }

  return (
    <div className={className} style={panelStyles.container} data-testid="sdk-probe">
      <div style={panelStyles.caption} data-testid="sdk-probe-loaded">
        loaded
      </div>

      <div style={panelStyles.heading}>Feedback (useIkonFeedback)</div>
      <div style={panelStyles.row}>
        <span>Feedback offered</span>
        <span style={panelStyles.actions}>
          <span style={panelStyles.value} data-testid="sdk-feedback-available">
            {String(feedback.available)}
          </span>
          <button type="button" style={panelStyles.button} onClick={openFeedback} data-testid="sdk-feedback-open">
            Send feedback
          </button>
        </span>
      </div>
      {feedbackResult && (
        <div style={resultStyle(feedbackResult)} data-testid="sdk-feedback-result">
          {feedbackResult}
        </div>
      )}

      <div style={panelStyles.heading}>Sibling connection (useSiblingClient)</div>
      <div style={panelStyles.actions}>
        <button type="button" style={panelStyles.button} onClick={() => setSiblingWanted((wanted) => !wanted)} data-testid="sdk-sibling-toggle">
          {siblingWanted ? 'Close sibling' : 'Open sibling'}
        </button>
        <span style={resultStyle(siblingStatus.startsWith('PASS') || siblingStatus.startsWith('Error') ? siblingStatus : 'SKIP')} data-testid="sdk-sibling-status">
          {siblingStatus}
        </span>
      </div>

      <div style={panelStyles.heading}>Capture hooks</div>
      <CaptureRow label="Microphone (useMicrophoneCapture)" testid="mic" active={micActive} onToggle={() => setMicActive((active) => !active)} capture={mic} />
      <CaptureRow label="Camera (useCameraCapture)" testid="camera" active={cameraActive} onToggle={() => setCameraActive((active) => !active)} capture={camera} />
      <CaptureRow label="Screen (useScreenCapture)" testid="screen" active={screenActive} onToggle={() => setScreenActive((active) => !active)} capture={screen} />
      <div style={panelStyles.actions}>
        {cameraEchoStreamId && <VideoStreamView client={client} streamId={cameraEchoStreamId} style={panelStyles.video} attributes={{ 'data-testid': 'sdk-camera-echo' }} />}
        {screenEchoStreamId && <VideoStreamView client={client} streamId={screenEchoStreamId} style={panelStyles.video} attributes={{ 'data-testid': 'sdk-screen-echo' }} />}
      </div>
    </div>
  );
});

export function createSdkProbeResolver(): IkonUiComponentResolver {
  return (node) => (node.type === SDK_PROBE_NODE_TYPE ? SdkProbeRenderer : undefined);
}
