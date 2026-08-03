import type { Translations } from './i18n';

export const en: Translations = {
  // Connection states
  'connection.connecting': 'Connecting...',
  'connection.reconnecting': 'Reconnecting...',
  'connection.offline.title': 'Connection lost',
  'connection.offline.message': 'Refresh to continue',
  'connection.serverFull.title': 'App is at capacity',
  'connection.serverFull.message': 'Too many people are using this app right now. Please try again in a little while.',
  'connection.sessionExpired.title': 'Session expired',
  'connection.sessionExpired.message': 'The session this link points to has ended. Open the app without the link to start a new one.',

  // Auth screen
  'auth.welcome.title': 'Welcome',
  'auth.welcome.subtitle': 'Sign in to continue',
  'auth.divider': 'or',
  'auth.dismiss': 'Not now',

  // Email login form
  'auth.email.placeholder': 'Enter your email',
  'auth.email.submit': 'Send code',
  'auth.email.submitting': 'Sending...',
  'auth.email.sent.message': 'We sent a code to',
  'auth.email.code.placeholder': 'Enter code',
  'auth.email.code.submit': 'Verify',
  'auth.email.code.submitting': 'Verifying...',
  'auth.email.code.back': 'Use a different email',
  'auth.email.error.empty': 'Please enter your email',
  'auth.email.error.noSpaceId': 'Space ID is not configured',
  'auth.email.error.sendFailed': 'Could not send the code. Please try again.',
  'auth.email.error.codeEmpty': 'Please enter the code',
  'auth.email.error.verifyFailed': 'Could not verify the code. Please try again.',

  // Access denied
  'connection.accessDenied.title': 'Access Denied',
  'connection.accessDenied.message': 'You do not have permission to access this app.',
  'connection.accessDenied.backToLogin': 'Back to login',

  // Login buttons
  'auth.button.guest': 'Continue as Guest',
  'auth.button.provider': 'Continue with {provider}',
  'auth.button.registerPasskey': 'Register with Passkey',
};
