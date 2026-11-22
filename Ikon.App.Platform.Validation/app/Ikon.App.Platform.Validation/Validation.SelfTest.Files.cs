public partial class Validation
{
    // A public repo file whose name needs URL escaping. Deployed, the app reads it back from its own
    // URL, so this also proves the frontend serves such a name
    private const string SelfTestOddPublicFile = "validation/take #2.txt";
    private const string SelfTestOddPublicText = "validation take #2";

    private static readonly HttpClient _selfTestFilesHttp = new() { Timeout = TimeSpan.FromSeconds(8) };

    private async Task<string> SelfTestRepoFilesAsync()
    {
        var dataFiles = app.Files.Data.ListRepoFiles();
        Expect(dataFiles.Contains("audio.raw") && dataFiles.Contains("whoosh.mp3"), $"data/ lists {string.Join(", ", dataFiles)}, without audio.raw and whoosh.mp3");

        // audio.raw is offloaded to the Asset store. A deployment or a run that failed to restore it
        // would hand the app the pointer's JSON instead of the audio
        var audio = await app.Files.Data.ReadBytesAsync("audio.raw");
        Expect(audio.Length > 1024 && Ikon.Common.Assets.AssetPointer.TryParse(System.Text.Encoding.UTF8.GetString(audio)) is null, $"data/audio.raw holds {audio.Length} bytes that are not the restored audio");
        var streamedAudio = await ReadStreamedAsync(app.Files.Data, "audio.raw");
        Expect(streamedAudio.AsSpan().SequenceEqual(audio), $"OpenReadAsync gave {streamedAudio.Length} bytes for data/audio.raw, ReadBytesAsync {audio.Length}");

        await ExpectThrowsAsync<InvalidOperationException>(() => app.Files.Data.GetUrlAsync("whoosh.mp3"), "a URL for a private repo file");

        var publicFiles = app.Files.Public.ListRepoFiles();
        Expect(publicFiles.Contains("favicon-32.png") && publicFiles.Contains(SelfTestOddPublicFile), $"public/ lists {publicFiles.Count} files, without favicon-32.png or {SelfTestOddPublicFile}");

        var url = await app.Files.Public.GetUrlAsync(SelfTestOddPublicFile);
        Expect(url == "/validation/take%20%232.txt", $"GetUrlAsync gave {url} for {SelfTestOddPublicFile}");

        var text = await app.Files.Public.ReadTextAsync(SelfTestOddPublicFile);
        Expect(text == SelfTestOddPublicText, $"{SelfTestOddPublicFile} read as '{OneLine(text)}'");
        var streamedText = System.Text.Encoding.UTF8.GetString(await ReadStreamedAsync(app.Files.Public, SelfTestOddPublicFile));
        Expect(streamedText == SelfTestOddPublicText, $"OpenReadAsync read {SelfTestOddPublicFile} as '{OneLine(streamedText)}'");
        Expect(!await app.Files.Public.ExistsAsync("validation/no-such-file.txt"), "a missing public file was reported to exist");

        await ExpectThrowsAsync<ArgumentException>(() => app.Files.Data.ReadBytesAsync("../ikon-config.development.toml"), "a path with '..'");

        return $"data/ lists {dataFiles.Count} files with audio.raw restored ({audio.Length} bytes, the same streamed); public/ lists {publicFiles.Count}, and '{SelfTestOddPublicFile}' has URL {url} and reads back whole and streamed; '..' refused";
    }

    private static async Task<byte[]> ReadStreamedAsync(AppFileTree tree, string path)
    {
        await using var stream = await tree.OpenReadAsync(path);
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        return copy.ToArray();
    }

    private async Task<string> SelfTestDataFilesAsync()
    {
        var path = $"selftest/{Guid.NewGuid():N}.bin";
        var bytes = new byte[4096];
        Random.Shared.NextBytes(bytes);
        bytes[0] = 0;

        try
        {
            await app.Files.Data.WriteBytesAsync(path, bytes, "application/octet-stream");
            Expect(await app.Files.Data.ExistsAsync(path), $"{path} does not exist after the write");
            Expect((await app.Files.Data.ReadBytesAsync(path)).AsSpan().SequenceEqual(bytes), $"{path} read back other bytes");
            Expect((await ReadStreamedAsync(app.Files.Data, path)).AsSpan().SequenceEqual(bytes), $"OpenReadAsync read {path} back as other bytes");
            Expect((await app.Files.Data.ListAsync("selftest")).Contains(path), $"ListAsync(\"selftest\") does not list {path}");

            var signed = await app.Files.Data.GetUrlAsync(path);
            Expect((await _selfTestFilesHttp.GetByteArrayAsync(signed)).AsSpan().SequenceEqual(bytes), $"the signed URL of {path} served other bytes");
        }
        finally
        {
            await app.Files.Data.DeleteAsync(path);
        }

        Expect(!await app.Files.Data.ExistsAsync(path), $"{path} still exists after the delete");
        return $"{path}: written, listed, read back whole and streamed, fetched by signed URL and deleted";
    }

    private async Task<string> SelfTestPublicFilesAsync()
    {
        var path = $"selftest/{Guid.NewGuid():N} #1.txt";
        var text = $"written {DateTime.UtcNow:O}";

        try
        {
            await app.Files.Public.WriteTextAsync(path, text);
            Expect(await app.Files.Public.ReadTextAsync(path) == text, $"{path} read back other text");

            var absolute = await app.Files.Public.GetAbsoluteUrlAsync(path);
            Expect(await _selfTestFilesHttp.GetStringAsync(absolute) == text, $"{absolute} served other text");
        }
        finally
        {
            await app.Files.Public.DeleteAsync(path);
        }

        Expect(!await app.Files.Public.ExistsAsync(path), $"{path} still exists after the delete");
        return $"'{path}': written, read back, fetched by its absolute URL and deleted";
    }
}
