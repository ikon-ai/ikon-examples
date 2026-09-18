import { SignInLegalLine } from '@ikonai/sdk-react-ui';
import { useI18n } from '../i18n/i18n';

/**
 * The sign-in screen's legal attribution, in this app's language.
 *
 * Every sign-in surface an end user sees carries it — the wall in auth-guard.tsx, the same screen
 * raised on demand by ClientFunctions.LoginShowAsync, and any sign-in of your own. Render this
 * component in yours too.
 *
 * The links point at `/legal/privacy` and `/legal/terms` on this app's own domain, which is where
 * the platform serves the app's notice and terms. Point them elsewhere by setting
 * `window.__IKON_LEGAL_CONFIG__`, or pass `legal` here.
 */
export function SignInLegalAttribution() {
  const { t } = useI18n();

  return (
    <SignInLegalLine
      labels={{
        attribution: t('legal.attribution'),
        responsibility: t('legal.responsibility'),
        privacyNotice: t('legal.privacyNotice'),
        terms: t('legal.terms'),
        ikonPrivacyPolicy: t('legal.ikonPrivacyPolicy'),
      }}
    />
  );
}
