import { type IkonUiRegistry } from '@ikonai/sdk-react-ui';
import { createUploadLabResolver } from './upload-lab';

export function registerUploadLabModule(registry: IkonUiRegistry): void {
  registry.registerModule('upload-lab', () => [createUploadLabResolver()]);
}
