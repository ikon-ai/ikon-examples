public partial class Validation
{
    // Live playbacks that are never written to: a viewer is announced nothing, so a client connected
    // while the battery runs sees none of it
    private async Task<string> SelfTestVideoPlaybackAsync()
    {
        const string held = "validation-selftest-held";
        const string unheard = "validation-selftest-unheard";

        Expect(MediaTargets.EveryoneExcept(7).Includes(9) && !MediaTargets.EveryoneExcept(7).Includes(7), "EveryoneExcept(7) did not include 9 and leave out 7");
        Expect(MediaTargets.EveryoneExcept(7).Resolve([7, 8, 9]).SessionIds is [8, 9], "EveryoneExcept(7) did not resolve [7, 8, 9] to [8, 9]");
        Expect(MediaTargets.EveryoneExcept().IsEveryone, "EveryoneExcept with no ids is not Everyone");

        await using var other = new Video(app);

        var nobody = Video.PlayLive(MediaTargets.To(), unheard, VideoCodec.H264);
        var nobodyOutcome = await nobody.Completion.WaitAsync(TimeSpan.FromSeconds(5));
        Expect(nobodyOutcome == VideoPlaybackOutcome.NoViewers, $"a playback to no one ended {nobodyOutcome}, not NoViewers");

        await using (var taken = other.PlayLive(MediaTargets.Everyone, unheard, VideoCodec.H264))
        {
            Expect(!taken.IsEnded, "a surface a playback to no one had claimed was not free for another Video");
        }

        await using var live = Video.PlayLive(MediaTargets.Everyone, held, VideoCodec.H264);
        ExpectThrows<InvalidOperationException>(() => other.PlayLive(MediaTargets.Everyone, held, VideoCodec.H264), "another Video playing on a surface in use");
        Expect(Video.GetPlayback(held) == live, "GetPlayback did not return the surface's playback");
        Expect(live.Input == null, "a live playback reported an input");

        Video.Stop(MediaTargets.To(424242), held);
        Expect(!live.IsEnded && live.Audience.IsEveryoneExcept, $"Stop for one client turned Everyone into {live.Audience}, not EveryoneExcept, or ended it");

        live.Stop();
        var stoppedOutcome = await live.Completion.WaitAsync(TimeSpan.FromSeconds(5));
        Expect(stoppedOutcome == VideoPlaybackOutcome.Stopped, $"Stop ended the playback {stoppedOutcome}");
        Expect(Video.GetPlayback(held) == null, "a stopped surface still reported a playback");

        return "EveryoneExcept includes and resolves; a playback to no one ends NoViewers and frees its surface; a surface in use refuses another Video; Stop for one client keeps Everyone open as EveryoneExcept";
    }
}
