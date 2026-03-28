import { type IkonUiComponentResolver, type IkonUiModuleLoader, type IkonUiRegistry } from '@ikonai/sdk-react-ui';
import { createAmbientVideoResolver } from './components/ambient-video-player';

export const IKON_UI_AMBIENT_VIDEO_MODULE = 'ambient-video';

export function createAmbientVideoResolvers(): IkonUiComponentResolver[] {
  return [createAmbientVideoResolver()];
}

export const loadAmbientVideoModule: IkonUiModuleLoader = () => createAmbientVideoResolvers();

export function registerAmbientVideoModule(registry: IkonUiRegistry): void {
  registry.registerModule(IKON_UI_AMBIENT_VIDEO_MODULE, loadAmbientVideoModule);
}
