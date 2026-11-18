using System.ComponentModel.DataAnnotations;

namespace S3FileApi.Options;

public sealed class S3Options
{
    public const string SectionName = "S3";

    [Required]
    public string BucketName { get; init; } = string.Empty;

    [Required]
    public string Region { get; init; } = string.Empty;

    public string? ServiceUrl { get; init; }

    public bool ForcePathStyle { get; init; }
}
