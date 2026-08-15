# Live2D Model Assets

This directory should contain the Live2D model files for the Shizuku character (or another Live2D model).

## Getting the Shizuku Model

You can download the Shizuku sample model from:
- Live2D Cubism SDK Samples: https://www.live2d.com/en/download/sample-data/
- Or from the pixi-live2d-display demos

## Required Files

Place the following files in this directory:
- `shizuku.model3.json` - Main model definition file
- `shizuku.moc3` - Model data file
- `shizuku.physics3.json` - Physics settings (if available)
- `shizuku.pose3.json` - Pose settings (if available)
- `shizuku.cdi3.json` - Display info (if available)
- `textures/` - Directory containing texture PNG files
- `motions/` - Directory containing motion files (.motion3.json)
- `expressions/` - Directory containing expression files

## Alternative Models

You can use any Live2D Cubism 3/4 model. Just update the model path in `Live2DChat.cs`:

```csharp
view.Live2DCanvas(
    source: "/models/your-model/your-model.model3.json",
    ...
);
```

## Model Resources

- Live2D Free Materials: https://www.live2d.com/en/learn/sample/
- pixi-live2d-display: https://github.com/guansss/pixi-live2d-display
