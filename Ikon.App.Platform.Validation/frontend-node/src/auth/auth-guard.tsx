import { useRef, useState, type ReactNode } from 'react';
import { useAuth, useAuthGuard, type AuthConfig, type LoginMethod } from '@ikonai/sdk-react-ui';
import { useI18n } from '../i18n/i18n';
import './auth.css';
import { EmailLoginForm } from './email-login-form';
import { LoginButton, RegisterPasskeyButton } from './login-button';

type ErrorScope = 'primary' | 'passkey' | 'email' | 'guest';

export interface AuthGuardProps {
  children: ReactNode;
  config: AuthConfig;
}

export function formatAuthError(error: string): string {
  const trimmed = error.trim();

  try {
    const parsed = JSON.parse(trimmed) as { message?: unknown };

    if (typeof parsed.message === 'string' && parsed.message.length > 0) {
      return parsed.message;
    }
  } catch {
    /* not JSON */
  }

  return trimmed;
}

export function AuthGuard({ children, config }: AuthGuardProps) {
  const { isCheckingAuth, shouldRenderChildren, isLoginPrompt, dismissLoginPrompt } = useAuthGuard({
    config,
    guestUrlParam: 'guest',
  });
  const [errorScope, setErrorScope] = useState<ErrorScope | null>(null);
  const initialCheckDoneRef = useRef(false);

  if (!isCheckingAuth) {
    initialCheckDoneRef.current = true;
  }

  if (isCheckingAuth && !initialCheckDoneRef.current) {
    return null;
  }

  if (!shouldRenderChildren) {
    return (
      <AuthScreen
        config={config}
        errorScope={errorScope}
        setErrorScope={setErrorScope}
        isLoginPrompt={isLoginPrompt}
        onDismiss={dismissLoginPrompt}
      />
    );
  }

  return <>{children}</>;
}

interface AuthScreenProps {
  config: AuthConfig;
  errorScope: ErrorScope | null;
  setErrorScope: (scope: ErrorScope) => void;
  isLoginPrompt: boolean;
  onDismiss: () => void;
}

function AuthScreen({ config, errorScope, setErrorScope, isLoginPrompt, onDismiss }: AuthScreenProps) {
  const { t } = useI18n();
  const { state } = useAuth();

  const primaryMethods = config.methods.filter(
    (m): m is Exclude<LoginMethod, 'email' | 'guest' | 'global' | 'passkey'> => m !== 'email' && m !== 'guest' && m !== 'global' && m !== 'passkey',
  );
  const hasPasskey = config.methods.includes('passkey');
  const hasEmail = config.methods.includes('email');
  const guestProvider = config.methods.includes('global') ? ('global' as const) : config.methods.includes('guest') ? ('guest' as const) : null;
  const hasGuest = guestProvider !== null;

  // An error can arrive with the page itself — an OAuth callback redirecting back after a refused
  // sign-in — in which case no button click has scoped it yet; show it in the primary slot.
  const errorFor = (scope: ErrorScope) =>
    state.error && (errorScope === scope || (errorScope === null && scope === 'primary')) ? (
      <div className="ikon-auth-error">{formatAuthError(state.error)}</div>
    ) : null;

  return (
    <main className="ikon-auth-screen">
      <div className="ikon-aurora-1" />
      <div className="ikon-aurora-2" />

      <section className="ikon-auth-container">
        <h1 className="ikon-auth-title">{t('auth.welcome.title')}</h1>
        <p className="ikon-auth-subtitle">{t('auth.welcome.subtitle')}</p>

        {errorFor('primary')}

        <div className="ikon-auth-buttons">
          {primaryMethods.map((method) => (
            <LoginButton
              key={method}
              provider={method}
              disabled={state.isLoading}
              onAttempt={() => setErrorScope('primary')}
            />
          ))}
        </div>

        {hasPasskey && primaryMethods.length > 0 && (
          <div className="ikon-auth-divider">
            <span>{t('auth.divider')}</span>
          </div>
        )}

        {hasPasskey && (
          <>
            {errorFor('passkey')}
            <div className="ikon-auth-buttons">
              <LoginButton provider="passkey" disabled={state.isLoading} onAttempt={() => setErrorScope('passkey')} />
              <RegisterPasskeyButton disabled={state.isLoading} onAttempt={() => setErrorScope('passkey')} />
            </div>
          </>
        )}

        {hasEmail && (primaryMethods.length > 0 || hasPasskey) && (
          <div className="ikon-auth-divider">
            <span>{t('auth.divider')}</span>
          </div>
        )}

        {hasEmail && <EmailLoginForm config={config} onAttempt={() => setErrorScope('email')} />}

        {hasGuest && (primaryMethods.length > 0 || hasPasskey || hasEmail) && (
          <div className="ikon-auth-divider">
            <span>{t('auth.divider')}</span>
          </div>
        )}

        {hasGuest && (
          <>
            {errorFor('guest')}
            <LoginButton
              provider={guestProvider ?? 'guest'}
              disabled={state.isLoading}
              onAttempt={() => setErrorScope('guest')}
              onClick={isLoginPrompt ? onDismiss : undefined}
            />
          </>
        )}

        {isLoginPrompt && !hasGuest && (
          <button
            type="button"
            className="ikon-auth-dismiss"
            onClick={onDismiss}
            style={{ marginTop: '1rem', background: 'none', border: 'none', color: 'rgba(255, 255, 255, 0.7)', fontSize: '0.875rem', cursor: 'pointer' }}
          >
            {t('auth.dismiss')}
          </button>
        )}
      </section>
    </main>
  );
}
