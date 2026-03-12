public partial class Validation
{
    private void RenderDragDropSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Sortable List
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Sortable List");
                view.Text([Text.Caption, "mb-4"], "Drag items to reorder with auto-handled reordering");

                view.Column([Layout.Column.Lg], content: view =>
                {
                    view.Text([Text.BodyStrong], "Vertical (default)");
                    view.SortableList(
                        items: _sortableSimpleVertical.Value,
                        strategy: SortStrategy.VerticalList,
                        onReorder: async args => _sortableSimpleVertical.Value = args.NewOrder);

                    view.Text([Text.BodyStrong, "mt-4"], "Horizontal");
                    view.SortableList(
                        items: _sortableSimpleHorizontal.Value,
                        strategy: SortStrategy.HorizontalList,
                        onReorder: async args => _sortableSimpleHorizontal.Value = args.NewOrder);

                    view.Text([Text.BodyStrong, "mt-4"], "With custom content");
                    view.SortableList(
                        items: _sortableSimpleVertical.Value,
                        strategy: SortStrategy.VerticalList,
                        onReorder: async args => _sortableSimpleVertical.Value = args.NewOrder,
                        itemContent: (v, item) =>
                        {
                            v.Row([Layout.Row.InlineCenter], content: v =>
                            {
                                v.Icon([Icon.Default], name: "star");
                                v.Text([Text.BodyStrong], item);
                                v.Text([Text.Caption, "ml-auto"], "(custom)");
                            });
                        });
                });
            });

            // Draggable and Droppable
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Draggable and Droppable");
                view.Text([Text.Caption, "mb-4"], "Independent draggable elements and drop zones");

                view.DndContext(
                    collisionDetection: CollisionDetection.RectIntersection,
                    onDragStart: async args => _dndStatus.Value = $"Dragging: {args.ActiveId}",
                    onDragOver: async args => _dndStatus.Value = $"Over: {args.OverId ?? "none"}",
                    onDragEnd: async args =>
                    {
                        _dndStatus.Value = $"Dropped {args.ActiveId} on {args.OverId ?? "nothing"}";

                        if (!_draggableItemZones.Value.ContainsKey(args.ActiveId))
                        {
                            return;
                        }

                        var zones = new Dictionary<string, string?>(_draggableItemZones.Value);

                        if (!string.IsNullOrEmpty(args.OverId) && args.OverId.StartsWith("drop-zone-") && args.OverId != "drop-disabled")
                        {
                            zones[args.ActiveId] = args.OverId;
                        }
                        else if (args.OverId == "draggables-source")
                        {
                            zones[args.ActiveId] = null;
                        }

                        _draggableItemZones.Value = zones;
                    },
                    onDragCancel: async () => _dndStatus.Value = "Drag cancelled",
                    content: view =>
                    {
                        view.Row(["gap-8 flex-wrap"], content: view =>
                        {
                            // Draggables column - wrapped in droppable to allow returning items
                            view.Droppable(
                                [DragDrop.Droppable.Default, "min-h-[100px] flex-1 min-w-[200px]"],
                                id: "draggables-source",
                                content: sourceView =>
                                {
                                    sourceView.Column(["gap-2"], content: colView =>
                                    {
                                        colView.Text([Text.BodyStrong], "Draggable Items");

                                        var availableItems = _draggableItemZones.Value
                                            .Where(kvp => kvp.Value == null)
                                            .Select(kvp => kvp.Key);

                                        foreach (var itemId in availableItems)
                                        {
                                            RenderDraggableItem(colView, itemId);
                                        }
                                    });
                                });

                            // Droppables column
                            view.Column(["gap-2", "flex-1", "min-w-[200px]"], content: view =>
                            {
                                view.Text([Text.BodyStrong], "Drop Zones");
                                view.Droppable(
                                    [DragDrop.Droppable.Success, "min-h-[60px]"],
                                    id: "drop-zone-1",
                                    content: v =>
                                    {
                                        v.Text([Text.Body], "Drop Zone A");
                                        var droppedItems = _draggableItemZones.Value
                                            .Where(kvp => kvp.Value == "drop-zone-1")
                                            .Select(kvp => kvp.Key);

                                        foreach (var itemId in droppedItems)
                                        {
                                            RenderDraggableItem(v, itemId);
                                        }
                                    });

                                view.Droppable(
                                    [DragDrop.Droppable.Info, "min-h-[60px]"],
                                    id: "drop-zone-2",
                                    data: new { accepts = "special" },
                                    content: v =>
                                    {
                                        v.Text([Text.Body], "Drop Zone B (with data)");
                                        var droppedItems = _draggableItemZones.Value
                                            .Where(kvp => kvp.Value == "drop-zone-2")
                                            .Select(kvp => kvp.Key);

                                        foreach (var itemId in droppedItems)
                                        {
                                            RenderDraggableItem(v, itemId);
                                        }
                                    });

                                view.Droppable(
                                    [DragDrop.Droppable.Disabled],
                                    id: "drop-disabled",
                                    disabled: true,
                                    content: v => v.Text([Text.Body], "Disabled Zone"));
                            });
                        });
                    });
            });

            // DragOverlay
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Drag Overlay");
                view.Text([Text.Caption, "mb-4"], "Customize the appearance of items while being dragged");

                view.DndContext(
                    collisionDetection: CollisionDetection.PointerWithin,
                    onDragStart: async args => _activeDragId.Value = args.ActiveId,
                    onDragEnd: async args =>
                    {
                        _dndStatus.Value = $"Overlay demo: {args.ActiveId} -> {args.OverId ?? "cancelled"}";

                        if (_overlayItemZones.Value.ContainsKey(args.ActiveId))
                        {
                            var zones = new Dictionary<string, string?>(_overlayItemZones.Value);

                            if (args.OverId == "overlay-drop")
                            {
                                zones[args.ActiveId] = args.OverId;
                            }
                            else if (args.OverId == "overlay-source")
                            {
                                zones[args.ActiveId] = null;
                            }

                            _overlayItemZones.Value = zones;
                        }

                        _activeDragId.Value = "";
                    },
                    onDragCancel: async () => _activeDragId.Value = "",
                    content: view =>
                    {
                        view.Row(["gap-4 flex-wrap"], content: view =>
                        {
                            // Source area - wrapped in droppable to allow returning items
                            view.Droppable(
                                [DragDrop.Droppable.Default, "min-h-[60px]"],
                                id: "overlay-source",
                                content: sourceView =>
                                {
                                    sourceView.Row(["gap-4"], content: rowView =>
                                    {
                                        var availableItems = _overlayItemZones.Value
                                            .Where(kvp => kvp.Value == null)
                                            .Select(kvp => kvp.Key);

                                        foreach (var itemId in availableItems)
                                        {
                                            RenderOverlayDraggableItem(rowView, itemId);
                                        }
                                    });
                                });

                            // Drop zone
                            view.Droppable(
                                [DragDrop.Droppable.Default, "min-h-[60px]"],
                                id: "overlay-drop",
                                content: v =>
                                {
                                    v.Text(text: "Drop here");
                                    var droppedItems = _overlayItemZones.Value
                                        .Where(kvp => kvp.Value == "overlay-drop")
                                        .Select(kvp => kvp.Key);

                                    foreach (var itemId in droppedItems)
                                    {
                                        RenderOverlayDraggableItem(v, itemId);
                                    }
                                });
                        });

                        view.DragOverlay(
                            [DragDrop.OverlayContent],
                            dropAnimation: true,
                            activeDragId: _activeDragId.Value,
                            content: view =>
                            {
                                if (!string.IsNullOrEmpty(_activeDragId.Value))
                                {
                                    view.Row([Layout.Row.InlineCenter], content: v =>
                                    {
                                        v.Icon([Icon.Default], name: "hand");
                                        v.Text([Text.BodyStrong], $"Moving: {_activeDragId.Value}");
                                    });
                                }
                            });
                    });
            });

            // Sortable with disabled items
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Sortable with Disabled Items");
                view.Text([Text.Caption, "mb-4"], "Some items in the list cannot be moved");

                view.DndContext(
                    onDragEnd: async args => _dndStatus.Value = $"Mixed list: {args.ActiveId} -> {args.OverId ?? "none"}",
                    content: view =>
                    {
                        var items = new[] { "movable-1", "fixed-item", "movable-2", "movable-3" };
                        view.SortableContext(
                            [DragDrop.Container],
                            items: items,
                            strategy: SortStrategy.VerticalList,
                            content: view =>
                            {
                                foreach (var item in items)
                                {
                                    var isDisabled = item.StartsWith("fixed");
                                    var itemStyle = isDisabled
                                        ? DragDrop.Item.Base + " cursor-not-allowed opacity-50"
                                        : DragDrop.Item.Base;

                                    view.SortableItem(
                                        [itemStyle],
                                        id: item,
                                        key: item,
                                        disabled: isDisabled,
                                        content: v =>
                                        {
                                            v.Row([Layout.Row.InlineCenter], content: v =>
                                            {
                                                v.Icon([Icon.Default], name: isDisabled ? "lock" : "drag-handle-horizontal");
                                                v.Text([Text.Body], item);
                                                if (isDisabled)
                                                {
                                                    v.Text([Text.Caption, "ml-auto"], "(locked)");
                                                }
                                            });
                                        });
                                }
                            });
                    });
            });

            // Status display
            if (!string.IsNullOrEmpty(_dndStatus.Value))
            {
                view.Box([Card.Elevated, "p-4 mt-4"], content: view =>
                {
                    view.Text([Text.Caption], "Last DnD Event:");
                    view.Text([Text.Body], _dndStatus.Value);
                });
            }
        });
    }

    private void RenderDraggableItem(UIView view, string itemId)
    {
        var isDisabled = itemId == "drag-disabled";
        var style = isDisabled ? DragDrop.Draggable.Disabled : DragDrop.Draggable.Dashed;

        view.Draggable(
            [style],
            id: itemId,
            disabled: isDisabled,
            data: itemId == "drag-2" ? new { type = "special", priority = 1 } : null,
            content: v =>
            {
                v.Row([Layout.Row.InlineCenter], content: v =>
                {
                    var icon = itemId switch
                    {
                        "drag-1" => "move",
                        "drag-2" => "star",
                        _ => "lock"
                    };
                    var text = itemId switch
                    {
                        "drag-1" => "Drag me (1)",
                        "drag-2" => "Drag me (2) with data",
                        _ => "Disabled"
                    };
                    v.Icon([Icon.Default], name: icon);
                    v.Text(text: text);
                });
            });
    }

    private void RenderOverlayDraggableItem(UIView view, string itemId)
    {
        var text = itemId == "overlay-item-1" ? "Item with overlay" : "Another item";

        view.Draggable(
            [DragDrop.Draggable.Base],
            id: itemId,
            hideOnDrag: true,
            content: v => v.Text(text: text));
    }
}
