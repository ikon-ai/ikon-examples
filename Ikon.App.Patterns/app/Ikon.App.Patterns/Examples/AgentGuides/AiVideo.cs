namespace Ikon.App.Patterns.Examples;

internal sealed partial class AgentGuideExamples
{

    private static async Task<string> DocVideoGenerateOneShotAsync()
    {
        #region example:video-generate-one-shot
        var video = await VideoGenerator.GenerateAsync("A timelapse of a flower blooming");  // Veo31Fast (cheap+fast) by default
        // video.Url (string)
        #endregion

        return video.Url;
    }

    private static async Task<string> DocVideoGenerateConfigAsync()
    {
        #region example:video-generate-config
        using var generator = new VideoGenerator(VideoGeneratorModel.Veo31);
        var result = await generator.GenerateVideoAsync(new VideoGeneratorConfig
        {
            Prompt = "A timelapse of a flower blooming",
            AspectRatio = VideoGeneratorAspectRatio.Ratio16x9,
            Length = 6  // Veo31 supports lengths 4, 6, and 8 — any other length is refused
        });
        // result.Url (string)
        #endregion

        return result.Url;
    }

    private static async Task<string> DocVideoEnhanceOneShotAsync(string clipUrl)
    {
        #region example:video-enhance-one-shot
        var enhanced = await VideoEnhancer.EnhanceAsync(clipUrl);
        // enhanced.Url (string), enhanced.OutputFps, enhanced.OutputSizeBytes
        #endregion

        return enhanced.Url;
    }

    private static async Task<string> DocVideoEnhanceConfigAsync(byte[] videoBytes)
    {
        #region example:video-enhance-config
        using var enhancer = new VideoEnhancer(VideoEnhancerModel.TensorPixUpscale2xUltra41);
        var result = await enhancer.EnhanceVideoAsync(new VideoEnhancerConfig
        {
            Data = videoBytes,
            MimeType = "video/mp4"
        });
        // result.Url (string), result.OutputFps, result.OutputSizeBytes
        #endregion

        return result.Url;
    }

    private static void DocVideoPlayback(UIView view, DocClip clip)
    {
        #region example:video-playback
        view.VideoUrlPlayer(
            ["w-full rounded-xl"],
            url: clip.Url,
            controls: true,
            autoplay: false,
            loop: false,
            muted: false,
            poster: clip.PosterUrl);  // optional still-frame shown before play
        #endregion
    }

    private static void DocVideoFeedSlides(UIView view, IReadOnlyList<DocClip> clips)
    {
        #region example:video-feed-slides
        // MediaUrl only preloads, in a hidden element. The slide's own Content draws the player
        var slides = clips.Select(clip => new FeedSlide(
            Content: slide => slide.VideoUrlPlayer(["h-full w-full object-cover"], url: clip.Url, poster: clip.PosterUrl, loop: true, controls: false),
            Key: clip.Url,
            MediaKind: FeedMediaKind.Video,
            MediaUrl: clip.Url)).ToList();

        view.FeedScroller(slides, style: [FeedScroller.Root, "h-dvh"]);
        #endregion
    }
}
