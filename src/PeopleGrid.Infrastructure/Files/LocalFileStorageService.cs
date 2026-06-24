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
}
