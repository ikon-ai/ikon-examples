import { type IkonUiComponentResolver, type IkonUiModuleLoader, type IkonUiRegistry } from '@ikonai/sdk-react-ui';
import { createWebGLGlobeResolver } from './components/webgl-globe';

export const IKON_UI_GLOBE_MODULE = 'webgl-globe';

export function createGlobeResolvers(): IkonUiComponentResolver[] {
  return [createWebGLGlobeResolver()];
}

export const loadGlobeModule: IkonUiModuleLoader = () => createGlobeResolvers();

export function registerGlobeModule(registry: IkonUiRegistry): void {
  registry.registerModule(IKON_UI_GLOBE_MODULE, loadGlobeModule);
}
