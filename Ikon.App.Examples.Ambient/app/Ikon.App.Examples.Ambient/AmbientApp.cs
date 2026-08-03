using System.Security.Cryptography;
using System.Text;
using Ikon.AI.SoundEffectGeneration;
using Ikon.AI.VideoEnhancement;

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
    private readonly ReactiveList<VideoVersion> _videoVersions = new();
    private readonly Reactive<int> _selectedVersionIndex = new(0);
    private readonly Reactive<string?> _currentVideoPrompt = new(null);
    private readonly ReactiveList<CustomExperienceData> _customExperiences = new();
    private readonly Reactive<bool> _showCreateForm = new(false);
    private readonly Reactive<string> _createDescription = new("");
    private readonly Reactive<bool> _isCreating = new(false);
    private readonly ReactiveDictionary<string, string> _thumbnailUrls = new();
    private readonly Reactive<int> _featuredIndex = new(0);
    private readonly Reactive<int?> _hoveredIndex = new(null);

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
        UI = new UI(app, new IkonTheme());
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
        _videoVersions.Clear();
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
            var publicUrl = await UploadAudioToAssets(await result.GetDataAsync(), hash);

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

    private async Task PreloadThumbnailsAsync()
    {
        var experiences = GetAllExperiences();
        var map = new Dictionary<string, string>(_thumbnailUrls.Value);
        var upscaled = new HashSet<string>();

        foreach (var exp in experiences)
        {
            if (map.ContainsKey(exp.VideoPrompt))
            {
                continue;
            }

            try
            {
                var versions = await GetCachedVideoVersions(exp.VideoPrompt);

                if (versions != null && versions.Count > 0)
                {
                    var best = versions.FirstOrDefault(v => v.Name == "Upscaled");

                    if (best != null)
                    {
                        upscaled.Add(exp.VideoPrompt);
                    }
                    else
                    {
                        best = versions[^1];
                    }

                    map[exp.VideoPrompt] = best.Url;
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Warning($"Thumbnail preload failed for {exp.Title}: {ex.Message}");
            }
        }

        _thumbnailUrls.Value = map;

        var indexed = experiences.Select((e, i) => (exp: e, idx: i)).ToList();

        // 1. Prefer any experience with a true 4K upscaled version
        var preferred = indexed.FirstOrDefault(t => upscaled.Contains(t.exp.VideoPrompt));

        if (preferred.exp != null)
        {
            _featuredIndex.Value = preferred.idx;
            return;
        }

        // 2. Then prefer Desert if it has a cached video
        var desert = indexed.FirstOrDefault(t =>
            string.Equals(t.exp.Title, "Desert", StringComparison.OrdinalIgnoreCase) &&
            map.ContainsKey(t.exp.VideoPrompt));

        if (desert.exp != null)
        {
            _featuredIndex.Value = desert.idx;
            return;
        }

        // 3. Otherwise the first cached experience
        var fallback = indexed.FirstOrDefault(t => map.ContainsKey(t.exp.VideoPrompt));

        if (fallback.exp != null)
        {
            _featuredIndex.Value = fallback.idx;
        }
    }

    public async Task Main()
    {
        await LoadCustomExperiences();
        _ = Task.Run(async () => await PreloadThumbnailsAsync());

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
        var thumbnails = _thumbnailUrls.Value;
        var featuredIndex = Math.Clamp(_featuredIndex.Value, 0, experiences.Count - 1);
        var featured = experiences[featuredIndex];
        var builtIns = experiences.Select((e, i) => (exp: e, idx: i)).Where(t => !t.exp.IsCustom).ToList();
        var customs = experiences.Select((e, i) => (exp: e, idx: i)).Where(t => t.exp.IsCustom).ToList();

        view.Box([
            "min-h-screen w-full",
            "bg-[radial-gradient(ellipse_at_50%_-10%,rgba(34,211,238,0.05)_0%,transparent_50%),linear-gradient(180deg,#0a0a0e_0%,#050507_100%)]",
            "text-white relative overflow-hidden"
        ], content: shell =>
        {
            shell.Column(["relative z-10"], content: column =>
            {
                RenderTopNav(column);
                RenderHero(column, featured, featuredIndex, thumbnails);
                column.Column(["px-10 2xl:px-24 pt-10 pb-20 gap-12 relative z-20"], content: rails =>
                {
                    if (customs.Count > 0)
                    {
                        RenderRail(rails, "Your Creations", customs, thumbnails, featuredIndex);
                    }

                    RenderRail(rails, "Atmospheres", builtIns, thumbnails, featuredIndex);
                });
            });
        });
    }

    private void RenderTopNav(UIView parent)
    {
        parent.Row(["px-10 2xl:px-24 py-7 items-center justify-between"], content: nav =>
        {
            nav.Text(["text-white text-[22px] font-black tracking-[0.18em] leading-none"], "AMBIENT");

            nav.Button(
                [
                    "h-10 px-5 rounded-full backdrop-blur-2xl",
                    "bg-[linear-gradient(180deg,rgba(255,255,255,0.14),rgba(255,255,255,0.04))]",
                    "shadow-[0_10px_30px_-10px_rgba(0,0,0,0.6)]",
                    "text-white text-[11px] font-bold tracking-[0.22em] uppercase",
                    "hover:bg-[linear-gradient(180deg,rgba(255,255,255,0.22),rgba(255,255,255,0.08))]",
                    "transition-all duration-300"
                ],
                text: "+ Create Scene",
                onClick: async () => { _showCreateForm.Value = true; }
            );
        });
    }

    private void RenderHero(UIView parent, AmbientExperience featured, int index, IReadOnlyDictionary<string, string> thumbnails)
    {
        var hasThumb = thumbnails.TryGetValue(featured.VideoPrompt, out var thumbUrl);
        var experiences = GetAllExperiences();
        var navigable = experiences
            .Select((e, i) => (e, i))
            .Where(t => thumbnails.ContainsKey(t.e.VideoPrompt) || t.i == index)
            .OrderBy(t => string.Equals(t.e.Title, "Desert", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(t => t.i)
            .ToList();
        var posInNav = navigable.FindIndex(t => t.i == index);
        var prevIdx = navigable.Count > 1 && posInNav > 0
            ? navigable[posInNav - 1].i
            : (navigable.Count > 1 ? navigable[^1].i : index);
        var nextIdx = navigable.Count > 1 && posInNav < navigable.Count - 1
            ? navigable[posInNav + 1].i
            : (navigable.Count > 1 ? navigable[0].i : index);

        parent.Box([
            "px-10 2xl:px-24 pt-2 pb-0",
            "drop-shadow-[0_24px_40px_rgba(0,0,0,0.45)]"
        ], content: outer =>
        {
            outer.Box([
                "relative w-full h-[680px] overflow-hidden rounded-[28px]",
                featured.Backdrop
            ], content: hero =>
            {
                if (hasThumb && thumbUrl != null)
                {
                    hero.Box(["absolute inset-0"], content: media =>
                    {
                        media.AmbientVideoPlayer(
                            url: thumbUrl,
                            playbackRate: 0.5f,
                            loop: true,
                            muted: true,
                            autoplay: true,
                            thumbnail: true,
                            style: ["w-full h-full"],
                            key: $"hero-{index}");
                    });
                }

                // Soft directional gradients — no vignette, no inset frame
                hero.Box(["pointer-events-none absolute inset-0 bg-[linear-gradient(180deg,transparent_0%,transparent_45%,rgba(2,4,10,0.65)_100%)]"]);
                hero.Box(["pointer-events-none absolute inset-0 bg-[linear-gradient(90deg,rgba(2,4,10,0.7)_0%,rgba(2,4,10,0.25)_50%,transparent_75%)]"]);

                hero.Column([
                    "relative z-10 px-14 py-14 h-full justify-end max-w-[820px]",
                    "motion-[0:opacity-0_translate-y-[28px],100:opacity-100_translate-y-0] motion-duration-1000ms motion-fill-both motion-ease-[cubic-bezier(0.22,1,0.36,1)]"
                ], content: text =>
                {
                    text.Row(["items-center gap-4 mb-5 opacity-90"], content: meta =>
                    {
                        meta.Box([
                            "px-3 py-1 rounded-full backdrop-blur-2xl",
                            "bg-[linear-gradient(180deg,rgba(255,255,255,0.16),rgba(255,255,255,0.04))]"
                        ], content: badge =>
                        {
                            badge.Row(["items-center gap-2"], content: row =>
                            {
                                row.Box(["w-1.5 h-1.5 rounded-full bg-white/80"]);
                                row.Text(["text-white/85 text-[10px] font-bold tracking-[0.36em] uppercase"], "Featured");
                            });
                        });
                        meta.Text(["text-white/90 text-xs tracking-[0.32em] uppercase font-semibold"], featured.Subtitle);
                        meta.Box(["w-1 h-1 rounded-full bg-white/40"]);
                        meta.Text(["text-white/65 text-xs tracking-[0.28em] uppercase"], "Atmospheric Loop · 4K");
                    });

                    text.Text([
                        "text-white/95 text-[128px] leading-[0.88] font-black tracking-[-0.02em]",
                        "drop-shadow-[0_6px_40px_rgba(0,0,0,0.95)] mb-7",
                        featured.Accent
                    ], featured.Title);

                    text.Text(["text-white/75 text-2xl leading-snug max-w-[680px] mb-10 font-light drop-shadow-[0_2px_14px_rgba(0,0,0,0.9)]"], featured.Description);

                    text.Row(["items-center gap-3"], content: cta =>
                    {
                        cta.Button(
                            [
                                "h-14 px-10 bg-white hover:bg-white/95 text-zinc-950",
                                "font-bold tracking-[0.18em] text-sm rounded-full",
                                "shadow-[0_10px_30px_-12px_rgba(0,0,0,0.6)]",
                                "transition-all duration-300 hover:scale-[1.02]"
                            ],
                            text: "▶   PLAY",
                            onClick: async () => await SelectExperienceAsync(index)
                        );

                        cta.Button(
                            [
                                "h-14 px-7 backdrop-blur-2xl text-white",
                                "bg-[linear-gradient(180deg,rgba(255,255,255,0.18),rgba(255,255,255,0.05))]",
                                "shadow-[0_10px_30px_-10px_rgba(0,0,0,0.6)]",
                                "font-bold tracking-[0.18em] text-xs rounded-full",
                                "hover:bg-[linear-gradient(180deg,rgba(255,255,255,0.26),rgba(255,255,255,0.10))]",
                                "transition-all duration-300"
                            ],
                            text: "+  MY LIST",
                            onClick: async () => { }
                        );

                        cta.Button(
                            [
                                "h-14 w-14 backdrop-blur-2xl text-white",
                                "bg-[linear-gradient(180deg,rgba(255,255,255,0.14),rgba(255,255,255,0.04))]",
                                "shadow-[0_10px_30px_-10px_rgba(0,0,0,0.6)]",
                                "font-bold rounded-full text-base p-0",
                                "hover:bg-[linear-gradient(180deg,rgba(255,255,255,0.22),rgba(255,255,255,0.08))]",
                                "transition-all duration-300"
                            ],
                            text: "ⓘ",
                            onClick: async () => { }
                        );

                        if (!hasThumb)
                        {
                            cta.Box([
                                "ml-2 px-4 py-2.5 rounded-full backdrop-blur-2xl",
                                "bg-[linear-gradient(180deg,rgba(217,70,239,0.22),rgba(217,70,239,0.06))]"
                            ], content: hint =>
                            {
                                hint.Text(["text-pink-100/80 text-[10px] font-bold tracking-[0.28em] uppercase"], "Generates on Play");
                            });
                        }
                    });
                });

                // 2026-style minimal control cluster bottom-right: ‹ • • • • ›
                if (navigable.Count > 1)
                {
                    var capturedPrev = prevIdx;
                    var capturedNext = nextIdx;
                    hero.Row([
                        "absolute bottom-8 right-10 z-20 items-center gap-3",
                        "px-4 py-2.5 rounded-full backdrop-blur-2xl",
                        "bg-[linear-gradient(180deg,rgba(255,255,255,0.12),rgba(255,255,255,0.03))]",
                        "shadow-[0_10px_30px_-10px_rgba(0,0,0,0.6)]"
                    ], content: cluster =>
                    {
                        cluster.Box([
                            "h-7 w-7 rounded-full cursor-pointer flex items-center justify-center",
                            "text-white/80 hover:text-white hover:bg-white/10 transition-all"
                        ], onClick: async () => { _featuredIndex.Value = capturedPrev; }, content: btn =>
                        {
                            btn.Text(["text-base leading-none -mt-0.5"], "‹");
                        });

                        cluster.Row(["items-center gap-1.5"], content: dots =>
                        {
                            foreach (var (_, i) in navigable.Take(8))
                            {
                                var capturedIdx = i;
                                var isActive = capturedIdx == index;
                                var dotStyle = isActive
                                    ? "w-6 h-[3px] rounded-full bg-white/85 transition-all duration-500"
                                    : "w-1.5 h-[3px] rounded-full bg-white/30 hover:bg-white/60 transition-all duration-300";
                                dots.Box(
                                    [dotStyle, "cursor-pointer"],
                                    onClick: async () => { _featuredIndex.Value = capturedIdx; });
                            }
                        });

                        cluster.Box([
                            "h-7 w-7 rounded-full cursor-pointer flex items-center justify-center",
                            "text-white/80 hover:text-white hover:bg-white/10 transition-all"
                        ], onClick: async () => { _featuredIndex.Value = capturedNext; }, content: btn =>
                        {
                            btn.Text(["text-base leading-none -mt-0.5"], "›");
                        });
                    });
                }
            });
        });
    }

    private async Task SelectExperienceAsync(int index)
    {
        var experiences = GetAllExperiences();

        if (index < 0 || index >= experiences.Count)
        {
            return;
        }

        var experience = experiences[index];
        _selectedIndex.Value = index;
        _currentVideoPrompt.Value = experience.VideoPrompt;
        _ = GenerateVideoForExperience(index, experience);
        _ = GenerateAudioForExperience(index, experience);
    }

    private void RenderRail(UIView parent, string title, List<(AmbientExperience exp, int idx)> entries, IReadOnlyDictionary<string, string> thumbnails, int featuredIndex)
    {
        parent.Column(["gap-6"], content: section =>
        {
            section.Row(["items-end justify-between"], content: head =>
            {
                head.Text(["text-white/85 text-[15px] font-bold tracking-[0.04em]"], title);
                head.Text(["text-white/35 text-[10px] tracking-[0.32em] uppercase"], $"{entries.Count} scenes");
            });

            section.ScrollArea(
                scrollbars: ScrollAreaScrollbars.Horizontal,
                type: ScrollAreaType.Hover,
                rootStyle: ["w-full -mx-2"],
                viewportStyle: ["px-2 py-4"],
                scrollbarStyle: ["data-[orientation=horizontal]:h-1.5"],
                content: scroll =>
                {
                    scroll.Row(["gap-6 flex-nowrap"], content: row =>
                    {
                        foreach (var (exp, idx) in entries)
                        {
                            RenderSceneCard(row, exp, idx, thumbnails, idx == featuredIndex);
                        }

                        if (title == "Atmospheres")
                        {
                            RenderCreateCard(row);
                        }
                    });
                });
        });
    }

    private void RenderSceneCard(UIView parent, AmbientExperience exp, int index, IReadOnlyDictionary<string, string> thumbnails, bool isFeatured)
    {
        var hasThumb = thumbnails.TryGetValue(exp.VideoPrompt, out var thumbUrl);

        parent.Button(
            [
                "bg-transparent group relative w-[560px] min-w-[560px] h-[316px] rounded-2xl overflow-hidden cursor-pointer",
                "transition-all duration-500 ease-[cubic-bezier(0.22,1,0.36,1)]",
                "hover:scale-[1.03] hover:-translate-y-1",
                "shadow-[0_10px_28px_-12px_rgba(0,0,0,0.55)]",
                "hover:shadow-[0_18px_44px_-14px_rgba(0,0,0,0.7)]",
                "focus-visible:outline-none",
                "motion-[0:opacity-0_translate-y-[16px],100:opacity-100_translate-y-0] motion-duration-600ms motion-fill-both motion-ease-[cubic-bezier(0.22,1,0.36,1)]"
            ],
            onClick: async () =>
            {
                _featuredIndex.Value = index;
                await SelectExperienceAsync(index);
            },
            content: card =>
            {
                if (hasThumb && thumbUrl != null)
                {
                    card.AmbientVideoPlayer(
                        url: thumbUrl,
                        playbackRate: 0.25f,
                        loop: true,
                        muted: true,
                        autoplay: true,
                        thumbnail: true,
                        style: ["absolute inset-0 w-full h-full"],
                        key: $"thumb-{index}");
                }
                else
                {
                    card.Box(["absolute inset-0", exp.Backdrop]);
                    card.Box(["pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_30%_20%,rgba(255,255,255,0.08)_0%,transparent_60%)]"]);
                }

                // Cinematic darkening — lifts on hover so the thumbnail brightens
                card.Box(["pointer-events-none absolute inset-0 bg-black/45 group-hover:bg-black/15 transition-colors duration-500"]);
                card.Box(["pointer-events-none absolute inset-0 bg-[linear-gradient(180deg,rgba(2,4,10,0.65)_0%,rgba(2,4,10,0.3)_35%,rgba(2,4,10,0.6)_70%,rgba(2,4,10,0.95)_100%)] group-hover:opacity-60 transition-opacity duration-500"]);
                card.Box(["pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_50%_115%,rgba(0,0,0,0.65)_0%,transparent_60%)]"]);

                if (isFeatured)
                {
                    card.Box(["pointer-events-none absolute inset-0 rounded-2xl shadow-[inset_0_0_0_1.5px_rgba(255,255,255,0.35)]"]);
                }

                // Hover shimmer
                card.Box(["pointer-events-none absolute -inset-px rounded-2xl opacity-0 group-hover:opacity-100 transition-opacity duration-500 bg-[linear-gradient(135deg,transparent_40%,rgba(255,255,255,0.06)_50%,transparent_60%)]"]);

                if (hasThumb)
                {
                    card.Box([
                        "absolute top-4 left-4 z-10 px-2.5 py-1 rounded-full backdrop-blur-xl",
                        "bg-[linear-gradient(180deg,rgba(255,255,255,0.14),rgba(255,255,255,0.04))]"
                    ], content: tag =>
                    {
                        tag.Row(["items-center gap-1.5"], content: row =>
                        {
                            row.Box(["w-1.5 h-1.5 rounded-full bg-white/80"]);
                            row.Text(["text-white/85 text-[9px] font-bold tracking-[0.28em] uppercase"], "Ready");
                        });
                    });
                }

                if (exp.IsCustom)
                {
                    card.Box([
                        "absolute top-4 right-4 z-10 px-2.5 py-1 rounded-full backdrop-blur-xl",
                        "bg-[linear-gradient(180deg,rgba(255,255,255,0.10),rgba(255,255,255,0.02))]"
                    ], content: tag =>
                    {
                        tag.Text(["text-white/65 text-[9px] font-bold tracking-[0.28em] uppercase"], "Custom");
                    });
                }

                card.Column(["relative z-10 p-7 h-full justify-end gap-2"], content: body =>
                {
                    body.Text(["text-white/45 text-[11px] tracking-[0.32em] uppercase"], exp.Subtitle);
                    body.Text([
                        "text-white/85 text-[40px] font-black tracking-[-0.02em] leading-[1.02] line-clamp-2",
                        "drop-shadow-[0_2px_16px_rgba(0,0,0,0.9)]"
                    ], exp.Title);
                    body.Row(["items-center gap-2 mt-1"], content: meta =>
                    {
                        meta.Text(["text-zinc-400/70 text-[12px] tracking-[0.18em] uppercase"], exp.Atmosphere);
                    });
                });

                // Wide PLAY slice on hover, no circle, no border
                card.Box([
                    "pointer-events-none absolute left-7 right-7 bottom-7 z-20",
                    "opacity-0 translate-y-2 group-hover:opacity-100 group-hover:translate-y-0",
                    "transition-all duration-300 ease-out"
                ], content: slice =>
                {
                    slice.Row([
                        "items-center justify-center gap-3 h-11 rounded-full",
                        "bg-white/95 backdrop-blur-xl",
                        "shadow-[0_18px_40px_-12px_rgba(255,255,255,0.45)]"
                    ], content: row =>
                    {
                        row.Text(["text-zinc-950 text-sm leading-none"], "▶");
                        row.Text(["text-zinc-950 text-[11px] font-black tracking-[0.32em] uppercase"], "Play");
                    });
                });
            }
        );
    }

    private void RenderCreateCard(UIView parent)
    {
        parent.Button(
            [
                "group relative w-[560px] min-w-[560px] h-[316px] rounded-2xl overflow-hidden cursor-pointer",
                "bg-[linear-gradient(180deg,rgba(255,255,255,0.04),rgba(255,255,255,0.01))]",
                "shadow-[0_10px_28px_-12px_rgba(0,0,0,0.55)]",
                "hover:bg-[linear-gradient(180deg,rgba(255,255,255,0.08),rgba(255,255,255,0.02))]",
                "transition-all duration-500 ease-[cubic-bezier(0.22,1,0.36,1)] hover:scale-[1.03] hover:-translate-y-1",
                "focus-visible:outline-none"
            ],
            onClick: async () => { _showCreateForm.Value = true; },
            content: card =>
            {
                card.Column(["absolute inset-0 items-center justify-center gap-3"], content: inner =>
                {
                    inner.Box([
                        "w-12 h-12 rounded-full backdrop-blur-xl flex items-center justify-center",
                        "bg-[linear-gradient(180deg,rgba(255,255,255,0.12),rgba(255,255,255,0.03))]"
                    ], content: circle =>
                    {
                        circle.Text(["text-white/70 text-2xl font-extralight leading-none"], "+");
                    });
                    inner.Text(["text-white/80 text-sm font-bold tracking-[0.18em] uppercase"], "Create Scene");
                    inner.Text(["text-white/40 text-[10px] tracking-[0.22em] uppercase"], "Describe your own");
                });
            }
        );
    }

    private void RenderCreateForm(UIView view)
    {
        view.Box([
            "min-h-screen w-full",
            "bg-[radial-gradient(ellipse_at_50%_-10%,rgba(34,211,238,0.05)_0%,transparent_50%),linear-gradient(180deg,#0a0a0e_0%,#050507_100%)]",
            "text-white relative overflow-hidden"
        ], content: shell =>
        {
            shell.Column(["relative z-10 min-h-screen w-full"], content: column =>
            {
                column.Row(["px-10 2xl:px-24 py-7 items-center justify-between"], content: nav =>
                {
                    nav.Text(["text-white text-[22px] font-black tracking-[0.18em] leading-none"], "AMBIENT");
                    nav.Button(
                        [
                            "h-10 px-5 rounded-full backdrop-blur-2xl",
                            "bg-[linear-gradient(180deg,rgba(255,255,255,0.10),rgba(255,255,255,0.02))]",
                            "shadow-[0_10px_30px_-10px_rgba(0,0,0,0.6)]",
                            "text-white/80 text-[11px] font-bold tracking-[0.22em] uppercase",
                            "hover:bg-[linear-gradient(180deg,rgba(255,255,255,0.18),rgba(255,255,255,0.06))]",
                            "transition-all duration-300"
                        ],
                        text: "← Back",
                        onClick: async () =>
                        {
                            _showCreateForm.Value = false;
                            _createDescription.Value = "";
                        }
                    );
                });

                column.Column(["flex-1 px-10 2xl:px-24 pt-12 pb-20 max-w-3xl gap-10 motion-[0:opacity-0_translate-y-[20px],100:opacity-100_translate-y-0] motion-duration-800ms motion-fill-both motion-ease-[cubic-bezier(0.22,1,0.36,1)]"], content: form =>
                {
                    form.Column(["gap-3"], content: head =>
                    {
                        head.Text(["text-white/45 text-[10px] tracking-[0.4em] uppercase"], "Compose your own");
                        head.Text(["text-white/95 text-[64px] leading-[0.95] font-black tracking-[-0.02em]"], "Create Scene");
                        head.Text(["text-white/55 text-lg leading-snug max-w-2xl mt-2"], "Describe the place. We compose the visuals and the sound. Be specific — light, motion, texture, mood.");
                    });

                    form.TextArea(
                        [
                            "unstyled w-full h-44 px-5 py-4 rounded-2xl backdrop-blur-2xl",
                            "bg-[linear-gradient(180deg,rgba(255,255,255,0.06),rgba(255,255,255,0.015))]",
                            "shadow-[0_20px_60px_-20px_rgba(0,0,0,0.7)]",
                            "text-white/90 placeholder:text-white/30 text-base leading-relaxed",
                            "outline-none focus:bg-[linear-gradient(180deg,rgba(255,255,255,0.10),rgba(255,255,255,0.03))]",
                            "transition-all duration-300"
                        ],
                        placeholder: "A peaceful Japanese zen garden at dawn with cherry blossoms falling...",
                        value: _createDescription.Value,
                        onValueChange: async value => _createDescription.Value = value ?? ""
                    );

                    form.Row(["gap-3 items-center"], content: actions =>
                    {
                        actions.Button(
                            [
                                "h-14 px-10 bg-white hover:bg-white/95 text-zinc-950",
                                "font-black tracking-[0.18em] text-sm rounded-full",
                                "shadow-[0_18px_50px_-10px_rgba(255,255,255,0.4)]",
                                "transition-all duration-300 hover:scale-[1.03]",
                                "disabled:opacity-40 disabled:hover:scale-100"
                            ],
                            text: _isCreating.Value ? "Creating..." : "▶  Create Scene",
                            disabled: _isCreating.Value || string.IsNullOrWhiteSpace(_createDescription.Value),
                            onClick: async () => await CreateCustomExperience()
                        );

                        actions.Button(
                            [
                                "h-14 px-7 backdrop-blur-2xl text-white/80",
                                "bg-[linear-gradient(180deg,rgba(255,255,255,0.10),rgba(255,255,255,0.02))]",
                                "shadow-[0_10px_30px_-10px_rgba(0,0,0,0.6)]",
                                "font-bold tracking-[0.18em] text-xs rounded-full",
                                "hover:bg-[linear-gradient(180deg,rgba(255,255,255,0.18),rgba(255,255,255,0.06))]",
                                "transition-all duration-300"
                            ],
                            text: "Cancel",
                            onClick: async () =>
                            {
                                _showCreateForm.Value = false;
                                _createDescription.Value = "";
                            }
                        );
                    });
                });
            });
        });
    }

    private void RenderExperience(UIView view, AmbientExperience experience)
    {
        view.Box([
            "min-h-screen w-full",
            "bg-[radial-gradient(ellipse_at_50%_-10%,rgba(34,211,238,0.04)_0%,transparent_50%),linear-gradient(180deg,#0a0a0e_0%,#050507_100%)]",
            "text-white relative overflow-hidden"
        ], content: shell =>
        {
            shell.Column(["relative z-10 min-h-screen w-full px-10 2xl:px-24 py-7 gap-7"], content: view =>
            {
            view.Row(["items-center justify-between gap-6"], content: view =>
            {
                view.Column(["gap-2 min-w-0 flex-1"], content: view =>
                {
                    view.Text(["text-white/45 text-[10px] tracking-[0.4em] uppercase"], experience.Subtitle);
                    view.Text([
                        "text-white/95 text-[56px] leading-[0.95] font-black tracking-[-0.02em]",
                        experience.Accent,
                        "motion-[0:opacity-0_translate-y-[10px],100:opacity-100_translate-y-0] motion-duration-500ms motion-fill-both"
                    ], experience.Title);
                    view.Text(["text-white/55 text-base leading-snug max-w-xl mt-1"], experience.Description);
                });

                view.Row(["gap-3 flex-shrink-0 items-center flex-wrap"], content: view =>
                {
                    var hasUpscaled = _videoVersions.Value.Any(v => v.Name == "Upscaled");
                    var canUpscale = _currentVideoUrl.Value != null && !_isVideoLoading.Value && !_isUpscaling.Value && !hasUpscaled;

                    if (!hasUpscaled)
                    {
                        var upscaleLabel = _isUpscaling.Value ? "Upscaling..." : "↑  Upscale";

                        view.Button(
                            [
                                "h-11 px-6 rounded-full backdrop-blur-2xl flex-shrink-0",
                                "font-bold tracking-[0.18em] text-[11px] uppercase",
                                "shadow-[0_10px_30px_-10px_rgba(0,0,0,0.6)]",
                                "transition-all duration-300",
                                canUpscale
                                    ? "bg-[linear-gradient(180deg,rgba(167,139,250,0.32),rgba(167,139,250,0.10))] text-violet-100 hover:bg-[linear-gradient(180deg,rgba(167,139,250,0.42),rgba(167,139,250,0.16))]"
                                    : "bg-[linear-gradient(180deg,rgba(255,255,255,0.06),rgba(255,255,255,0.02))] text-white/40"
                            ],
                            text: upscaleLabel,
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
                            "h-11 px-6 rounded-full backdrop-blur-2xl flex-shrink-0",
                            "bg-[linear-gradient(180deg,rgba(255,255,255,0.14),rgba(255,255,255,0.04))]",
                            "shadow-[0_10px_30px_-10px_rgba(0,0,0,0.6)]",
                            "text-white/85 font-bold tracking-[0.18em] text-[11px] uppercase",
                            "hover:bg-[linear-gradient(180deg,rgba(255,255,255,0.22),rgba(255,255,255,0.08))]",
                            "transition-all duration-300"
                        ],
                        text: "←  Back",
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
                            _videoVersions.Clear();
                            _selectedVersionIndex.Value = 0;
                        }
                    );

                    if (experience.IsCustom)
                    {
                        view.Button(
                            [
                                "h-11 px-6 rounded-full backdrop-blur-2xl flex-shrink-0",
                                "bg-[linear-gradient(180deg,rgba(244,63,94,0.28),rgba(244,63,94,0.08))]",
                                "shadow-[0_10px_30px_-10px_rgba(0,0,0,0.6)]",
                                "text-rose-100/90 font-bold tracking-[0.18em] text-[11px] uppercase",
                                "hover:bg-[linear-gradient(180deg,rgba(244,63,94,0.38),rgba(244,63,94,0.14))]",
                                "transition-all duration-300"
                            ],
                            text: "Delete",
                            onClick: async () => await DeleteCustomExperience(experience.CustomId!)
                        );
                    }
                });
            });

            view.Box([
                "relative flex-1 w-full rounded-[28px] overflow-hidden",
                "bg-[linear-gradient(180deg,rgba(255,255,255,0.04),rgba(255,255,255,0.01))]"
            ],
            content: view =>
            {
                view.Box(["absolute inset-0 flex items-center justify-center"], content: view =>
                {
                    if (_isVideoLoading.Value)
                    {
                        view.Column(["items-center text-center gap-4"], content: view =>
                        {
                            view.Box([
                                "w-10 h-10 border-2 border-white/15 border-t-white/70 rounded-full",
                                "animate-spin"
                            ]);
                            view.Text(["text-[10px] uppercase tracking-[0.4em] text-white/40 mt-4"], "Generating");
                            view.Text(["text-2xl font-light text-white/90 tracking-tight"], "Creating your scene");
                            view.Text(["text-sm text-white/45 max-w-md text-center"], "This usually takes a minute or two");
                        });
                    }
                    else if (_videoError.Value != null)
                    {
                        view.Column(["items-center text-center px-8 gap-3"], content: view =>
                        {
                            view.Text(["text-[10px] uppercase tracking-[0.4em] text-rose-300/80"], "Something went wrong");
                            view.Text(["text-2xl font-light text-white/90 tracking-tight"], "We couldn't generate that scene");
                            view.Text(["text-sm text-white/45 max-w-md text-center break-words"], _videoError.Value);
                        });
                    }
                    else if (_currentVideoUrl.Value == null)
                    {
                        view.Column(["items-center text-center gap-3"], content: view =>
                        {
                            view.Text(["text-[10px] uppercase tracking-[0.4em] text-white/40"], "Idle");
                            view.Text(["text-2xl font-light text-white/85 tracking-tight"], "Ambient feed will play here");
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

            view.Row(["justify-between text-[10px] uppercase tracking-[0.36em] text-white/35"], content: view =>
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
                Url = sourceUrl,
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
                Url = fpsUrl,
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
