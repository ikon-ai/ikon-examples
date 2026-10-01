public partial class Validation
{
    // Extra MeshGeneratorConfig options. Every default is the config's own default, so leaving the
    // card untouched sends exactly what it sent before — Meshy throws on a Rodin-only value.
    private readonly Reactive<string> _meshGeometryMode = new(nameof(MeshGeneratorGeometryMode.Auto));
    private readonly Reactive<string> _meshMaterial = new(nameof(MeshGeneratorMaterial.Auto));
    private readonly Reactive<string> _meshOutputFormat = new(nameof(MeshGeneratorFileFormat.Glb));
    private readonly Reactive<string> _meshInputImageView = new(nameof(MeshGeneratorView.Unknown));
    private readonly Reactive<bool> _meshBoundingBox = new(false);
    private readonly Reactive<bool> _meshRestPose = new(false);
    private readonly Reactive<bool> _meshPreviewRender = new(false);
    private readonly Reactive<int> _meshTargetPolycount = new(0);

    private readonly Reactive<bool> _meshOpsProcessing = new(false);
    private readonly Reactive<string?> _meshOpsResult = new(null);
    private readonly Reactive<string?> _meshOpsError = new(null);
    private readonly Reactive<string?> _meshRigTaskId = new(null);
    private MeshGeneratorResult? _meshOpsResultData;

    private readonly Reactive<bool> _meshAnimationsProcessing = new(false);
    private readonly Reactive<string?> _meshAnimationsResult = new(null);
    private readonly Reactive<string?> _meshAnimationsError = new(null);

    private static MeshGeneratorCapabilities? TryGetMeshCapabilities(string modelName)
        => Enum.TryParse<MeshGeneratorModel>(modelName, out var model) ? MeshGenerator.GetCapabilities(model) : null;

    private void RenderMeshGeneratorOptions(UIView view)
    {
        var capabilities = TryGetMeshCapabilities(_meshGeneratorModel.Value);

        if (capabilities != null)
        {
            view.Text([Text.Caption],
                $"Capabilities: text {capabilities.SupportsTextToMesh}, image {capabilities.SupportsImageToMesh} (max {capabilities.MaxInputImages}), PBR {capabilities.SupportsPbr}, low-poly {capabilities.SupportsLowPoly}, polycount {capabilities.MinPolycount}-{capabilities.MaxPolycount}, rig {capabilities.SupportsRigging}, texture {capabilities.SupportsTexturing}, split {capabilities.SupportsPartSplitting}");
        }


        view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
        {
            view.Box([FormField.Root, "flex-1 min-w-40"], content: view =>
            {
                view.Text([FormField.Label], "Geometry mode");
                view.Select(
                    value: _meshGeometryMode.Value,
                    options: GetModelOptions<MeshGeneratorGeometryMode>(),
                    onValueChange: async v => _meshGeometryMode.Value = v ?? _meshGeometryMode.Value);
            });

            view.Box([FormField.Root, "flex-1 min-w-40"], content: view =>
            {
                view.Text([FormField.Label], "Material");
                view.Select(
                    value: _meshMaterial.Value,
                    options: GetModelOptions<MeshGeneratorMaterial>(),
                    onValueChange: async v => _meshMaterial.Value = v ?? _meshMaterial.Value);
            });

            view.Box([FormField.Root, "flex-1 min-w-40"], content: view =>
            {
                view.Text([FormField.Label], "Output format");
                view.Select(
                    value: _meshOutputFormat.Value,
                    options: GetModelOptions<MeshGeneratorFileFormat>(),
                    onValueChange: async v => _meshOutputFormat.Value = v ?? _meshOutputFormat.Value);
            });

            view.Box([FormField.Root, "flex-1 min-w-40"], content: view =>
            {
                view.Text([FormField.Label], "Input image view");
                view.Select(
                    value: _meshInputImageView.Value,
                    options: GetModelOptions<MeshGeneratorView>(),
                    onValueChange: async v => _meshInputImageView.Value = v ?? _meshInputImageView.Value);
            });

            view.Box([FormField.Root, "flex-1 min-w-40"], content: view =>
            {
                view.Text([FormField.Label], "Target polycount (0 = default)");
                view.TextField(
                    [Input.Default],
                    value: _meshTargetPolycount.Value.ToString(),
                    type: "number",
                    onValueChange: async v =>
                    {
                        if (int.TryParse(v, out var num) && num >= 0)
                        {
                            _meshTargetPolycount.Value = num;
                        }
                    });
            });
        });

        view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
        {
            view.Checkbox([Checkbox.Default], label: "Bounding box 100x100x100", value: _meshBoundingBox.Value,
                onValueChange: async v => _meshBoundingBox.Value = v);
            view.Checkbox([Checkbox.Default], label: "Rest pose (for rigging)", value: _meshRestPose.Value,
                onValueChange: async v => _meshRestPose.Value = v);
            view.Checkbox([Checkbox.Default], label: "Preview render", value: _meshPreviewRender.Value,
                onValueChange: async v => _meshPreviewRender.Value = v);
        });
    }

    private MeshGeneratorConfig ApplyMeshGeneratorOptions(MeshGeneratorConfig config)
    {
        config = config with
        {
            GeometryMode = Enum.Parse<MeshGeneratorGeometryMode>(_meshGeometryMode.Value),
            Material = Enum.Parse<MeshGeneratorMaterial>(_meshMaterial.Value),
            OutputFormat = Enum.Parse<MeshGeneratorFileFormat>(_meshOutputFormat.Value),
            RestPose = _meshRestPose.Value,
            PreviewRender = _meshPreviewRender.Value
        };

        if (_meshTargetPolycount.Value > 0)
        {
            config = config with { TargetPolycount = _meshTargetPolycount.Value };
        }

        if (_meshBoundingBox.Value)
        {
            config = config with { BoundingBox = new MeshGeneratorBoundingBox { Width = 100, Height = 100, Length = 100 } };
        }

        var view = Enum.Parse<MeshGeneratorView>(_meshInputImageView.Value);

        if (view != MeshGeneratorView.Unknown && config.InputImages.Count > 0)
        {
            config.InputImageViews.Add(view);
        }

        return config;
    }

    private void RenderMeshOperations(UIView view, MeshGeneratorResult mesh)
    {
        var capabilities = TryGetMeshCapabilities(_meshGeneratorModel.Value);
        var busy = _meshOpsProcessing.Value;

        view.Box([Card.Subtle, "mt-4 p-4"], props: TestId("ai-mesh-ops"), content: view =>
        {
            view.Text([Text.BodyStrong, "mb-1"], "Follow-up operations (manual, each is a paid provider task)");
            view.Text([Text.Caption, "mb-3"], $"Provider task id: {mesh.ProviderTaskId ?? "none"}");

            view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
            {
                view.Button([Button.PrimaryMd], text: "Rig",
                    props: TestId("ai-mesh-rig"),
                    disabled: busy || capabilities?.SupportsRigging != true || string.IsNullOrEmpty(mesh.GlbUrl),
                    onClick: () => RunMeshOperationAsync("Rig", generator => generator.RigMeshAsync(new MeshRigConfig
                    {
                        Mesh = new InputMesh { Url = mesh.GlbUrl, FileName = "character.glb" },
                        HeightMeters = 1.7
                    }), rigged => _meshRigTaskId.Value = rigged.ProviderTaskId));

                view.Button([Button.PrimaryMd], text: "Animate (2 walk clips)",
                    props: TestId("ai-mesh-animate"),
                    disabled: busy || string.IsNullOrEmpty(_meshRigTaskId.Value),
                    onClick: AnimateRiggedMeshAsync);

                view.Button([Button.PrimaryMd], text: "Split into parts",
                    props: TestId("ai-mesh-split"),
                    disabled: busy || capabilities?.SupportsPartSplitting != true || string.IsNullOrEmpty(mesh.GlbUrl),
                    onClick: () => RunMeshOperationAsync("Split", generator => generator.SplitMeshAsync(new MeshSplitConfig
                    {
                        Mesh = new InputMesh { Url = mesh.GlbUrl, FileName = "model.glb" },
                        Strength = 4
                    })));

                view.Button([Button.PrimaryMd], text: "Retexture from santa.jpg",
                    props: TestId("ai-mesh-texture"),
                    disabled: busy || capabilities?.SupportsTexturing != true || string.IsNullOrEmpty(mesh.GlbUrl),
                    onClick: () => RunMeshOperationAsync("Texture", async generator =>
                    {
                        var reference = await File.ReadAllBytesAsync(Path.Combine(app.DataDirectory, "santa.jpg"));
                        return await generator.TextureMeshAsync(new MeshTextureConfig
                        {
                            Mesh = new InputMesh { Url = mesh.GlbUrl, FileName = "model.glb" },
                            ReferenceImage = new InputImage { Data = reference, MimeType = MimeTypes.ImageJpeg }
                        });
                    }));

                if (busy)
                {
                    view.Box([Icon.Spinner]);
                }
            });

            if (!string.IsNullOrEmpty(_meshRigTaskId.Value))
            {
                view.Text([Text.Caption, "mt-2"], $"Rig task id: {_meshRigTaskId.Value}");
            }

            if (!string.IsNullOrEmpty(_meshOpsError.Value))
            {
                view.Box([Alert.Error, "mt-3"], props: TestId("ai-mesh-ops-error"), content: view =>
                {
                    view.Text([Alert.Description], _meshOpsError.Value);
                });
            }

            var opsResult = _meshOpsResultData;

            if (!string.IsNullOrEmpty(_meshOpsResult.Value) && opsResult != null)
            {
                view.Box([Alert.Success, "mt-3"], props: TestId("ai-mesh-ops-result"), content: view =>
                {
                    view.Text([Alert.Description], _meshOpsResult.Value);
                });

                view.Row([Layout.Row.Md, "mt-3 items-center flex-wrap"], content: view =>
                {
                    RenderMeshDownloadButton(view, "GLB", opsResult.GlbUrl);
                    RenderMeshDownloadButton(view, "FBX", opsResult.FbxUrl);
                    RenderMeshDownloadButton(view, "STL", opsResult.StlUrl);

                    foreach (var file in opsResult.Files)
                    {
                        RenderMeshDownloadButton(view, file.Name, file.Url);
                    }
                });
            }
        });
    }

    private async Task RunMeshOperationAsync(string name, Func<MeshGenerator, Task<MeshGeneratorResult>> operation,
        System.Action<MeshGeneratorResult>? onResult = null)
    {
        _meshOpsProcessing.Value = true;
        _meshOpsError.Value = null;
        _meshOpsResult.Value = null;
        _meshOpsResultData = null;

        try
        {
            var model = Enum.Parse<MeshGeneratorModel>(_meshGeneratorModel.Value);
            using var generator = new MeshGenerator(model);
            var result = await operation(generator);

            onResult?.Invoke(result);
            _meshOpsResultData = result;
            _meshOpsResult.Value = $"{name} done: {result.Files.Count} file(s), task {result.ProviderTaskId ?? "none"}"
                + (result.ExpiresAt != null ? $", links expire {result.ExpiresAt:u}" : "");
        }
        catch (Exception ex)
        {
            _meshOpsError.Value = $"{name} failed: {ex.Message}";
        }
        finally
        {
            _meshOpsProcessing.Value = false;
        }
    }

    private Task AnimateRiggedMeshAsync()
    {
        var rigTaskId = _meshRigTaskId.Value;

        return RunMeshOperationAsync("Animate", async generator =>
        {
            var library = await generator.GetAnimationLibraryAsync(search: "walk");

            if (library.Count == 0)
            {
                throw new InvalidOperationException("The animation library has no walk actions");
            }

            return await generator.AnimateMeshAsync(new MeshAnimationConfig
            {
                RigTaskId = rigTaskId!,
                ActionIds = [.. library.Take(2).Select(action => action.Id)],
                Fps = 30
            });
        });
    }

    private void RenderMeshAnimationLibraryCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], props: TestId("ai-mesh-animations-card"), content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Mesh Animation Library");

            view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
            {
                view.Button(
                    [Button.PrimaryMd],
                    text: "List Animations",
                    props: TestId("ai-mesh-animations-run"),
                    disabled: _meshAnimationsProcessing.Value,
                    onClick: ListMeshAnimationsAsync);

                if (_meshAnimationsProcessing.Value)
                {
                    view.Box([Icon.Spinner]);
                }
            });

            if (!string.IsNullOrEmpty(_meshAnimationsError.Value))
            {
                view.Box([Alert.Error, "mt-4"], props: TestId("ai-mesh-animations-error"), content: view =>
                {
                    view.Text([Alert.Description, "whitespace-pre-wrap"], _meshAnimationsError.Value);
                });
            }

            if (!string.IsNullOrEmpty(_meshAnimationsResult.Value))
            {
                view.Box([Alert.Success, "mt-4"], props: TestId("ai-mesh-animations-result"), content: view =>
                {
                    view.Text([Alert.Description, "whitespace-pre-wrap"], _meshAnimationsResult.Value);
                });
            }
        });
    }

    private async Task ListMeshAnimationsAsync()
    {
        _meshAnimationsProcessing.Value = true;
        _meshAnimationsError.Value = null;
        _meshAnimationsResult.Value = null;

        try
        {
            using var generator = new MeshGenerator(MeshGeneratorModel.Meshy6);

            if (!generator.SupportsRigging)
            {
                _meshAnimationsError.Value = "FAIL: Meshy 6 no longer reports SupportsRigging";
                return;
            }

            var all = await generator.GetAnimationLibraryAsync();
            var dancing = await generator.GetAnimationLibraryAsync(category: "Dancing");
            var walk = await generator.GetAnimationLibraryAsync(search: "walk");

            var failures = new List<string>();

            if (all.Count == 0)
            {
                failures.Add("the full library is empty");
            }

            if (walk.Count == 0)
            {
                failures.Add("search \"walk\" found nothing");
            }

            if (dancing.Count > all.Count || walk.Count > all.Count)
            {
                failures.Add("a filtered listing is larger than the full one");
            }

            var categories = string.Join(", ", all.Select(a => a.Category).Distinct().Order());
            var first = all.FirstOrDefault();
            var summary = $"{all.Count} actions in categories [{categories}]; Dancing: {dancing.Count}; search \"walk\": {walk.Count}"
                + (first != null ? $"\nFirst: #{first.Id} {first.Name} ({first.Category}/{first.SubCategory}, key {first.Key})" : "");

            if (failures.Count > 0)
            {
                _meshAnimationsError.Value = "FAIL: " + string.Join("; ", failures) + "\n" + summary;
                return;
            }

            _meshAnimationsResult.Value = "PASS " + summary;
        }
        catch (Exception ex)
        {
            _meshAnimationsError.Value = $"FAIL: {ex.Message}";
        }
        finally
        {
            _meshAnimationsProcessing.Value = false;
        }
    }
}
