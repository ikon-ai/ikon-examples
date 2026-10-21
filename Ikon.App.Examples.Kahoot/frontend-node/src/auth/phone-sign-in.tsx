import { useState } from 'react';
import { useAuth, usePhoneSignIn } from '@ikonai/sdk-react-ui';
import { useI18n } from '../i18n/i18n';

interface PhoneSignInProps {
  disabled?: boolean;
  onAttempt?: () => void;
}

const PHONE_ICON = (
  <svg viewBox="0 0 24 24" width="20" height="20" fill="currentColor">
    <path d="M17 1.01 7 1c-1.1 0-2 .9-2 2v18c0 1.1.9 2 2 2h10c1.1 0 2-.9 2-2V3c0-1.1-.9-1.99-2-1.99zM17 19H7V5h10v14z" />
  </svg>
);

export function PhoneSignIn({ disabled, onAttempt }: PhoneSignInProps) {
  const { t } = useI18n();
  const [showing, setShowing] = useState(false);
  const phoneSignIn = usePhoneSignIn(showing);

  if (!showing) {
    return (
      <div className="ikon-auth-buttons">
        <button
          type="button"
          className="ikon-auth-login-button ikon-auth-login-button-phone"
          disabled={disabled}
          onClick={() => {
            onAttempt?.();
            setShowing(true);
          }}
        >
          {PHONE_ICON}
          {t('auth.phone.button')}
        </button>
      </div>
    );
  }

  if (phoneSignIn?.status === 'failed' || phoneSignIn?.status === 'denied') {
    return (
      <div className="ikon-auth-phone">
        {phoneSignIn.status === 'failed' ? <div className="ikon-auth-error">{phoneSignIn.error}</div> : <p className="ikon-auth-phone-hint">{t('auth.phone.denied')}</p>}
        <button type="button" className="ikon-auth-email-button" onClick={() => setShowing(false)}>
          {t('auth.phone.back')}
        </button>
      </div>
    );
  }

  if (phoneSignIn?.status !== 'waiting') {
    return (
      <div className="ikon-auth-phone">
        <span className="ikon-auth-email-spinner" />
      </div>
    );
  }

  return (
    <div className="ikon-auth-phone">
      <img className="ikon-auth-phone-qr" src={`data:image/svg+xml;charset=utf-8,${encodeURIComponent(phoneSignIn.qrSvg)}`} alt={t('auth.phone.qrAlt')} />
      <span className="ikon-auth-phone-code">{phoneSignIn.confirmationCode}</span>
      <p className="ikon-auth-phone-hint">{t('auth.phone.hint')}</p>
    </div>
  );
}

export function HandoffPrompt() {
  const { t } = useI18n();
  const { handoffPrompt, answerHandoff, dismissHandoff } = useAuth();
  const [answering, setAnswering] = useState(false);

  if (!handoffPrompt) {
    return null;
  }

  const answer = async (approve: boolean) => {
    setAnswering(true);
    await answerHandoff(approve);
    setAnswering(false);
  };

  return (
    <div className="ikon-surface ikon-handoff-backdrop" role="dialog" aria-modal="true">
      <section className="ikon-auth-container">
        {handoffPrompt.status === 'asking' ? (
          <>
            <h1 className="ikon-auth-title">{t('auth.handoff.title')}</h1>
            <p className="ikon-auth-subtitle">{handoffPrompt.request.browser ?? new URL(handoffPrompt.request.appOrigin).host}</p>
            <div className="ikon-auth-phone">
              <span className="ikon-auth-phone-code">{handoffPrompt.request.confirmationCode}</span>
              <p className="ikon-auth-phone-hint">{t('auth.handoff.hint')}</p>
              <button type="button" className="ikon-auth-email-button" disabled={answering} onClick={() => void answer(true)}>
                {t('auth.handoff.approve')}
              </button>
              <button type="button" className="ikon-auth-dismiss" disabled={answering} onClick={() => void answer(false)}>
                {t('auth.handoff.deny')}
              </button>
            </div>
          </>
        ) : (
          <>
            <p className="ikon-auth-subtitle">
              {handoffPrompt.status === 'approved' ? t('auth.handoff.approved') : handoffPrompt.status === 'expired' ? t('auth.handoff.expired') : handoffPrompt.error}
            </p>
            <button type="button" className="ikon-auth-email-button" onClick={dismissHandoff}>
              {t('auth.handoff.close')}
            </button>
          </>
        )}
      </section>
    </div>
  );
}
