import { type IkonUiComponentResolver, type IkonUiModuleLoader, type IkonUiRegistry } from '@ikonai/sdk-react-ui';
import { createMomentumMapResolver } from './components/momentum-map';

export const IKON_UI_MOMENTUM_MAP_MODULE = 'momentum-map';

export function createMomentumMapResolvers(): IkonUiComponentResolver[] {
  return [createMomentumMapResolver()];
}

export const loadMomentumMapModule: IkonUiModuleLoader = () => createMomentumMapResolvers();

export function registerMomentumMapModule(registry: IkonUiRegistry): void {
  registry.registerModule(IKON_UI_MOMENTUM_MAP_MODULE, loadMomentumMapModule);
}
