using PeopleGrid.Application.Abstractions;

namespace PeopleGrid.Infrastructure.Files;

public sealed class LocalFileStorageService : IFileStorageService
{
    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var key = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var folder = Path.Combine(AppContext.BaseDirectory, "uploads");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, key);
        await using var file = File.Create(path);
        await stream.CopyToAsync(file, cancellationToken);
        return key;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "uploads");
        var path = Path.Combine(folder, Path.GetFileName(storageKey));
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Stored file was not found.", storageKey);
        }

        return Task.FromResult<Stream>(File.OpenRead(path));
    }
}
