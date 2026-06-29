import { type IkonUiRegistry } from '@ikonai/sdk-react-ui';
import { createTpProbeResolver } from './tp-probe';

export function registerTpProbeModule(registry: IkonUiRegistry): void {
  registry.registerModule('tp-probe', () => [createTpProbeResolver()]);
}
