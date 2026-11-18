namespace S3FileApi.Models;

public sealed record FileListResponse(
    IReadOnlyCollection<S3FileResponse> Files,
    string? NextContinuationToken);

public sealed record S3FileResponse(
    string Key,
    long? Size,
    DateTime? LastModified,
    string? ETag);

public sealed record UploadedFileResponse(string Key, string? ETag);

public sealed record DownloadedFile(Stream Content, string ContentType);
