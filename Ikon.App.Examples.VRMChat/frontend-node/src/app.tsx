import './app.css';

import { AuthProvider, IkonApp, useAuthOptional, useIkonApp, useLazyFont } from '@ikonai/sdk-react-ui';
import { registerStandardUiModule, registerLucideIconsModule } from '@ikonai/sdk-react-ui-standard';
import { registerVRMModule } from './lib/vrm';
import { AuthGuard } from './auth/auth-guard';
import { authConfig } from './env';
import { I18nProvider, useI18n } from './i18n/i18n';
import { en } from './i18n/en';

function App() {
  if (authConfig.enabled) {
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

  return (
    <I18nProvider translations={{ en }}>
      <AuthorizedApp />
    </I18nProvider>
  );
}

function AuthorizedApp() {
  const loadFont = useLazyFont('https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@400;500;600;700&display=swap');
  const app = useIkonApp({
    modules: [registerStandardUiModule, registerLucideIconsModule, registerVRMModule],
  });

  return (
    <IkonApp
      {...app}
      connectingOverlay={(isSlow) => (isSlow ? <ConnectingOverlay loadFont={loadFont} /> : null)}
      reconnectingOverlay={<ReconnectingOverlay loadFont={loadFont} />}
      offlineOverlay={(error) => <OfflineOverlay error={error} isServerFull={app.isServerFull} loadFont={loadFont} />}
      accessDeniedScreen={(reason) => <AccessDeniedScreen reason={reason} loadFont={loadFont} />}
    />
  );
}

function ConnectingOverlay({ loadFont }: { loadFont: () => void }) {
  const { t } = useI18n();
  loadFont();
  return (
    <div className="ikon-connecting-overlay">
      <div className="ikon-connecting-chip">
        <div className="ikon-connecting-spinner" />
        <span>{t('connection.connecting')}</span>
      </div>
    </div>
  );
}

function ReconnectingOverlay({ loadFont }: { loadFont: () => void }) {
  const { t } = useI18n();
  loadFont();
  return (
    <div className="ikon-reconnecting-overlay">
      <div className="ikon-reconnecting-chip">
        <div className="ikon-reconnecting-spinner" />
        <span>{t('connection.reconnecting')}</span>
      </div>
    </div>
  );
}

function OfflineOverlay({ error, isServerFull, loadFont }: { error: string | null; isServerFull: boolean; loadFont: () => void }) {
  const { t } = useI18n();
  loadFont();

  const scope = isServerFull ? 'serverFull' : 'offline';
  return (
    <div className="ikon-offline-overlay">
      <div className="ikon-offline-chip">
        <span className="ikon-offline-title">{t(`connection.${scope}.title`)}</span>
        <span className="ikon-offline-message">{t(`connection.${scope}.message`)}</span>
        {!isServerFull && error && <span className="ikon-offline-error">{error}</span>}
      </div>
    </div>
  );
}

function AccessDeniedScreen({ reason, loadFont }: { reason: string; loadFont: () => void }) {
  const { t } = useI18n();
  const auth = useAuthOptional();
  loadFont();
  return (
    <main className="ikon-app">
      <div className="ikon-aurora-1" />
      <div className="ikon-aurora-2" />
      <section className="ikon-hero">
        <h1>{t('connection.accessDenied.title')}</h1>
        <p className="ikon-info">{t('connection.accessDenied.message')}</p>
        <p className="ikon-error">{reason}</p>
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
