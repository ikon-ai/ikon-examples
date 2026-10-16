using System.Diagnostics;
using Ikon.Common.Core.Protocol;
using SkiaSharp;

namespace Ikon.App.Patterns.Patterns;

// Pattern: rendered-video-feed — see docs/patterns/rendered-video-feed.md.
// Starting the feed runs ffmpeg, so a render in its default state shows the surface and the buttons only.
internal sealed class RenderedVideoFeed(IAppBase app) : IPatternDemo
{
    public string Slug => "rendered-video-feed";
    public string Title => "Server-drawn live video feed";
    public string Category => "Image & video";
    public void RenderDemo(IView view) => RenderBoard((UIView)view);

    #region example:pattern-rendered-video-feed
    private const int Width = 1280;
    private const int Height = 720;
    private const int Fps = 15;
    private const string Surface = "board";

    private Video Video { get; } = new(app);

    // What the frames show. The drawing loop reads it 15 times a second, so a change is on screen at once
    private readonly Reactive<int> _orders = new(0);
    private readonly Reactive<bool> _live = new(false);
    private readonly Reactive<string?> _error = new(null);
    private CancellationTokenSource? _feed;

    private void StartFeed()
    {
        if (_feed is not null)
        {
            return;
        }

        var feed = CancellationTokenSource.CreateLinkedTokenSource(app.StoppingToken);
        _feed = feed;
        _live.Value = true;
        _error.Value = null;
        _ = Task.Run(() => RunFeedAsync(feed));
    }

    private void StopFeed()
    {
        _feed?.Cancel();
        _feed = null;
        _live.Value = false;
    }

    private async Task RunFeedAsync(CancellationTokenSource feed)
    {
        var ct = feed.Token;
        Process? ffmpeg = null;

        try
        {
            // The server image has no fonts and Skia's Linux build looks for none, so the typeface
            // comes from a TTF shipped in data/fonts/. Without one, DrawText draws nothing.
            var fontBytes = await app.Files.Data.ReadBytesAsync("fonts/BarlowCondensed-Medium.ttf", ct);
            using var typeface = SKTypeface.FromData(SKData.CreateCopy(fontBytes));

            // Raw BGRA frames in on stdin, H.264 Constrained Baseline out on stdout
            ffmpeg = Process.Start(new ProcessStartInfo("ffmpeg")
            {
                ArgumentList =
                {
                    "-loglevel", "error",
                    "-f", "rawvideo", "-pix_fmt", "bgra", "-s", $"{Width}x{Height}", "-r", $"{Fps}", "-i", "pipe:0",
                    "-c:v", "libx264", "-preset", "ultrafast", "-tune", "zerolatency", "-profile:v", "baseline",
                    "-pix_fmt", "yuv420p", "-bf", "0", "-g", $"{Fps * 2}", "-x264-params", "repeat-headers=1",
                    "-f", "h264", "pipe:1",
                },
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            })!;

            await using var live = Video.PlayLive(MediaTargets.Everyone, Surface, VideoCodec.H264);

            // No token: the playout ends by itself when ffmpeg exits and its output closes
            var playout = live.WriteH264StreamAsync(ffmpeg.StandardOutput.BaseStream, frameRate: Fps);

            using var bitmap = new SKBitmap(new SKImageInfo(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul));
            using var canvas = new SKCanvas(bitmap);
            var pixels = new byte[Width * Height * 4];
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1.0 / Fps));
            var clock = Stopwatch.StartNew();

            try
            {
                // One frame per tick. The playout holds the stream to real time, so frames drawn
                // faster would only wait in the pipes and show late.
                while (await timer.WaitForNextTickAsync(ct))
                {
                    DrawFrame(canvas, typeface, clock.Elapsed.TotalSeconds);
                    bitmap.GetPixelSpan().CopyTo(pixels);
                    await ffmpeg.StandardInput.BaseStream.WriteAsync(pixels, ct);
                }
            }
            finally
            {
                if (!ffmpeg.HasExited)
                {
                    ffmpeg.Kill();
                }

                await playout;
            }
        }
        catch (Exception) when (ct.IsCancellationRequested)
        {
            // StopFeed or the app's shutdown cancelled the feed, and killing ffmpeg is the normal way it ends
        }
        catch (Exception ex)
        {
            // A Win32Exception means ffmpeg is not on PATH. Deployed apps have it, but a local run needs
            // it installed. An IOException means ffmpeg exited, and its error output says why.
            var reason = ex is IOException && ffmpeg is not null ? await ffmpeg.StandardError.ReadToEndAsync() : ex.Message;
            Log.Instance.Error($"Video feed '{Surface}' stopped: {reason}");

            if (_feed == feed)
            {
                _feed = null;
                _live.Value = false;
                _error.Value = reason;
            }
        }
        finally
        {
            ffmpeg?.Dispose();
        }
    }

    // Ordinary Skia drawing: text, shapes, images and paths all work as they do anywhere else
    private void DrawFrame(SKCanvas canvas, SKTypeface typeface, double seconds)
    {
        canvas.Clear(new SKColor(0x0B, 0x12, 0x20));
        using var font = new SKFont(typeface, 64);
        using var paint = new SKPaint { IsAntialias = true, Color = new SKColor(0x94, 0xA3, 0xB8) };

        canvas.DrawText("ORDERS TODAY", 80, 140, SKTextAlign.Left, font, paint);
        canvas.DrawText($"{DateTime.UtcNow:HH:mm:ss} UTC", Width - 80, 140, SKTextAlign.Right, font, paint);

        font.Size = 300;
        paint.Color = SKColors.White;
        canvas.DrawText($"{_orders.Value}", 80, 480, SKTextAlign.Left, font, paint);

        // A bar that fills every four seconds, so anyone watching can tell the picture is live
        paint.Color = new SKColor(0x34, 0xD3, 0x99);
        canvas.DrawRoundRect(80, Height - 120, (float)((Width - 160) * (seconds % 4 / 4)), 24, 12, 12, paint);
    }

    private void RenderBoard(UIView view)
    {
        view.Column(["gap-3 w-full max-w-3xl"], content: col =>
        {
            // Every viewer sees the same picture: the surface is the app's, not one client's
            col.VideoSurface(["aspect-video w-full rounded-lg bg-black"], surface: Surface, fit: VideoFit.Contain,
                placeholder: off => off.Text([Text.Caption, "text-white/70"], "The feed is off"));

            col.Row(["gap-2 items-center flex-wrap"], content: row =>
            {
                row.Button([Button.PrimaryMd], text: _live.Value ? "Stop the feed" : "Start the feed",
                    onClick: async () =>
                    {
                        if (_live.Value)
                        {
                            StopFeed();
                        }
                        else
                        {
                            StartFeed();
                        }
                    });
                row.Button([Button.OutlineMd], text: "Add an order", onClick: async () => _orders.Value++);
            });

            if (_error.Value is { } error)
            {
                col.Text([Text.Caption, "text-destructive"], $"The feed stopped: {error}");
            }
        });
    }
    #endregion
}
