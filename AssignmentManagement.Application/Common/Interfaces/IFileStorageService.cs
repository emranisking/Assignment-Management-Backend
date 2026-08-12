namespace AssignmentManagement.Application.Common.Interfaces;

public class StoredFile
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
}

public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(Stream content, string originalFileName, string contentType,
        string relativeFolder, CancellationToken ct = default);

    Task<Stream> OpenReadAsync(string filePath, CancellationToken ct = default);
}
