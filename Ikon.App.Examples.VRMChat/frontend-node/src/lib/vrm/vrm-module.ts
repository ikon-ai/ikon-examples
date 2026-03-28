import { type IkonUiComponentResolver, type IkonUiModuleLoader, type IkonUiRegistry } from '@ikonai/sdk-react-ui';
import { createVRMCanvasResolver } from './components/vrm-canvas';

export const IKON_UI_VRM_MODULE = 'vrm';

export function createVRMResolvers(): IkonUiComponentResolver[] {
  return [createVRMCanvasResolver()];
}

export const loadVRMModule: IkonUiModuleLoader = () => createVRMResolvers();

export function registerVRMModule(registry: IkonUiRegistry): void {
  registry.registerModule(IKON_UI_VRM_MODULE, loadVRMModule);
}
