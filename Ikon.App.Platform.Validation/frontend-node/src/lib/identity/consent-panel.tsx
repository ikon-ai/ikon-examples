import { memo, useEffect, useState } from 'react';
import { type IkonUiComponentResolver, type UiComponentRendererProps, useConsent, useUiNode } from '@ikonai/sdk-react-ui';
import {
  CONSENT_AI_PROVIDER_PERSONAL_DATA,
  CONSENT_LIST_FUNCTION_NAME,
  CONSENT_USAGE_MEASUREMENT,
  consentFor,
  grantConsent,
  isConsentGranted,
  notAskedConsent,
  readConsent,
  revokeConsent,
  type IkonClient,
} from '@ikonai/sdk';
import { describeError, panelStyles, resultStyle } from './panel-styles';

export const CONSENT_PANEL_NODE_TYPE = 'validation-consent-panel';

// Distinct from the C# self-check purpose, so the two checks never race over one answer.
const REACT_SELF_CHECK_PURPOSE = 'validation-react-self-check';

async function runRawConsentSelfCheck(client: IkonClient): Promise<string> {
  if (notAskedConsent('x').granted || isConsentGranted(null, CONSENT_USAGE_MEASUREMENT)) {
    return 'FAIL the not-asked default read as granted';
  }

  const revoked = await revokeConsent(client, REACT_SELF_CHECK_PURPOSE);
  const afterRevoke = await readConsent(client);

  if (revoked.state !== 'denied' || consentFor(afterRevoke, REACT_SELF_CHECK_PURPOSE).state !== 'denied' || isConsentGranted(afterRevoke, REACT_SELF_CHECK_PURPOSE)) {
    return `FAIL after revokeConsent the server answered ${consentFor(afterRevoke, REACT_SELF_CHECK_PURPOSE).state}`;
  }

  const granted = await grantConsent(client, REACT_SELF_CHECK_PURPOSE);
  const afterGrant = await readConsent(client);

  if (!granted.granted || granted.decidedAt === null || !isConsentGranted(afterGrant, REACT_SELF_CHECK_PURPOSE)) {
    return `FAIL after grantConsent the server answered ${consentFor(afterGrant, REACT_SELF_CHECK_PURPOSE).state}`;
  }

  await revokeConsent(client, REACT_SELF_CHECK_PURPOSE);

  if (consentFor(afterGrant, 'validation-never-asked').state !== 'not-asked') {
    return 'FAIL a purpose nobody answered did not read as not-asked';
  }

  return `PASS readConsent/grantConsent/revokeConsent round-tripped through ${CONSENT_LIST_FUNCTION_NAME}; ${Object.keys(afterGrant).length} answers on record`;
}

const ConsentPanelRenderer = memo(function ConsentPanelRenderer({ nodeId, context, className }: UiComponentRendererProps) {
  const node = useUiNode(context.store, nodeId);
  const client = context.client ?? null;
  const purposesProp = node?.props?.['purposes'];
  const version = node?.props?.['version'];
  const purposes = typeof purposesProp === 'string' && purposesProp.length > 0 ? purposesProp.split(',') : [CONSENT_USAGE_MEASUREMENT, CONSENT_AI_PROVIDER_PERSONAL_DATA];
  const consent = useConsent({ client, purposes });
  const { reload } = consent;
  const [selfCheckResult, setSelfCheckResult] = useState<string | null>(null);
  const [selfCheckRunning, setSelfCheckRunning] = useState(false);

  // The server bumps `version` on every app.Consent.OnChanged, so an answer recorded in C# is read
  // back through the SDK here — the browser half of the round trip.
  useEffect(() => {
    void reload();
  }, [reload, version]);

  const runSelfCheck = async () => {
    if (!client) {
      setSelfCheckResult('FAIL the app is not connected');
      return;
    }

    setSelfCheckRunning(true);

    try {
      setSelfCheckResult(await runRawConsentSelfCheck(client));
    } catch (error) {
      setSelfCheckResult(`FAIL ${describeError(error)}`);
    } finally {
      setSelfCheckRunning(false);
    }
  };

  if (!node) {
    return null;
  }

  return (
    <div className={className} style={panelStyles.container} data-testid="consent-react-panel">
      {purposes.map((purpose) => {
        const record = consent.consentFor(purpose);

        return (
          <div key={purpose} style={panelStyles.row}>
            <span style={panelStyles.value}>{purpose}</span>
            <span style={panelStyles.actions}>
              <span style={{ ...panelStyles.value, fontWeight: 600 }} data-testid={`consent-react-state-${purpose}`}>
                {consent.isLoading ? 'loading' : record.state}
              </span>
              <button type="button" style={panelStyles.button} disabled={consent.isSubmitting} onClick={() => void consent.grant(purpose)} data-testid={`consent-react-grant-${purpose}`}>
                Grant
              </button>
              <button type="button" style={panelStyles.ghostButton} disabled={consent.isSubmitting} onClick={() => void consent.revoke(purpose)} data-testid={`consent-react-revoke-${purpose}`}>
                Revoke
              </button>
            </span>
          </div>
        );
      })}
      <div style={panelStyles.caption} data-testid="consent-react-granted-count">
        granted: {purposes.filter((purpose) => consent.isGranted(purpose)).length} of {purposes.length}
      </div>
      {consent.error && (
        <div style={resultStyle('Error')} data-testid="consent-react-error">
          Error: {consent.error}
        </div>
      )}
      <div style={panelStyles.actions}>
        <button type="button" style={panelStyles.button} disabled={selfCheckRunning} onClick={() => void runSelfCheck()} data-testid="consent-react-selfcheck-run">
          Run self-check
        </button>
      </div>
      {selfCheckResult && (
        <div style={resultStyle(selfCheckResult)} data-testid="consent-react-selfcheck-result">
          {selfCheckResult}
        </div>
      )}
    </div>
  );
});

export function createConsentPanelResolver(): IkonUiComponentResolver {
  return (node) => (node.type === CONSENT_PANEL_NODE_TYPE ? ConsentPanelRenderer : undefined);
}
