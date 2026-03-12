import { type IkonUiRegistry } from '@ikonai/sdk-react-ui';
import { createFunctionTesterResolver } from './components/function-tester';

export function registerFunctionTesterModule(registry: IkonUiRegistry): void {
  registry.registerModule('function-tester', () => [createFunctionTesterResolver()]);
}
