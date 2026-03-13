# Custom UI Components

## Custom UI Components

When the built-in Ikon.Parallax components are not sufficient, you can integrate any React library as a custom UI component. This requires changes in both the frontend and the C# app.

### Overview

The integration has four parts:

1. **React component** - Implements the UI using any React/JS library
2. **Component resolver** - Tells the Ikon UI system how to render your component type
3. **Module registration** - Registers the resolver with the `useIkonApp` hook
4. **C# extension method** - Provides a typed API for using the component from C# code

### Step 1: React Component and Resolver

Create your React component and a resolver function that maps a node type string to it.

```typescript
// frontend-node/src/lib/my-component/components/my-component.tsx
import { memo } from 'react';
import { type IkonUiComponentResolver, type UiComponentRendererProps, useUiNode } from '@ikonai/sdk-react-ui';

const MyComponentRenderer = memo(function MyComponentRenderer({ nodeId, context, className }: UiComponentRendererProps) {
  const node = useUiNode(context.store, nodeId);
  if (!node) return null;

  const someProp = node.props?.['someProp'] as string | undefined;
  const onClickId = node.props?.['onClickId'] as string | undefined;

  return (
    <div
      className={className}
      onClick={onClickId ? () => context.dispatchAction(onClickId, { clicked: true }) : undefined}
    >
      {someProp}
      {context.renderChildren(node.children ?? [])}
    </div>
  );
});

export function createMyComponentResolver(): IkonUiComponentResolver {
  return (node) => {
    if (node.type !== 'my-component') return undefined;
    return MyComponentRenderer;
  };
}
```

### Step 2: Module Registration

Create a module that registers your resolver with the Ikon UI registry.

```typescript
// frontend-node/src/lib/my-component/my-component-module.ts
import { type IkonUiModuleLoader, type IkonUiRegistry } from '@ikonai/sdk-react-ui';
import { createMyComponentResolver } from './components/my-component';

export const loadMyComponentModule: IkonUiModuleLoader = () => [createMyComponentResolver()];

export function registerMyComponentModule(registry: IkonUiRegistry): void {
  registry.registerModule('my-component', loadMyComponentModule);
}
```

Then add the module to `useIkonApp` in `app.tsx`:

```typescript
const app = useIkonApp({
  modules: [registerStandardUiModule, registerLucideIconsModule, registerMyComponentModule],
});
```

### Step 3: C# Extension Method

Create an extension method on `UIView` that calls `view.AddNode` with your component type and a props dictionary. Use `view.CreateAction<T>` to create action IDs for callbacks.

```csharp
public static class MyComponentExtensions
{
    public static void MyComponent(
        this UIView view,
        string someProp,
        Func<MyClickEventArgs, Task>? onClick = null,
        string[]? style = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        string? onClickId = null;

        if (onClick != null)
        {
            onClickId = view.CreateAction<MyClickEventArgs>(args => onClick(args.Value));
        }

        view.AddNode(
            "my-component",
            new Dictionary<string, object?>
            {
                ["someProp"] = someProp,
                ["onClickId"] = onClickId
            },
            style: style,
            file: file,
            line: line);
    }
}
```

### Step 4: Use from C#

```csharp
view.MyComponent("Hello from custom component",
    onClick: async args => { Log.Instance.Info("Clicked!"); },
    style: ["w-full rounded-lg"]);
```

### Key Concepts

- **Node type string** - The string (e.g. `"my-component"`) must match between the C# `view.AddNode` call and the TypeScript resolver check
- **Props dictionary** - C# sends a `Dictionary<string, object?>` that maps directly to React component props
- **Action IDs** - C# creates action IDs via `view.CreateAction<T>()`, passes them as props, and the React component calls `context.dispatchAction(actionId, payload)` to send events back
- **Crosswind styles** - Style arrays passed from C# via `view.AddNode` work with custom components just like built-in components
