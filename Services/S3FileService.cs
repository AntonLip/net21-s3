using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using S3FileApi.Models;
using S3FileApi.Options;

namespace S3FileApi.Services;

public sealed class S3FileService(IAmazonS3 s3Client, IOptions<S3Options> options)
{
    private readonly string _bucketName = options.Value.BucketName;

    public async Task<FileListResponse> ListAsync(
        string? prefix,
        string? continuationToken,
        CancellationToken cancellationToken)
    {
        var response = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix,
            ContinuationToken = continuationToken
        }, cancellationToken);

        var files = response.S3Objects
            .Select(file => new S3FileResponse(file.Key, file.Size, file.LastModified, file.ETag))
            .ToArray();

        return new FileListResponse(files, response.NextContinuationToken);
    }

    public async Task<UploadedFileResponse> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var key = Path.GetFileName(file.FileName.Trim());
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Укажите имя файла.");
        }

        await using var stream = file.OpenReadStream();
        var response = await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType
        }, cancellationToken);

        return new UploadedFileResponse(key, response.ETag);
    }

    public async Task<DownloadedFile> DownloadAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var response = await s3Client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        }, cancellationToken);

        return new DownloadedFile(
            response.ResponseStream,
            string.IsNullOrWhiteSpace(response.Headers.ContentType)
                ? "application/octet-stream"
                : response.Headers.ContentType);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        await s3Client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        }, cancellationToken);
    }
}
