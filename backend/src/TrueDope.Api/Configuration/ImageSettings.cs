namespace TrueDope.Api.Configuration;

public class ImageSettings
{
    public const string SectionName = "ImageProcessing";

    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024; // 20MB
    public int MaxImagesPerEntity { get; set; } = 10;
    public int ThumbnailSize { get; set; } = 300;
    public int FullImageMaxDimension { get; set; } = 4096;
    public int JpegQuality { get; set; } = 85;
    public int ThumbnailQuality { get; set; } = 80;
    public int PreSignedUrlExpirySeconds { get; set; } = 3600; // 1 hour
}
