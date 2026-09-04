# Component Vocabulary

## Component Vocabulary

The enums and records the components take and hand back. Every one is in
`Ikon.Parallax.Components.Standard` unless noted.

**Charts.** `BarChart` takes `BarLayout` (`Vertical`/`Horizontal`) and `BarGroupMode`
(`Stacked`/`Grouped`); `LineChart` takes a `LineCurve` interpolation (`LineCurve.Linear`,
`LineCurve.MonotoneX`, `LineCurve.Step`, `LineCurve.StepBefore`, `LineCurve.StepAfter`,
`LineCurve.Cardinal`, `LineCurve.Basis`) and a `ScaleType` (`Point`, `Linear`, `Time`, `Log`) per
axis.
`LegendConfig` places the legend with `LegendAnchor` and `LegendDirection`; `ChartCrosshairStyle`
with `CrosshairType` draws the hover guides. The look is a tree of style records —
`ChartAxisStyle`, `ChartGridStyle`, `ChartLabelsStyle`, `ChartLegendStyle`, all built from
`ChartTextStyle` (color, family, size). A click yields `ChartClickArgs` naming the series, the
index value and the datum.

**Action buttons.** `ActionButton` reports through `ActionEvent` (`ActionType`, `Success`), which
the richer kinds subclass: `ImageCaptureActionEvent` carries the image bytes and dimensions,
`LocationActionEvent` latitude/longitude/accuracy, `ContactsActionEvent` the picked `ClientContact`
list. Options are `ActionOptions` subclasses — `CaptureImageActionOptions` (with `CaptureImageMode`:
`Native` for the OS camera UI, `Headless` for a silent single-frame grab, plus
`CaptureImageConstraints` and its `CameraFacing`) and `PickContactsActionOptions`. A permission
prompt's outcome arrives as `MediaPermissionEvent`. A handler that receives a typed value takes
`ActionArgs<T>`, which pairs the value with the calling client's `Context`.

**Drag and drop.** `SortableContext` reports `DragStartArgs`, `DragMoveArgs` (with the pointer
delta), `DragOverArgs`, `DragEndArgs` (active and drop-target ids) and `DragCancelArgs`. `DragDrop`
holds the theme slots for the container, the drop zones, the drag overlay, and the `DragDrop.Item`
and `DragDrop.Droppable` states.

**Overlays.** Dialog, Popover, Sheet and friends let you veto dismissal: `EscapeKeyDownArgs`,
`PointerDownOutsideArgs`, `FocusOutsideArgs` and `InteractOutsideArgs`, the last three naming the
outside target. `ActivationMode` chooses whether a tab or menu trigger activates on focus
(`Automatic`) or only on click (`Manual`).

**Toasts.** A queued toast is a `ToastItem` — id, title, description, `SemanticTone`, and the
auto-dismiss `DurationMs` the client counts down. `ToastType` picks `Foreground` or `Background`
ARIA urgency, and a swipe-to-dismiss gesture reports `ToastSwipeArgs` with a `ToastSwipeDirection`.

**Carousel.** Slides are `CarouselSlideItem` records (a build lambda plus an optional key).
`CarouselAlign` sets snap alignment, and `CarouselBreakpoint` varies slides-per-view,
slides-per-group and gap by container width.

**Tables, forms and inputs.** A `DataTable` column declares a `CellType` (`Text`, `Badge`,
`Action`, `Actions`, `Checkbox`); `Action` cells take `CellAction` records. `CheckedState` is the
tri-state a checkbox can hold (`Unchecked`, `Checked`, `Indeterminate`). `FormMessage` fires on a
`FormMessageMatch` (`FormMessageMatch.ValueMissing`, `FormMessageMatch.TooShort`,
`FormMessageMatch.PatternMismatch`, …). `ColorPicker` emits its value
in a `ColorFormat` (`Hex`, `Rgb`, `Hsl`); `DatePicker` takes a `WeekStart` and `TimePicker` an
`HourFormat`; `Badge` takes a `BadgeSize`. `TreeView` expansion lives in an `ExpandedSet` you hold
as a field and drive with `Expand`/`Collapse`/`Toggle`.

**Scrolling and feeds.** `InfiniteScrollView` and `FeedScroller` report `ScrollNearEndArgs` — the
scroll offsets plus the `ScrollDirection` the user was moving — which is the cue to load the next
page.

**Theming.** Alongside the per-component slot classes, `OnSurface` carries the text tones that read
correctly on each surface, `ImageCard` the slots of a captioned image tile, `Sticky` whether a
sticky header detaches partially or always, and `ZIndex` the platform's layering scale.
`ThemeVocabulary` is the alias table itself — every shorthand theme key and the canonical CSS
variables it expands to, which the renderer, the styling tools and the drift tests all read.

**The tree itself.** `UIViewNode` is a built node: type, id, props, style ids and children.
Rendering never hands you one; it is what the differ walks, and reading it is for tests and tooling.
Treat it as immutable — it is shared by reference into the subtree cache.
