namespace FortLauncher;

public static class HttpExtensions 
{
    public static async Task DownloadAsync(this HttpClient client, string requestUri, Stream dest, StringProgress progress = null, CancellationToken token = default) 
    {
        using var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead);
        long? length = response.Content.Headers.ContentLength;

        using var download = await response.Content.ReadAsStreamAsync();

        if (progress == null || !length.HasValue) 
        {
            await download.CopyToAsync(dest);
            return;
        }
        progress.TotalBytes = length.Value;

        var relProgress = new Progress<long>(totalBytes => progress.Report(totalBytes));
        await download.CopyToAsync(dest, 81920, relProgress, token);
    }

    public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, IProgress<long> progress = null, CancellationToken cancellationToken = default) {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (!source.CanRead)
            throw new ArgumentException("Has to be readable", nameof(source));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite)
            throw new ArgumentException("Has to be writable", nameof(destination));
        if (bufferSize < 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));

        var buffer = new byte[bufferSize];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0) 
        {
            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            totalBytesRead += bytesRead;
            progress?.Report(totalBytesRead);
        }
    }
}