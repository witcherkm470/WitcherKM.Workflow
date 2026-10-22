using System.Net;
using System.Net.Sockets;

namespace WitcherKM.Workflow.Infrastructure;

public interface IExternalImageFetcher
{
    Task<(byte[] Bytes, string ContentType)> Download(string sourceUrl, CancellationToken ct);
}

public sealed class ExternalImageDownloader : IExternalImageFetcher
{
    private const int MaxBytes = 10 * 1024 * 1024;
    private static readonly HttpClient Client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        UseProxy = false,
        ConnectCallback = ConnectPublic
    }) { Timeout = TimeSpan.FromSeconds(20) };

    public async Task<(byte[] Bytes, string ContentType)> Download(string sourceUrl, CancellationToken ct)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
            throw new HttpRequestException("Недопустимая ссылка на изображение.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
        for (var redirect = 0; redirect <= 3; redirect++)
        {
            if (uri.AbsoluteUri.Length > 4096 || uri.Scheme is not ("http" or "https") ||
                !string.IsNullOrEmpty(uri.UserInfo))
                throw new HttpRequestException("Допустимы только публичные HTTP(S) ссылки на изображения.");

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linked.Token);
            if ((int)response.StatusCode is 301 or 302 or 303 or 307 or 308)
            {
                if (response.Headers.Location is null || redirect == 3)
                    throw new HttpRequestException("Слишком много перенаправлений изображения.");
                uri = new Uri(uri, response.Headers.Location);
                continue;
            }
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Источник вернул HTTP {(int)response.StatusCode}.");
            if (response.Content.Headers.ContentLength > MaxBytes)
                throw new IOException("Изображение превышает 10 МБ.");

            await using var source = await response.Content.ReadAsStreamAsync(linked.Token);
            using var output = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = await source.ReadAsync(buffer, linked.Token)) != 0)
            {
                if (output.Length + read > MaxBytes) throw new IOException("Изображение превышает 10 МБ.");
                await output.WriteAsync(buffer.AsMemory(0, read), linked.Token);
            }
            var bytes = output.ToArray();
            return (bytes, DetectContentType(bytes));
        }
        throw new HttpRequestException("Слишком много перенаправлений изображения.");
    }

    private static string DetectContentType(byte[] bytes)
    {
        if (bytes.AsSpan().StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]))
            return "image/png";
        if (bytes.AsSpan().StartsWith((ReadOnlySpan<byte>)[0xff, 0xd8, 0xff])) return "image/jpeg";
        if (bytes.AsSpan().StartsWith("GIF87a"u8) || bytes.AsSpan().StartsWith("GIF89a"u8))
            return "image/gif";
        if (bytes.Length >= 12 && bytes.AsSpan().StartsWith("RIFF"u8) &&
            bytes.AsSpan(8).StartsWith("WEBP"u8)) return "image/webp";
        throw new IOException("Ссылка не содержит PNG, JPEG, GIF или WebP изображение.");
    }

    private static async ValueTask<Stream> ConnectPublic(SocketsHttpConnectionContext context, CancellationToken ct)
    {
        var host = context.DnsEndPoint.Host;
        var addresses = await Dns.GetHostAddressesAsync(host, ct);
        if (addresses.Length == 0 || addresses.Any(address => !IsPublic(address)))
            throw new HttpRequestException("Адрес источника недоступен для загрузки.");

        var address = addresses.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork) ?? addresses[0];
        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(address, context.DnsEndPoint.Port, ct);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static bool IsPublic(IPAddress value)
    {
        if (value.IsIPv4MappedToIPv6) value = value.MapToIPv4();
        if (value.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = value.GetAddressBytes();
            return (bytes[0] & 0xe0) == 0x20 && !value.IsIPv6LinkLocal && !value.IsIPv6Multicast;
        }
        if (value.AddressFamily != AddressFamily.InterNetwork) return false;
        var b = value.GetAddressBytes();
        return b[0] is > 0 and < 224 &&
               b[0] != 10 && b[0] != 127 &&
               !(b[0] == 100 && b[1] is >= 64 and <= 127) &&
               !(b[0] == 169 && b[1] == 254) &&
               !(b[0] == 172 && b[1] is >= 16 and <= 31) &&
               !(b[0] == 192 && (b[1] == 168 || b[1] == 0 || b[1] == 88)) &&
               !(b[0] == 198 && (b[1] is 18 or 19 || b[1] == 51 && b[2] == 100)) &&
               !(b[0] == 203 && b[1] == 0 && b[2] == 113);
    }
}
