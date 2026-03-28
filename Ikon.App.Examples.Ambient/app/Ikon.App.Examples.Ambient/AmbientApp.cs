using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using Ikon.AI.SoundEffectGeneration;
using Ikon.AI.VideoEnhancement;
using Ikon.Common.Core;

return await App.Run(args);

public record SessionIdentity(string UserId);
public record ClientParams(string Name = "Ikon");

[App]
public class AmbientApp
{
    private UI UI { get; }

    private readonly Reactive<int> _selectedIndex = new(-1);
    private readonly Reactive<string?> _currentVideoUrl = new(null);
    private readonly Reactive<bool> _isVideoLoading = new(false);
    private readonly Reactive<string?> _videoError = new(null);
    private readonly Reactive<float> _playbackRate = new(0.25f);
    private readonly Reactive<bool> _isUpscaling = new(false);
    private readonly Reactive<string?> _currentAudioUrl = new(null);
    private readonly Reactive<bool> _isAudioLoading = new(false);
    private readonly Reactive<List<VideoVersion>> _videoVersions = new([]);
    private readonly Reactive<int> _selectedVersionIndex = new(0);
    private readonly Reactive<string?> _currentVideoPrompt = new(null);
    private readonly Reactive<List<CustomExperienceData>> _customExperiences = new([]);
    private readonly Reactive<bool> _showCreateForm = new(false);
    private readonly Reactive<string> _createDescription = new("");
    private readonly Reactive<bool> _isCreating = new(false);

    private record VideoVersion(string Name, string Url);
    private readonly Dictionary<string, List<VideoVersion>> _videoVersionsCache = new();
    private readonly Dictionary<string, string> _audioUrlCache = new();
    private readonly HashSet<string> _generatingPrompts = new();
    private readonly HashSet<string> _generatingAudioPrompts = new();
    private readonly List<AmbientExperience> _builtInExperiences =
    [
        new("Fireplace", "Warm Ember", "Crackling glow with slow, comforting light", "text-amber-200", "bg-gradient-to-br from-amber-500/40 via-orange-500/30 to-rose-500/20", "Cozy · 24°C", "Wide cinematic shot of a crackling fireplace with warm ember glow, flames dancing gently in cozy room, ambient atmosphere, static camera, seamlessly looping, 4K", "Crackling fireplace with gentle wood pops and soft ember sounds, warm cozy atmosphere"),
        new("Winter", "Frosted Calm", "Soft snowfall drifting across a quiet night", "text-sky-200", "bg-gradient-to-br from-sky-400/40 via-blue-500/30 to-indigo-500/20", "Cold · -2°C", "Wide establishing shot of soft snowfall drifting across a quiet winter night, scenic landscape view, gentle snowflakes floating down, static camera, seamlessly looping, 4K cinematic", "Soft winter wind with gentle snowfall sounds, quiet peaceful winter night ambience"),
        new("Rain", "Midnight Rain", "City rain patterns with a mellow glow", "text-cyan-200", "bg-gradient-to-br from-cyan-500/40 via-blue-600/30 to-slate-800/30", "Rain · 12°C", "Wide cinematic aerial view of city at night in rain, neon reflections on wet streets, raindrops visible, distant cityscape, static camera, seamlessly looping, 4K", "Gentle rain falling on city streets at night, soft raindrops with distant urban ambience"),
        new("Clouds", "Open Skies", "Slow-moving clouds with soft morning light", "text-violet-200", "bg-gradient-to-br from-violet-400/35 via-indigo-500/30 to-slate-900/30", "Breezy · 18°C", "Wide panoramic timelapse of slow-moving clouds drifting across vast sky, soft morning golden hour light, scenic vista, static camera, seamlessly looping, 4K cinematic", "Soft gentle breeze with light wind sounds, peaceful open sky atmosphere"),
        new("Skyline", "City Skyline", "Neon silhouettes and a quiet horizon", "text-fuchsia-200", "bg-gradient-to-br from-fuchsia-500/35 via-purple-600/30 to-slate-900/40", "Urban · 20°C", "Wide establishing shot of city skyline at twilight, neon building silhouettes against purple sky, distant horizon view, static camera, seamlessly looping, 4K cinematic", "Distant city ambience at twilight, soft urban hum with gentle evening atmosphere"),
        new("Hong Kong", "Neon Harbor", "Harbor lights with midnight shimmer", "text-emerald-200", "bg-gradient-to-br from-emerald-500/30 via-teal-500/25 to-slate-900/40", "Harbor · 23°C", "Wide panoramic view of Hong Kong Victoria Harbor at night, distant city lights reflecting on water, skyline vista from across the bay, static camera, seamlessly looping, 4K cinematic", "Harbor water lapping gently with distant city sounds, calm night harbor ambience"),
        new("London", "Foggy Thames", "Soft fog rolling over quiet city lights", "text-rose-200", "bg-gradient-to-br from-rose-400/35 via-slate-700/30 to-slate-950/50", "Fog · 11°C", "Wide cinematic shot of foggy Thames river at dusk, soft city lights in distance, mist rolling gently over water, scenic London skyline, static camera, seamlessly looping, 4K", "Foggy river atmosphere with gentle water sounds, misty evening ambience with distant city"),
        new("Aquarium", "Deep Blue", "Slow drift of light beneath the surface", "text-blue-200", "bg-gradient-to-br from-blue-500/40 via-cyan-600/25 to-slate-950/50", "Calm · 19°C", "Wide underwater scene in deep blue aquarium, slow drifting tropical fish, volumetric light rays from above, serene ambient atmosphere, static camera, seamlessly looping, 4K cinematic", "Gentle underwater bubbles and soft water movement, peaceful deep aquarium ambience"),
        new("Forest", "Pine Grove", "Layered greens with gentle motion", "text-lime-200", "bg-gradient-to-br from-lime-500/30 via-emerald-600/25 to-slate-950/50", "Fresh · 16°C", "Wide scenic establishing shot of pine forest, sunlight filtering through tall trees, gentle wind motion in branches, forest vista view, static camera, seamlessly looping, 4K cinematic", "Forest wind rustling through pine trees, birds chirping softly in the distance, peaceful nature ambience"),
        new("Desert", "Dusk Dunes", "Warm dunes with a cinematic sunset", "text-orange-200", "bg-gradient-to-br from-orange-400/35 via-amber-500/30 to-slate-900/40", "Dry · 28°C", "Wide panoramic view of desert sand dunes at golden hour sunset, cinematic landscape vista, gentle sand ripples, distant horizon, static camera, seamlessly looping, 4K", "Soft desert wind blowing over sand dunes, warm dry breeze with distant emptiness"),
        new("Lapland", "Arctic Winter", "Pristine snow covering frozen wilderness", "text-sky-100", "bg-gradient-to-br from-sky-300/35 via-blue-400/30 to-slate-900/50", "Arctic · -15°C", "Wide cinematic panoramic view of Finnish Lapland winter landscape, pristine white snow covering pine forests and frozen lakes, soft blue hour light, gentle snowfall, distant fell mountains, static camera, seamlessly looping, 4K", "Arctic wind with soft snow crunching, peaceful frozen wilderness silence with gentle breeze"),
        new("Aurora", "Northern Lights", "Dancing curtains of light across polar skies", "text-emerald-100", "bg-gradient-to-br from-emerald-400/40 via-teal-500/30 to-purple-900/40", "Polar · -20°C", "Wide panoramic view of Aurora Borealis northern lights dancing across night sky over Finnish Lapland, green and purple light curtains reflecting on snow, distant forest silhouette, stars visible, static camera, seamlessly looping, 4K cinematic", "Ethereal aurora shimmer with soft arctic wind, magical northern lights atmosphere with quiet polar night")
    ];

    public AmbientApp(IApp<SessionIdentity, ClientParams> app)
    {
        UI = new UI(app, new Theme());
    }

    private static string GetPromptHash(string prompt)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(prompt));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    private static AssetUri GetCacheAssetUri(string hash)
    {
        return new AssetUri(AssetClass.CloudJson, $"ambient-video-cache/{hash}.json");
    }

    private static AssetUri GetGenerationStatusAssetUri(string hash)
    {
        return new AssetUri(AssetClass.CloudJson, $"ambient-generation-status/{hash}.json");
    }

    private async Task<List<VideoVersion>?> GetCachedVideoVersions(string prompt)
    {
        var hash = GetPromptHash(prompt);

        if (_videoVersionsCache.TryGetValue(hash, out var cachedVersions))
        {
            return cachedVersions;
        }

        var assetUri = GetCacheAssetUri(hash);
        var cached = await Asset.Instance.TryGetAsync<CachedVideo>(assetUri);

        if (cached?.Versions != null && cached.Versions.Count > 0)
        {
            var versions = cached.Versions.Select(v => new VideoVersion(v.Name, v.Url)).ToList();
            _videoVersionsCache[hash] = versions;
            return versions;
        }

        return null;
    }

    private async Task CacheVideoVersions(string prompt, List<VideoVersion> versions)
    {
        var hash = GetPromptHash(prompt);
        _videoVersionsCache[hash] = versions;

        var cachedVersions = versions.Select(v => new CachedVideoVersion(v.Name, v.Url)).ToList();
        var assetUri = GetCacheAssetUri(hash);
        await Asset.Instance.SetAsync(assetUri, new CachedVideo(prompt, DateTime.UtcNow, cachedVersions));
    }

    private async Task<GenerationStatus?> GetGenerationStatus(string hash)
    {
        var statusUri = GetGenerationStatusAssetUri(hash);
        var status = await Asset.Instance.TryGetAsync<GenerationStatus>(statusUri);

        if (status != null && (DateTime.UtcNow - status.StartedAt) > TimeSpan.FromMinutes(35))
        {
            await Asset.Instance.DeleteAsync(statusUri);
            return null;
        }

        return status;
    }

    private async Task SetGenerationStatus(string hash, bool isGenerating, bool isUpscaling)
    {
        var statusUri = GetGenerationStatusAssetUri(hash);
        var status = new GenerationStatus(isGenerating, isUpscaling, DateTime.UtcNow);
        await Asset.Instance.SetAsync(statusUri, status);
    }

    private async Task ClearGenerationStatus(string hash)
    {
        var statusUri = GetGenerationStatusAssetUri(hash);
        await Asset.Instance.DeleteAsync(statusUri);
    }

    private sealed record CachedVideoVersion(string Name, string Url);
    private sealed record CachedVideo(string Prompt, DateTime GeneratedAt, List<CachedVideoVersion> Versions);
    private sealed record GenerationStatus(bool IsGenerating, bool IsUpscaling, DateTime StartedAt);
    private sealed record CachedAudio(string Url, string Prompt, DateTime GeneratedAt);

    private static AssetUri GetAudioCacheAssetUri(string hash)
    {
        return new AssetUri(AssetClass.CloudJson, $"ambient-audio-cache/{hash}.json");
    }

    private static AssetUri GetAudioAssetUri(string hash)
    {
        return new AssetUri(AssetClass.CloudFilePublic, $"ambient-audio/{hash}.mp3");
    }

    private async Task<string?> GetCachedAudioUrl(string prompt)
    {
        var hash = GetPromptHash(prompt);

        if (_audioUrlCache.TryGetValue(hash, out var cachedUrl))
        {
            return cachedUrl;
        }

        var assetUri = GetAudioCacheAssetUri(hash);
        var cached = await Asset.Instance.TryGetAsync<CachedAudio>(assetUri);

        if (cached != null)
        {
            _audioUrlCache[hash] = cached.Url;
            return cached.Url;
        }

        return null;
    }

    private async Task CacheAudioUrl(string prompt, string url)
    {
        var hash = GetPromptHash(prompt);
        _audioUrlCache[hash] = url;

        var assetUri = GetAudioCacheAssetUri(hash);
        await Asset.Instance.SetAsync(assetUri, new CachedAudio(url, prompt, DateTime.UtcNow));
    }

    private static AssetUri GetCustomExperiencesAssetUri()
    {
        return new AssetUri(AssetClass.CloudJson, "ambient-custom-experiences.json");
    }

    private List<AmbientExperience> GetAllExperiences()
    {
        var all = new List<AmbientExperience>(_builtInExperiences);
        foreach (var custom in _customExperiences.Value)
        {
            all.Add(new AmbientExperience(
                custom.Title,
                "Custom",
                custom.Description,
                "text-pink-200",
                "bg-gradient-to-br from-pink-500/40 via-purple-500/30 to-slate-900/40",
                "Custom · Your Creation",
                custom.VideoPrompt,
                custom.AudioPrompt,
                IsCustom: true,
                CustomId: custom.Id));
        }
        return all;
    }

    private async Task LoadCustomExperiences()
    {
        var assetUri = GetCustomExperiencesAssetUri();
        var data = await Asset.Instance.TryGetAsync<List<CustomExperienceData>>(assetUri);
        if (data != null)
        {
            _customExperiences.Value = data;
        }
    }

    private async Task SaveCustomExperiences()
    {
        var assetUri = GetCustomExperiencesAssetUri();
        await Asset.Instance.SetAsync(assetUri, _customExperiences.Value);
    }

    private static string GenerateTitle(string description)
    {
        var words = description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Take(3));
    }

    private static string GenerateVideoPrompt(string description)
    {
        return $"Wide cinematic shot of {description}, ambient atmosphere, soft lighting, static camera, seamlessly looping, 4K";
    }

    private static string GenerateAudioPrompt(string description)
    {
        return $"Ambient soundscape for {description}, peaceful atmosphere, suitable for relaxation";
    }

    private async Task CreateCustomExperience()
    {
        if (string.IsNullOrWhiteSpace(_createDescription.Value) || _isCreating.Value)
        {
            return;
        }

        _isCreating.Value = true;

        try
        {
            var description = _createDescription.Value.Trim();
            var id = Guid.NewGuid().ToString("N")[..8];
            var title = GenerateTitle(description);
            var videoPrompt = GenerateVideoPrompt(description);
            var audioPrompt = GenerateAudioPrompt(description);

            var customExperience = new CustomExperienceData(
                id,
                title,
                description,
                videoPrompt,
                audioPrompt,
                DateTime.UtcNow);

            var updated = _customExperiences.Value.ToList();
            updated.Add(customExperience);
            _customExperiences.Value = updated;
            await SaveCustomExperiences();

            _createDescription.Value = "";
            _showCreateForm.Value = false;

            var allExperiences = GetAllExperiences();
            var newIndex = allExperiences.Count - 1;
            var newExperience = allExperiences[newIndex];
            _selectedIndex.Value = newIndex;
            _currentVideoPrompt.Value = newExperience.VideoPrompt;
            _isVideoLoading.Value = true;
            _isAudioLoading.Value = true;
            _videoError.Value = null;
            _ = Task.Run(async () =>
            {
                try
                {
                    await GenerateVideoForExperience(newIndex, newExperience);
                }
                catch (Exception ex)
                {
                    Log.Instance.Error($"Failed to generate video for custom experience: {ex}");
                    if (_selectedIndex.Value == newIndex)
                    {
                        _videoError.Value = $"Video generation failed: {ex.Message}";
                        _isVideoLoading.Value = false;
                    }
                }
            });
            _ = Task.Run(async () =>
            {
                try
                {
                    await GenerateAudioForExperience(newIndex, newExperience);
                }
                catch (Exception ex)
                {
                    Log.Instance.Error($"Failed to generate audio for custom experience: {ex}");
                    if (_selectedIndex.Value == newIndex)
                    {
                        _isAudioLoading.Value = false;
                    }
                }
            });
        }
        finally
        {
            _isCreating.Value = false;
        }
    }

    private async Task DeleteCustomExperience(string customId)
    {
        var updated = _customExperiences.Value.Where(e => e.Id != customId).ToList();
        _customExperiences.Value = updated;
        await SaveCustomExperiences();

        _selectedIndex.Value = -1;
        _currentVideoUrl.Value = null;
        _currentAudioUrl.Value = null;
        _currentVideoPrompt.Value = null;
        _videoVersions.Value = [];
    }

    private async Task<string?> UploadAudioToAssets(byte[] audioData, string hash)
    {
        var assetUri = GetAudioAssetUri(hash);
        await Asset.Instance.SetBytesAsync(assetUri, audioData, new AssetMetadata(mimeType: "audio/mpeg"));

        var metadata = await Asset.Instance.GetMetadataAsync(assetUri);
        return metadata.Url;
    }

    private async Task GenerateAudioForExperience(int index, AmbientExperience experience)
    {
        var cachedUrl = await GetCachedAudioUrl(experience.AudioPrompt);
        if (cachedUrl != null)
        {
            if (_selectedIndex.Value == index)
            {
                _currentAudioUrl.Value = cachedUrl;
            }

            return;
        }

        if (_generatingAudioPrompts.Contains(experience.AudioPrompt))
        {
            return;
        }

        _generatingAudioPrompts.Add(experience.AudioPrompt);

        if (_selectedIndex.Value == index)
        {
            _isAudioLoading.Value = true;
        }

        try
        {
            using var generator = new SoundEffectGenerator(SoundEffectGeneratorModel.ElevenLabsV2);
            var result = await generator.GenerateSoundEffectFileAsync(new SoundEffectGeneratorConfig
            {
                Prompt = experience.AudioPrompt,
                DurationSeconds = 22.0,
                Loop = true,
                PromptInfluence = 0.3
            });

            var hash = GetPromptHash(experience.AudioPrompt);
            var publicUrl = await UploadAudioToAssets(result.AudioData, hash);

            if (publicUrl != null)
            {
                await CacheAudioUrl(experience.AudioPrompt, publicUrl);

                if (_selectedIndex.Value == index)
                {
                    _currentAudioUrl.Value = publicUrl;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Failed to generate audio for {experience.Title}: {ex.Message}");
        }
        finally
        {
            _generatingAudioPrompts.Remove(experience.AudioPrompt);

            if (_selectedIndex.Value == index)
            {
                _isAudioLoading.Value = false;
            }
        }
    }

    public async Task Main()
    {
        await LoadCustomExperiences();

        UI.Root([Page.Default, "min-h-screen bg-slate-950 text-white font-sans"], content: view =>
        {
            view.Box(["min-h-screen w-full bg-slate-950 text-white"], content: view =>
            {
                if (_showCreateForm.Value)
                {
                    RenderCreateForm(view);
                }
                else if (_selectedIndex.Value < 0)
                {
                    RenderSelection(view);
                }
                else
                {
                    RenderExperience(view, GetAllExperiences()[_selectedIndex.Value]);
                }
            });
        });
    }

    private void RenderSelection(UIView view)
    {
        var experiences = GetAllExperiences();

        view.Column(["min-h-screen w-full px-8 py-10 2xl:px-24 box-border", Layout.Column.Lg], content: view =>
        {
            view.Column([Layout.Column.Sm], content: view =>
            {
                view.Text([
                    "text-5xl font-semibold tracking-tight",
                    "wave:motion-[0:translate-y-0,50:translate-y-[-6px],100:translate-y-0]",
                    "wave:motion-duration-2200ms wave:motion-stagger-120ms wave:motion-per-letter-loop wave:motion-ease-ease-in-out"
                ], "Ambient Experiences");
                view.Text([Text.Body, "text-white/70 max-w-2xl"], "Select a scene to fill the room with calm motion");
                view.Text([Text.Caption, "text-white/50"], "Designed for large screens with minimal controls");
            });

            view.Box(["grid gap-4 sm:gap-6 sm:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-5"], content: view =>
            {
                for (var i = 0; i < experiences.Count; i++)
                {
                    var index = i;
                    var experience = experiences[i];
                    view.Button(
                        [
                            "group relative h-44 sm:h-48 lg:h-52 2xl:h-56 w-full rounded-3xl border border-white/10",
                            "bg-slate-900/60 text-left overflow-hidden",
                            "transition duration-500 ease-out hover:-translate-y-1 hover:border-white/30",
                            "hover:shadow-[0_24px_70px_-35px_rgba(15,23,42,0.9)]",
                            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-cyan-400/60",
                            "motion-[0:opacity-0_translate-y-[12px],100:opacity-100_translate-y-0] motion-duration-500ms motion-fill-both"
                        ],
                        onClick: async () =>
                        {
                            _selectedIndex.Value = index;
                            _currentVideoPrompt.Value = experience.VideoPrompt;
                            _ = GenerateVideoForExperience(index, experience);
                            _ = GenerateAudioForExperience(index, experience);
                        },
                        content: view =>
                        {
                            view.Box(["absolute inset-0 rounded-3xl", experience.Backdrop, "opacity-80"]);
                            view.Box(["absolute inset-0 rounded-3xl bg-gradient-to-t from-slate-950/90 via-slate-950/40 to-transparent"]);

                            if (experience.IsCustom)
                            {
                                view.Box(["absolute top-3 right-3 px-2 py-1 bg-pink-500/30 rounded-full z-20"], content: view =>
                                {
                                    view.Text(["text-[10px] uppercase tracking-wider text-pink-200"], "Custom");
                                });
                            }

                            view.Box([
                                "relative z-10 h-full w-full p-5",
                                "flex flex-col justify-between"
                            ], content: view =>
                            {
                                view.Column([Layout.Column.Xs], content: view =>
                                {
                                    view.Text(["text-xs uppercase tracking-[0.28em] text-white/60"], experience.Subtitle);
                                    view.Text([
                                        "text-2xl font-semibold tracking-tight",
                                        experience.Accent,
                                        "motion-[0:opacity-0_translate-y-[6px],100:opacity-100_translate-y-0] motion-duration-400ms motion-fill-both"
                                    ], experience.Title);
                                });

                                view.Column([Layout.Column.Xs], content: view =>
                                {
                                    view.Text(["text-sm text-white/70"], experience.Description);
                                    view.Text(["text-[11px] uppercase tracking-[0.3em] text-white/40"], experience.Atmosphere);
                                });
                            });
                        }
                    );
                }

                view.Button(
                    [
                        "group relative h-44 sm:h-48 lg:h-52 2xl:h-56 w-full rounded-3xl border border-dashed border-white/20",
                        "bg-slate-900/40 text-left overflow-hidden",
                        "transition duration-500 ease-out hover:-translate-y-1 hover:border-white/40",
                        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-pink-400/60",
                        "motion-[0:opacity-0_translate-y-[12px],100:opacity-100_translate-y-0] motion-duration-500ms motion-fill-both"
                    ],
                    onClick: async () => { _showCreateForm.Value = true; },
                    content: view =>
                    {
                        view.Box(["absolute inset-0 flex items-center justify-center"], content: view =>
                        {
                            view.Column(["items-center text-center", Layout.Column.Sm], content: view =>
                            {
                                view.Text(["text-4xl text-white/40"], "+");
                                view.Text(["text-lg font-semibold text-white/60"], "Create Your Own");
                                view.Text(["text-sm text-white/40"], "Describe your ambient scene");
                            });
                        });
                    }
                );
            });
        });
    }

    private void RenderCreateForm(UIView view)
    {
        view.Column(["min-h-screen w-full px-8 py-10 items-center justify-center", Layout.Column.Lg], content: view =>
        {
            view.Column(["max-w-2xl w-full", Layout.Column.Lg], content: view =>
            {
                view.Text(["text-4xl font-semibold text-white"], "Create Your Ambient Scene");
                view.Text(["text-white/60"], "Describe the scene you want to create. Be specific about visuals and sounds.");

                view.TextArea(
                    [Input.Default, "w-full h-32 bg-slate-800/50 border-white/20 text-white placeholder:text-white/40"],
                    placeholder: "e.g., A peaceful Japanese zen garden at dawn with cherry blossoms falling...",
                    value: _createDescription.Value,
                    onValueChange: async value => _createDescription.Value = value ?? ""
                );

                view.Row(["gap-4"], content: view =>
                {
                    view.Button(
                        [Button.OutlineMd, "bg-white/10 border-white/20 text-white"],
                        label: "Cancel",
                        onClick: async () =>
                        {
                            _showCreateForm.Value = false;
                            _createDescription.Value = "";
                        }
                    );

                    view.Button(
                        [Button.PrimaryMd, "bg-pink-500 text-white"],
                        label: _isCreating.Value ? "Creating..." : "Create Experience",
                        disabled: _isCreating.Value || string.IsNullOrWhiteSpace(_createDescription.Value),
                        onClick: async () => await CreateCustomExperience()
                    );
                });
            });
        });
    }

    private void RenderExperience(UIView view, AmbientExperience experience)
    {
        view.Column(["min-h-screen w-full px-8 py-10 2xl:px-24 relative box-border overflow-hidden", Layout.Column.Lg], content: view =>
        {
            view.Box(["absolute inset-0 -mx-8 -my-10 2xl:-mx-24", experience.Backdrop, "opacity-90"]);
            view.Box(["absolute inset-0 -mx-8 -my-10 2xl:-mx-24 bg-slate-950/70"]);

            view.Row(["relative z-10 items-center justify-between gap-6"], content: view =>
            {
                view.Column([Layout.Column.Sm, "min-w-0 flex-1"], content: view =>
                {
                    view.Text(["text-sm uppercase tracking-[0.35em] text-white/60"], experience.Subtitle);
                    view.Text([
                        "text-5xl font-semibold tracking-tight",
                        experience.Accent,
                        "motion-[0:opacity-0_translate-y-[10px],100:opacity-100_translate-y-0] motion-duration-500ms motion-fill-both"
                    ], experience.Title);
                    view.Text(["text-lg text-white/70 max-w-xl"], experience.Description);
                });

                view.Row(["gap-3 flex-shrink-0 items-center flex-wrap"], content: view =>
                {
                    var hasUpscaled = _videoVersions.Value.Any(v => v.Name == "Upscaled");
                    var canUpscale = _currentVideoUrl.Value != null && !_isVideoLoading.Value && !_isUpscaling.Value && !hasUpscaled;

                    if (!hasUpscaled)
                    {
                        var upscaleLabel = _isUpscaling.Value ? "Upscaling..." : "Upscale";

                        view.Button(
                            [
                                Button.OutlineMd,
                                canUpscale ? "bg-violet-500/20 border-violet-400/40 text-violet-200" : "bg-white/5 border-white/10 text-white/40",
                                canUpscale ? "hover:bg-violet-500/30 hover:border-violet-400/60" : "",
                                "flex-shrink-0"
                            ],
                            label: upscaleLabel,
                            disabled: !canUpscale,
                            onClick: async () =>
                            {
                                if (canUpscale)
                                {
                                    _ = UpscaleVideo();
                                }
                            }
                        );
                    }

                    view.Button(
                        [
                            Button.OutlineMd,
                            "bg-white/10 border-white/20 text-white flex-shrink-0",
                            "hover:bg-white/20 hover:border-white/40"
                        ],
                        label: "Back",
                        onClick: async () =>
                        {
                            _selectedIndex.Value = -1;
                            _currentVideoUrl.Value = null;
                            _currentAudioUrl.Value = null;
                            _currentVideoPrompt.Value = null;
                            _videoError.Value = null;
                            _isVideoLoading.Value = false;
                            _isAudioLoading.Value = false;
                            _isUpscaling.Value = false;
                            _videoVersions.Value = [];
                            _selectedVersionIndex.Value = 0;
                        }
                    );

                    if (experience.IsCustom)
                    {
                        view.Button(
                            [
                                Button.OutlineMd,
                                "bg-rose-500/20 border-rose-400/40 text-rose-200 flex-shrink-0",
                                "hover:bg-rose-500/30 hover:border-rose-400/60"
                            ],
                            label: "Delete",
                            onClick: async () => await DeleteCustomExperience(experience.CustomId!)
                        );
                    }
                });
            });

            view.Box([
                "relative z-10 flex-1 w-full rounded-[32px] border border-white/10",
                "bg-slate-900/60 backdrop-blur-xl overflow-hidden"
            ],
            content: view =>
            {
                view.Box(["absolute inset-0 bg-gradient-to-br from-white/5 via-transparent to-black/40"]);
                view.Box(["absolute inset-0 flex items-center justify-center"], content: view =>
                {
                    if (_isVideoLoading.Value)
                    {
                        view.Column(["items-center text-center", Layout.Column.Lg], content: view =>
                        {
                            view.Box([
                                "w-12 h-12 border-4 border-white/20 border-t-white/80 rounded-full",
                                "animate-spin"
                            ]);
                            view.Text(["text-sm uppercase tracking-[0.4em] text-white/50 mt-6"], "Generating video");
                            view.Text(["text-xl font-semibold text-white"], "Creating your ambient scene...");
                            view.Text(["text-white/60 max-w-md text-center"], "This may take a minute or two");
                        });
                    }
                    else if (_videoError.Value != null)
                    {
                        view.Column(["items-center text-center px-8", Layout.Column.Lg], content: view =>
                        {
                            view.Text(["text-sm uppercase tracking-[0.4em] text-rose-400/80"], "Error");
                            view.Text(["text-xl font-semibold text-white"], "Failed to generate video");
                            view.Text(["text-white/60 max-w-md text-center break-words"], _videoError.Value);
                        });
                    }
                    else if (_currentVideoUrl.Value == null)
                    {
                        view.Column(["items-center text-center", Layout.Column.Lg], content: view =>
                        {
                            view.Text(["text-sm uppercase tracking-[0.4em] text-white/50"], "Video placeholder");
                            view.Text(["text-3xl font-semibold text-white"], "Ambient video feed will play here");
                            view.Text(["text-white/60 max-w-md text-center"], "Use this space for looped motion visuals tailored for large screens");
                        });
                    }
                });

                if (_currentVideoUrl.Value != null)
                {
                    view.AmbientVideoPlayer(
                        _currentVideoUrl.Value,
                        _playbackRate.Value,
                        loop: true,
                        muted: true,
                        autoplay: true,
                        style: ["absolute inset-0 w-full h-full"],
                        key: $"video-{_selectedIndex.Value}");
                }

                if (_currentAudioUrl.Value != null)
                {
                    view.AudioUrlPlayer(
                        ["hidden"],
                        url: _currentAudioUrl.Value,
                        autoplay: true,
                        loop: true,
                        key: $"audio-{_selectedIndex.Value}");
                }
            });

            view.Row(["relative z-10 justify-between text-xs uppercase tracking-[0.3em] text-white/40"], content: view =>
            {
                view.Text([], experience.Atmosphere);

                var statusParts = new List<string>();

                if (_isVideoLoading.Value)
                {
                    statusParts.Add("Generating video");
                }

                if (_isUpscaling.Value)
                {
                    statusParts.Add("Upscaling");
                }

                if (_isAudioLoading.Value)
                {
                    statusParts.Add("Generating audio");
                }

                if (_videoVersions.Value.Any(v => v.Name == "Upscaled"))
                {
                    statusParts.Add("4K · High FPS");
                }

                if (statusParts.Count == 0)
                {
                    statusParts.Add("Ready");
                }

                view.Text([], string.Join(" · ", statusParts));
            });
        });
    }

    private static AssetUri GetVideoAssetUri(string hash)
    {
        return new AssetUri(AssetClass.CloudFilePublic, $"ambient-videos/{hash}.mp4");
    }

    private async Task<string?> UploadVideoToAssets(string sourceUrl, string hash)
    {
        using var httpClient = new HttpClient();
        var videoBytes = await httpClient.GetByteArrayAsync(sourceUrl);

        var assetUri = GetVideoAssetUri(hash);
        await Asset.Instance.SetBytesAsync(assetUri, videoBytes, new AssetMetadata(mimeType: "video/mp4"));

        var metadata = await Asset.Instance.GetMetadataAsync(assetUri);
        return metadata.Url;
    }

    private async Task GenerateVideoForExperience(int index, AmbientExperience experience)
    {
        var hash = GetPromptHash(experience.VideoPrompt);

        var cachedVersions = await GetCachedVideoVersions(experience.VideoPrompt);
        if (cachedVersions != null)
        {
            if (_selectedIndex.Value == index)
            {
                var bestIndex = cachedVersions.Count - 1;
                _currentVideoUrl.Value = cachedVersions[bestIndex].Url;
                _videoVersions.Value = cachedVersions;
                _selectedVersionIndex.Value = bestIndex;
            }

            return;
        }

        var existingStatus = await GetGenerationStatus(hash);
        if (existingStatus != null)
        {
            if (_selectedIndex.Value == index)
            {
                _isVideoLoading.Value = existingStatus.IsGenerating;
                _isUpscaling.Value = existingStatus.IsUpscaling;
            }

            return;
        }

        if (_generatingPrompts.Contains(experience.VideoPrompt))
        {
            return;
        }

        _generatingPrompts.Add(experience.VideoPrompt);

        if (_selectedIndex.Value == index)
        {
            _isVideoLoading.Value = true;
            _videoError.Value = null;
        }

        try
        {
            await SetGenerationStatus(hash, isGenerating: true, isUpscaling: false);

            using var generator = new VideoGenerator(VideoGeneratorModel.Pollo20);
            var result = await generator.GenerateVideoAsync(new VideoGeneratorConfig
            {
                Prompt = experience.VideoPrompt,
                Resolution = VideoGeneratorResolution.Resolution1080p,
                AspectRatio = VideoGeneratorAspectRatio.Ratio16x9,
                Length = 10
            });

            var publicUrl = await UploadVideoToAssets(result.Url, hash);

            if (publicUrl != null)
            {
                var versions = new List<VideoVersion> { new("Original", publicUrl) };
                await CacheVideoVersions(experience.VideoPrompt, versions);
                await ClearGenerationStatus(hash);

                if (_selectedIndex.Value == index)
                {
                    _currentVideoUrl.Value = publicUrl;
                    _videoVersions.Value = versions;
                    _selectedVersionIndex.Value = 0;
                }
            }
            else
            {
                await ClearGenerationStatus(hash);

                if (_selectedIndex.Value == index)
                {
                    _videoError.Value = "Failed to upload video to assets";
                }
            }
        }
        catch (Exception ex)
        {
            await ClearGenerationStatus(hash);

            if (_selectedIndex.Value == index)
            {
                _videoError.Value = $"Failed to generate video: {ex.Message}";
            }
        }
        finally
        {
            _generatingPrompts.Remove(experience.VideoPrompt);

            if (_selectedIndex.Value == index)
            {
                _isVideoLoading.Value = false;
            }
        }
    }

    private async Task UpscaleVideo()
    {
        if (_selectedIndex.Value < 0 || _currentVideoUrl.Value == null || _currentVideoPrompt.Value == null || _isUpscaling.Value)
        {
            return;
        }

        var index = _selectedIndex.Value;
        var videoPrompt = _currentVideoPrompt.Value;
        var hash = GetPromptHash(videoPrompt);
        var sourceUrl = _currentVideoUrl.Value;

        _isUpscaling.Value = true;
        _videoError.Value = null;

        try
        {
            await SetGenerationStatus(hash, isGenerating: false, isUpscaling: true);

            // Step 1: FPS 8x
            using var fpsEnhancer = new VideoEnhancer(VideoEnhancerModel.TensorPixFpsBoost);
            var fpsResult = await fpsEnhancer.EnhanceVideoAsync(new VideoEnhancerConfig
            {
                VideoUrl = sourceUrl,
                TargetFps = 24 * 8,
                Timeout = TimeSpan.FromMinutes(30)
            });

            var fpsHash = $"{hash}-fps8x";
            var fpsUrl = await UploadVideoToAssets(fpsResult.Url, fpsHash);

            if (fpsUrl == null)
            {
                await ClearGenerationStatus(hash);

                if (_selectedIndex.Value == index)
                {
                    _videoError.Value = "Failed to upload FPS enhanced video";
                }

                return;
            }

            // Step 2: 4K upscale on the FPS-boosted video
            using var spatialEnhancer = new VideoEnhancer(VideoEnhancerModel.TensorPixUpscale2xUltra41);
            var spatialResult = await spatialEnhancer.EnhanceVideoAsync(new VideoEnhancerConfig
            {
                VideoUrl = fpsUrl,
                Timeout = TimeSpan.FromMinutes(30)
            });

            var upscaledHash = $"{hash}-upscaled";
            var upscaledUrl = await UploadVideoToAssets(spatialResult.Url, upscaledHash);

            if (upscaledUrl != null)
            {
                var versions = _videoVersions.Value.ToList();
                versions.Add(new VideoVersion("Upscaled", upscaledUrl));
                await CacheVideoVersions(videoPrompt, versions);
                await ClearGenerationStatus(hash);

                if (_selectedIndex.Value == index)
                {
                    _videoVersions.Value = versions;
                    _selectedVersionIndex.Value = versions.Count - 1;
                    _currentVideoUrl.Value = upscaledUrl;
                }
            }
            else
            {
                await ClearGenerationStatus(hash);

                if (_selectedIndex.Value == index)
                {
                    _videoError.Value = "Failed to upload upscaled video";
                }
            }
        }
        catch (Exception ex)
        {
            await ClearGenerationStatus(hash);
            Log.Instance.Error($"Upscale failed: {ex}");

            if (_selectedIndex.Value == index)
            {
                _videoError.Value = $"Upscale failed: {ex.Message}";
            }
        }
        finally
        {
            _isUpscaling.Value = false;
        }
    }

    private sealed record AmbientExperience(
        string Title,
        string Subtitle,
        string Description,
        string Accent,
        string Backdrop,
        string Atmosphere,
        string VideoPrompt,
        string AudioPrompt,
        bool IsCustom = false,
        string? CustomId = null);

    private sealed record CustomExperienceData(
        string Id,
        string Title,
        string Description,
        string VideoPrompt,
        string AudioPrompt,
        DateTime CreatedAt);
}
