namespace Ikon.Parallax.Components.ImageEditor
  static class ImageEditorExtensions
    // triggerSave/triggerUndo/triggerRedo are edge-triggered — increment the value to fire that action.
    // brushColor: Hex color, e.g. "#ff0000".
    // tool: Defaults to ImageEditorTool.Brush on the frontend.
    // zoom: Zoom level: 1.0 = 100%.
    // highResolution: Keeps the canvas at the image's native resolution (capped): sharp zoom, full-quality export, but capped undo history. When false the canvas is downscaled to fit its container.
    // fillShapes: When true, the region, lasso and polygon tools fill the drawn shape with the brush color instead of stroking its outline. Defaults to false on the frontend.
    // overlaySrc: Non-editable reference image drawn above the canvas; never included in the saved image.
    // overlayOpacity: Opacity of overlaySrc, 0–1; null = 1.
    // textMaxLength: Max length of the text tool's floating input; null = no limit.
    // textFontSize: Font size in pixels; null = derived from brush width.
    // textPadding: Padding in pixels around the text; null = 4.
    // onSave: Receives the saved image as base64 data.
    static void ImageEditorCanvas(this UIView view, string[]? style = null, string? src = null, int? brushWidth = null, string? brushColor = null, ImageEditorTool? tool = null, double? zoom = null, bool? highResolution = null, bool? fitContainer = null, bool? fillShapes = null, string? overlaySrc = null, double? overlayOpacity = null, int? textMaxLength = null, int? textFontSize = null, int? textPadding = null, Func<ImageEditorSaveArgs, Task>? onSave = null, Func<ImageEditorHistoryArgs, Task>? onHistoryChange = null, int? triggerSave = null, int? triggerUndo = null, int? triggerRedo = null, string? styleId = null, string? key = null)
  sealed record ImageEditorHistoryArgs
    ctor(bool CanUndo, bool CanRedo)
    bool CanRedo { get; init; }
    bool CanUndo { get; init; }
  sealed record ImageEditorSaveArgs
    ctor(string ImageData)
    string ImageData { get; init; }
  enum ImageEditorTool
    Brush
    Eraser
    Text
    Arrow
    Region
    Lasso
    Line
    Polygon
