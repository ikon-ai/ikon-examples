import { useState } from 'react';
import { useAccountRemoval } from '@ikonai/sdk-react-ui';
import { authConfig } from '../env';
import { useI18n } from '../i18n/i18n';

export interface AccountScreenProps {
  /** Closes the screen and returns to the app. Left out, no dismiss button is offered. */
  onDismiss?: () => void;
}

function formatScheduledFor(scheduledFor: string, locale: string): string {
  const scheduledDate = new Date(scheduledFor);

  if (Number.isNaN(scheduledDate.getTime())) {
    return scheduledFor;
  }

  return scheduledDate.toLocaleDateString(locale, { year: 'numeric', month: 'long', day: 'numeric' });
}

/**
 * Where a signed-in end user asks for their account to be erased, and takes the request back.
 *
 * The erasure is scheduled, not immediate: the backend runs it after a grace period, and until then
 * cancelling it restores the account untouched. The screen states the period because the person
 * deciding has to know how long they have.
 */
export function AccountScreen({ onDismiss }: AccountScreenProps) {
  const { t, locale } = useI18n();
  const { scheduledRemoval, isLoadingAccount, isSubmittingRemoval, removalError, canRequestRemoval, removalGraceDays, requestAccountRemoval, cancelAccountRemoval } = useAccountRemoval({
    backendUrl: authConfig.backendUrl,
  });
  const [isConfirmingRemoval, setIsConfirmingRemoval] = useState(false);

  return (
    <main className="ikon-surface ikon-account-screen">
      <section className="ikon-account-container">
        <h1 className="ikon-account-title">{t('account.title')}</h1>

        {!canRequestRemoval && <p className="ikon-account-message">{t('account.signedOut')}</p>}

        {canRequestRemoval && scheduledRemoval && (
          <p className="ikon-account-scheduled">{t('account.scheduled', { date: formatScheduledFor(scheduledRemoval.scheduledFor, locale) })}</p>
        )}

        {canRequestRemoval && !scheduledRemoval && <p className="ikon-account-message">{t('account.delete.explanation', { days: String(removalGraceDays) })}</p>}

        {canRequestRemoval && !scheduledRemoval && isConfirmingRemoval && <p className="ikon-account-message">{t('account.delete.confirm', { days: String(removalGraceDays) })}</p>}

        {removalError && <p className="ikon-account-error">{removalError}</p>}

        <div className="ikon-account-actions">
          {canRequestRemoval && scheduledRemoval && (
            <button type="button" className="ikon-account-button" onClick={() => void cancelAccountRemoval()} disabled={isSubmittingRemoval}>
              {isSubmittingRemoval ? t('account.cancel.submitting') : t('account.cancel.action')}
            </button>
          )}

          {canRequestRemoval && !scheduledRemoval && !isConfirmingRemoval && (
            <button type="button" className="ikon-account-button ikon-account-button-destructive" onClick={() => setIsConfirmingRemoval(true)} disabled={isLoadingAccount}>
              {t('account.delete.action')}
            </button>
          )}

          {canRequestRemoval && !scheduledRemoval && isConfirmingRemoval && (
            <>
              <button
                type="button"
                className="ikon-account-button ikon-account-button-destructive"
                onClick={() => {
                  setIsConfirmingRemoval(false);
                  void requestAccountRemoval();
                }}
                disabled={isSubmittingRemoval}
              >
                {isSubmittingRemoval ? t('account.delete.submitting') : t('account.delete.confirmAction')}
              </button>
              <button type="button" className="ikon-account-button" onClick={() => setIsConfirmingRemoval(false)} disabled={isSubmittingRemoval}>
                {t('account.delete.keep')}
              </button>
            </>
          )}

          {onDismiss && (
            <button type="button" className="ikon-account-dismiss" onClick={onDismiss}>
              {t('account.back')}
            </button>
          )}
        </div>
      </section>
    </main>
  );
}
