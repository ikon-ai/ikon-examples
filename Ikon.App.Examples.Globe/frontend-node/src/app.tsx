import './app.css';

import { useEffect } from 'react';
import { AuthProvider, IkonApp, getConnectionRecovery, getOfflineScope, recoverWhenNetworkReturns, useAuthOptional, useIkonApp, useUnattendedRecovery } from '@ikonai/sdk-react-ui';
import { registerStandardUiModule, registerLucideIconsModule } from '@ikonai/sdk-react-ui-standard';
import { registerGlobeModule } from './lib/webgl-globe';
import { AuthGuard } from './auth/auth-guard';
import { authConfig } from './env';
import { I18nProvider, useI18n } from './i18n/i18n';
import { en } from './i18n/en';

function App() {
  return (
    <I18nProvider translations={{ en }}>
      <AuthProvider config={authConfig}>
        <AuthGuard config={authConfig}>
          <AuthorizedApp />
        </AuthGuard>
      </AuthProvider>
    </I18nProvider>
  );
}

function AuthorizedApp() {
  const app = useIkonApp({
    modules: [registerStandardUiModule, registerLucideIconsModule, registerGlobeModule],
  });

  return (
    <IkonApp
      {...app}
      connectingOverlay={(isSlow) => (isSlow ? <ConnectingOverlay /> : null)}
      reconnectingOverlay={<ReconnectingOverlay />}
      offlineOverlay={<OfflineOverlay isServerFull={app.isServerFull} isSessionExpired={app.isSessionExpired} isStartupFailed={app.isStartupFailed} isUnattended={app.isUnattended} />}
      accessDeniedScreen={(reason) => <AccessDeniedScreen reason={reason} />}
    />
  );
}

function ConnectingOverlay() {
  const { t } = useI18n();
  return (
    <div className="ikon-connecting-overlay">
      <div className="ikon-connecting-chip">
        <div className="ikon-connecting-spinner" />
        <span>{t('connection.connecting')}</span>
      </div>
    </div>
  );
}

function ReconnectingOverlay() {
  const { t } = useI18n();
  return (
    <div className="ikon-reconnecting-overlay">
      <div className="ikon-reconnecting-chip">
        <div className="ikon-reconnecting-spinner" />
        <span>{t('connection.reconnecting')}</span>
      </div>
    </div>
  );
}

function OfflineOverlay({ isServerFull, isSessionExpired, isStartupFailed, isUnattended }: { isServerFull: boolean; isSessionExpired: boolean; isStartupFailed: boolean; isUnattended: boolean }) {
  const { t } = useI18n();

  const scope = getOfflineScope({ isServerFull, isSessionExpired, isStartupFailed });
  const message = t(`connection.${scope}.message`);
  const recovery = getConnectionRecovery({ isSessionExpired });
  const { secondsLeft, isChecking } = useUnattendedRecovery({ enabled: isUnattended, scope, recover: recovery.run });
  const retry = isChecking ? t('connection.retrying') : secondsLeft === null ? '' : secondsLeft >= 120 ? t('connection.retryInMinutes', { minutes: String(Math.ceil(secondsLeft / 60)) }) : t('connection.retryInSeconds', { seconds: String(secondsLeft) });

  // Only the plain disconnect reloads when the network returns; a full server, ended session or
  // failed startup would come straight back to the same overlay.
  useEffect(() => (scope === 'offline' ? recoverWhenNetworkReturns(recovery.run) : undefined), [scope, recovery.run]);

  return (
    <div className="ikon-offline-overlay">
      <div className="ikon-offline-chip">
        <span className="ikon-offline-title">{t(`connection.${scope}.title`)}</span>
        {message && <span className="ikon-offline-message">{message}</span>}
        {retry && <span className="ikon-offline-message">{retry}</span>}
        <button type="button" className="ikon-offline-action" onClick={recovery.run}>
          <span className="ikon-offline-action-label">{t(`connection.${scope}.action`)}</span>
        </button>
      </div>
    </div>
  );
}

function AccessDeniedScreen({ reason }: { reason: string }) {
  const { t } = useI18n();
  const auth = useAuthOptional();
  const message = t('connection.accessDenied.message');
  return (
    <main className="ikon-surface ikon-auth-screen">
      <section className="ikon-auth-container">
        <h1 className="ikon-auth-title">{t('connection.accessDenied.title')}</h1>
        {message && <p className="ikon-auth-subtitle">{message}</p>}
        <div className="ikon-auth-error">{reason}</div>
        {auth && (
          <button type="button" className="ikon-auth-email-button" onClick={auth.logout}>
            {t('connection.accessDenied.backToLogin')}
          </button>
        )}
      </section>
    </main>
  );
}

export default App;
