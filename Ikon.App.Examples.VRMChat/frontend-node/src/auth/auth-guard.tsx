import type { ReactNode } from 'react';
import { useAuth, useAuthGuard, type AuthConfig, type LoginMethod } from '@ikonai/sdk-react-ui';
import { useI18n } from '../i18n/i18n';
import './auth.css';
import { EmailLoginForm } from './email-login-form';
import { LoginButton } from './login-button';

export interface AuthGuardProps {
  children: ReactNode;
  config: AuthConfig;
}

export function AuthGuard({ children, config }: AuthGuardProps) {
  const { isCheckingAuth, shouldRenderChildren } = useAuthGuard({
    config,
    guestUrlParam: 'guest',
  });

  if (isCheckingAuth) {
    return null;
  }

  if (!shouldRenderChildren) {
    return <AuthScreen config={config} />;
  }

  return <>{children}</>;
}

interface AuthScreenProps {
  config: AuthConfig;
}

function AuthScreen({ config }: AuthScreenProps) {
  const { t } = useI18n();
  const { state } = useAuth();

  const oauthMethods = config.methods.filter((m): m is Exclude<LoginMethod, 'email' | 'guest'> => m !== 'email' && m !== 'guest');
  const hasEmail = config.methods.includes('email');
  const hasGuest = config.methods.includes('guest');

  return (
    <main className="ikon-auth-screen">
      <div className="ikon-aurora-1" />
      <div className="ikon-aurora-2" />

      <section className="ikon-auth-container">
        <h1 className="ikon-auth-title">{t('auth.welcome.title')}</h1>
        <p className="ikon-auth-subtitle">{t('auth.welcome.subtitle')}</p>

        {state.error && <div className="ikon-auth-error">{state.error}</div>}

        <div className="ikon-auth-buttons">
          {oauthMethods.map((method) => (
            <LoginButton key={method} provider={method} disabled={state.isLoading} />
          ))}
        </div>

        {hasEmail && oauthMethods.length > 0 && (
          <div className="ikon-auth-divider">
            <span>{t('auth.divider')}</span>
          </div>
        )}

        {hasEmail && <EmailLoginForm config={config} />}

        {hasGuest && (oauthMethods.length > 0 || hasEmail) && (
          <div className="ikon-auth-divider">
            <span>{t('auth.divider')}</span>
          </div>
        )}

        {hasGuest && <LoginButton provider="guest" disabled={state.isLoading} />}
      </section>
    </main>
  );
}
