import { type IkonUiComponentResolver, type IkonUiModuleLoader, type IkonUiRegistry } from '@ikonai/sdk-react-ui';
import { createLive2DCanvasResolver } from './components/live2d-canvas';

export const IKON_UI_LIVE2D_MODULE = 'live2d';

export function createLive2DResolvers(): IkonUiComponentResolver[] {
  return [createLive2DCanvasResolver()];
}

export const loadLive2DModule: IkonUiModuleLoader = () => createLive2DResolvers();

export function registerLive2DModule(registry: IkonUiRegistry): void {
  registry.registerModule(IKON_UI_LIVE2D_MODULE, loadLive2DModule);
}
