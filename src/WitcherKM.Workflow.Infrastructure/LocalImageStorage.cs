using System.Text;
using System.Text.RegularExpressions;
using WitcherKM.Workflow.Application;

namespace WitcherKM.Workflow.Infrastructure;

public sealed class LocalImageStorage(string dataDirectory, IWorkflowStore store, IExternalImageFetcher fetcher) : IImageStorage
{
    private static readonly Regex LocalLinks = new(@"/api/images/(?<name>[a-f0-9]{32}\.(?:png|jpg|gif|webp))", RegexOptions.Compiled);
    private static readonly Regex RemoteImages = new(@"!\[[^\]\r\n]*\]\((?<url>https?://[^\s)\r\n]+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Fences = new(@"(?m)^[ \t]{0,3}(?<marker>`{3,}|~{3,})[^\r\n]*(?:\r?\n|$)", RegexOptions.Compiled);
    private static readonly SemaphoreSlim CacheGate = new(1, 1);
    private readonly string _directory = Path.Combine(dataDirectory, "images");

    public async Task<string> Save(Stream content, string contentType, CancellationToken ct)
    {
        var extension = contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => throw new WorkflowException(400, "Поддерживаются PNG, JPEG, GIF и WebP.")
        };
        Directory.CreateDirectory(_directory);
        var name = Guid.NewGuid().ToString("N") + extension;
        await using var file = File.Create(Path.Combine(_directory, name));
        await content.CopyToAsync(file, ct);
        return "/api/images/" + name;
    }

    public async Task<LocalizedImages> Localize(string markdown, CancellationToken ct)
    {
        var result = new StringBuilder(markdown.Length);
        var warnings = new List<string>();
        var resolved = new Dictionary<string, string>(StringComparer.Ordinal);
        var fenced = FenceRanges(markdown);
        var cursor = 0;
        foreach (Match match in RemoteImages.Matches(markdown))
        {
            if (fenced.Any(range => match.Index >= range.Start && match.Index < range.End)) continue;
            var url = match.Groups["url"];
            if (!resolved.TryGetValue(url.Value, out var localUrl))
            {
                try { localUrl = await DownloadAndCache(url.Value, ct); }
                catch (Exception e) when (!ct.IsCancellationRequested &&
                                          e is HttpRequestException or OperationCanceledException or IOException or WorkflowException)
                {
                    localUrl = url.Value;
                    warnings.Add($"Не удалось сохранить изображение {url.Value}: {e.Message}");
                }
                resolved[url.Value] = localUrl;
            }
            result.Append(markdown, cursor, url.Index - cursor);
            result.Append(localUrl);
            cursor = url.Index + url.Length;
        }
        result.Append(markdown, cursor, markdown.Length - cursor);
        return new LocalizedImages(result.ToString(), warnings);
    }

    private async Task<string> DownloadAndCache(string sourceUrl, CancellationToken ct)
    {
        await CacheGate.WaitAsync(ct);
        try
        {
            var cached = await store.CachedImage(sourceUrl, ct);
            if (cached is not null)
            {
                if (Get(Path.GetFileName(cached)) is not null) return cached;
                await store.ForgetImage(cached, ct);
            }
            var image = await fetcher.Download(sourceUrl, ct);
            await using var stream = new MemoryStream(image.Bytes, writable: false);
            var localUrl = await Save(stream, image.ContentType, ct);
            try { await store.RememberImage(sourceUrl, localUrl, ct); }
            catch
            {
                await DeleteUnreferenced(localUrl, CancellationToken.None);
                throw;
            }
            return localUrl;
        }
        finally { CacheGate.Release(); }
    }

    private static List<(int Start, int End)> FenceRanges(string markdown)
    {
        var ranges = new List<(int Start, int End)>();
        char? marker = null;
        var length = 0;
        var start = 0;
        foreach (Match line in Fences.Matches(markdown))
        {
            var fence = line.Groups["marker"].Value;
            if (marker is null)
            {
                marker = fence[0];
                length = fence.Length;
                start = line.Index;
            }
            else if (fence[0] == marker && fence.Length >= length &&
                     line.Value.Trim().Length == fence.Length)
            {
                ranges.Add((start, line.Index + line.Length));
                marker = null;
            }
        }
        if (marker is not null) ranges.Add((start, markdown.Length));
        return ranges;
    }

    public async Task DeleteUnreferenced(string oldMarkdown, CancellationToken ct)
    {
        foreach (Match match in LocalLinks.Matches(oldMarkdown).Cast<Match>().DistinctBy(x => x.Value))
        {
            if (await store.ImageIsReferenced(match.Value, ct)) continue;
            var file = Path.Combine(_directory, match.Groups["name"].Value);
            if (File.Exists(file)) File.Delete(file);
            await store.ForgetImage(match.Value, ct);
        }
    }

    public (string Path, string ContentType)? Get(string fileName)
    {
        if (!Regex.IsMatch(fileName, @"^[a-f0-9]{32}\.(png|jpg|gif|webp)$")) return null;
        var path = Path.Combine(_directory, fileName);
        if (!File.Exists(path)) return null;
        var contentType = Path.GetExtension(fileName) switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "image/webp"
        };
        return (path, contentType);
    }
}
