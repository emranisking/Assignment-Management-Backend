using AssignmentManagement.Application.Common.Interfaces;

namespace AssignmentManagement.Infrastructure.Storage;

public class FileStorageOptions
{
    public string RootPath { get; set; } = "storage";
}

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;

    public LocalFileStorageService(FileStorageOptions options)
    {
        _root = Path.IsPathRooted(options.RootPath)
            ? options.RootPath
            : Path.Combine(Directory.GetCurrentDirectory(), options.RootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredFile> SaveAsync(Stream content, string originalFileName, string contentType,
        string relativeFolder, CancellationToken ct = default)
    {
        var folder = Path.Combine(_root, relativeFolder);
        Directory.CreateDirectory(folder);

        var safeName = $"{Guid.NewGuid():N}{Path.GetExtension(originalFileName)}";
        var fullPath = Path.Combine(folder, safeName);

        await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(fs, ct);
        }

        var info = new FileInfo(fullPath);
        return new StoredFile
        {
            FilePath = Path.GetRelativePath(_root, fullPath),
            FileName = originalFileName,
            FileSize = info.Length,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/pdf" : contentType
        };
    }

    public Task<Stream> OpenReadAsync(string filePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_root, filePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Stored file not found.", filePath);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }
}
