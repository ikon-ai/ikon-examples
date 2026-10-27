import { type IkonUiRegistry } from '@ikonai/sdk-react-ui';
import { createSurfaceProbeResolver } from './surface-probe';

export function registerSurfaceProbeModule(registry: IkonUiRegistry): void {
  registry.registerModule('validation-surface-probe', () => [createSurfaceProbeResolver()]);
}
