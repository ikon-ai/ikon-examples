import { createLazyResolver, type IkonUiRegistry } from '@ikonai/sdk-react-ui';
import { createAccountPanelResolver } from './account-panel';
import { createConsentPanelResolver } from './consent-panel';

export function registerIdentityModule(registry: IkonUiRegistry): void {
  registry.registerModule('validation-identity', () => [
    createConsentPanelResolver(),
    createAccountPanelResolver(),
    // The probe pulls in the capture hooks and VideoStreamView, which only the Identity tab needs,
    // so it ships as its own chunk fetched the first time the node renders.
    createLazyResolver('validation-sdk-probe', () => import('./sdk-probe'), (probe) => probe.createSdkProbeResolver),
  ]);
}
