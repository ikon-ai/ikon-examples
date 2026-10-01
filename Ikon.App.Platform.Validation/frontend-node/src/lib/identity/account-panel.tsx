import { memo, useState } from 'react';
import { type IkonUiComponentResolver, type UiComponentRendererProps, useAccountRemoval, useAuth, useAuthOptional } from '@ikonai/sdk-react-ui';
import { USER_REMOVAL_GRACE_DAYS, getCurrentUser } from '@ikonai/sdk';
import { authConfig } from '../../env';
import { describeError, panelStyles, resultStyle } from './panel-styles';

export const ACCOUNT_PANEL_NODE_TYPE = 'validation-account-panel';

// Deleting a real account must never be one stray click away — least of all an automated one — so
// the request needs this phrase typed out first.
const DELETE_CONFIRMATION = 'DELETE MY ACCOUNT';

function formatInstant(value: string | undefined): string {
  if (!value) {
    return 'unknown';
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

// useAccountRemoval needs the AuthProvider; a render outside it (a prerender, an embed) says so
// instead of throwing the whole tab away.
const AccountPanelRenderer = memo(function AccountPanelRenderer({ className }: UiComponentRendererProps) {
  if (!useAuthOptional()) {
    return (
      <div className={className} style={panelStyles.caption} data-testid="account-panel">
        No sign-in provider
      </div>
    );
  }

  return <AccountPanel className={className} />;
});

function AccountPanel({ className }: { className?: string }) {
  const { state, getToken } = useAuth();
  const removal = useAccountRemoval({ backendUrl: authConfig.backendUrl });
  const [confirmation, setConfirmation] = useState('');
  const [readResult, setReadResult] = useState<string | null>(null);

  const provider = state.user?.provider ?? 'none';
  const confirmed = confirmation.trim() === DELETE_CONFIRMATION;

  const readAccount = async () => {
    const authToken = getToken();

    if (!authToken) {
      setReadResult('SKIP no session token: not signed in');
      return;
    }

    try {
      const user = await getCurrentUser({ authToken, backendUrl: authConfig.backendUrl });
      setReadResult(`PASS /users/me answered id=${user.id}${user.removal ? `, removal scheduled for ${user.removal.scheduledFor}` : ''}`);
    } catch (error) {
      setReadResult(`Error: ${describeError(error)}`);
    }
  };

  const requestRemoval = async () => {
    if (!confirmed) {
      return;
    }

    setConfirmation('');
    await removal.requestAccountRemoval();
  };

  return (
    <div className={className} style={panelStyles.container} data-testid="account-panel">
      <div style={panelStyles.row}>
        <span>Signed in with</span>
        <span style={panelStyles.value} data-testid="account-provider">
          {provider}
        </span>
      </div>
      <div style={panelStyles.row}>
        <span>Can request removal</span>
        <span style={panelStyles.value} data-testid="account-can-remove">
          {removal.canRequestRemoval ? 'true' : 'false (guest or dev sign-in has no account to erase)'}
        </span>
      </div>
      <div style={panelStyles.row}>
        <span>Grace period</span>
        <span style={panelStyles.value} data-testid="account-grace-days">
          {removal.removalGraceDays} days (SDK default {USER_REMOVAL_GRACE_DAYS})
        </span>
      </div>
      <div style={panelStyles.row}>
        <span>Scheduled removal</span>
        <span style={panelStyles.value} data-testid="account-scheduled-removal">
          {removal.isLoadingAccount ? 'loading' : removal.scheduledRemoval ? `runs ${formatInstant(removal.scheduledRemoval.scheduledFor)} (asked ${formatInstant(removal.scheduledRemoval.requestedAt)})` : 'none'}
        </span>
      </div>

      <div style={panelStyles.actions}>
        <button type="button" style={panelStyles.button} onClick={() => void readAccount()} data-testid="account-read-me">
          Read my account (getCurrentUser)
        </button>
        {readResult && (
          <span style={resultStyle(readResult)} data-testid="account-read-me-result">
            {readResult}
          </span>
        )}
      </div>

      <div style={panelStyles.heading}>Delete my account</div>
      <div style={panelStyles.caption}>
        Type <span style={panelStyles.value}>{DELETE_CONFIRMATION}</span> to enable.
      </div>
      <div style={panelStyles.actions}>
        <input
          type="text"
          style={panelStyles.input}
          value={confirmation}
          placeholder={DELETE_CONFIRMATION}
          aria-label="Type the confirmation phrase to enable account deletion"
          disabled={!removal.canRequestRemoval || removal.isSubmittingRemoval}
          onChange={(event) => setConfirmation(event.target.value)}
          data-testid="account-delete-confirmation"
        />
        <button
          type="button"
          style={panelStyles.dangerButton}
          disabled={!removal.canRequestRemoval || !confirmed || removal.isSubmittingRemoval || removal.scheduledRemoval !== null}
          onClick={() => void requestRemoval()}
          data-testid="account-delete-request"
        >
          Delete my account
        </button>
        <button
          type="button"
          style={panelStyles.button}
          disabled={!removal.canRequestRemoval || removal.isSubmittingRemoval || removal.scheduledRemoval === null}
          onClick={() => void removal.cancelAccountRemoval()}
          data-testid="account-delete-cancel"
        >
          Cancel scheduled deletion
        </button>
      </div>
      {removal.removalError && (
        <div style={resultStyle('Error')} data-testid="account-removal-error">
          Error: {removal.removalError}
        </div>
      )}
    </div>
  );
}

export function createAccountPanelResolver(): IkonUiComponentResolver {
  return (node) => (node.type === ACCOUNT_PANEL_NODE_TYPE ? AccountPanelRenderer : undefined);
}
