using eiupdds_backend.Models;
using eiupdds_backend.Services.Interfaces;
using SkiaSharp;

namespace eiupdds_backend.Services.Implementations;

public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileUploadService> _logger;

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly SKColor HealthyColor = new(34, 197, 94);
    private static readonly SKColor[] BoxPalette =
    [
        new(239, 68, 68),
        new(249, 115, 22),
        new(234, 179, 8),
        new(14, 165, 233),
        new(139, 92, 246),
        new(236, 72, 153)
    ];
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/pjpeg", "image/x-png"
    };

    public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("No file was uploaded or file is empty.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException("File size exceeds the maximum allowed limit of 5MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"Unsupported file format '{extension}'. Allowed formats: .jpg, .jpeg, .png, .webp.");
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            throw new ArgumentException($"Invalid MIME type '{file.ContentType}'. Only image uploads are allowed.");
        }

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "uploads");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _logger.LogInformation("File successfully uploaded: {FileName} ({Size} bytes)", uniqueFileName, file.Length);

        return $"/uploads/{uniqueFileName}";
    }

    public bool DeleteImage(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        try
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var normalizedPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(webRoot, normalizedPath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted file: {Path}", fullPath);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file at relative path: {Path}", relativePath);
        }

        return false;
    }

    public static string GetAnnotatedRelativeUrl(string relativeUploadUrl)
    {
        var stem = Path.GetFileNameWithoutExtension(relativeUploadUrl);
        var extension = Path.GetExtension(relativeUploadUrl);
        return $"/outputs/{stem}-result{extension}";
    }

    public string AnnotateImage(string relativeUploadUrl, IReadOnlyList<PredictionItem> predictions)
    {
        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var sourcePhysicalPath = Path.Combine(
            webRoot,
            relativeUploadUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(sourcePhysicalPath))
        {
            throw new FileNotFoundException($"Uploaded image does not exist at '{sourcePhysicalPath}'.", sourcePhysicalPath);
        }

        var outputRelativeUrl = GetAnnotatedRelativeUrl(relativeUploadUrl);
        var outputsFolder = Path.Combine(webRoot, "outputs");
        Directory.CreateDirectory(outputsFolder);

        var outputPhysicalPath = Path.Combine(
            webRoot,
            outputRelativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        using var bitmap = SKBitmap.Decode(sourcePhysicalPath)
            ?? throw new InvalidOperationException($"Unable to decode image at '{sourcePhysicalPath}'.");

        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
        var canvas = surface.Canvas;
        using var sourceImage = SKImage.FromBitmap(bitmap);
        canvas.DrawImage(sourceImage, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));

        if (predictions.Count > 0)
        {
            DrawPredictions(canvas, bitmap.Width, bitmap.Height, predictions);
        }

        var format = Path.GetExtension(relativeUploadUrl).ToLowerInvariant() switch
        {
            ".png" => SKEncodedImageFormat.Png,
            ".webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Jpeg
        };

        using var image = surface.Snapshot();
        using var encoded = image.Encode(format, 92);
        using var outputStream = File.Open(outputPhysicalPath, FileMode.Create, FileAccess.Write);
        encoded.SaveTo(outputStream);

        return outputRelativeUrl;
    }

    private static void DrawPredictions(
        SKCanvas canvas,
        int imageWidth,
        int imageHeight,
        IReadOnlyList<PredictionItem> predictions)
    {
        var scale = Math.Max(1f, Math.Min(imageWidth, imageHeight) / 400f);
        var strokeWidth = Math.Clamp(2.5f * scale, 2f, 8f);
        var fontSize = Math.Clamp(14f * scale, 12f, 36f);
        var paddingX = 6f * scale;
        var paddingY = 4f * scale;
        var cornerRadius = 4f * scale;

        using var typeface = SKTypeface.FromFamilyName(
            null,
            SKFontStyleWeight.Bold,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright);
        using var font = new SKFont(typeface, fontSize);
        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true
        };

        foreach (var item in predictions)
        {
            var left = Math.Clamp((float)(item.X - item.Width / 2.0), 0f, imageWidth - 1f);
            var top = Math.Clamp((float)(item.Y - item.Height / 2.0), 0f, imageHeight - 1f);
            var right = Math.Clamp((float)(item.X + item.Width / 2.0), left + 1f, imageWidth);
            var bottom = Math.Clamp((float)(item.Y + item.Height / 2.0), top + 1f, imageHeight);
            var boxRect = new SKRect(left, top, right, bottom);

            var color = item.DetectedClass.Contains("healthy", StringComparison.OrdinalIgnoreCase)
                ? HealthyColor
                : BoxPalette[Math.Abs(item.ClassId) % BoxPalette.Length];

            using var fillPaint = new SKPaint
            {
                Color = color.WithAlpha(28),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRoundRect(boxRect, cornerRadius, cornerRadius, fillPaint);

            using var borderPaint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = strokeWidth,
                IsAntialias = true
            };
            canvas.DrawRoundRect(boxRect, cornerRadius, cornerRadius, borderPaint);

            var label = $"{item.DetectedClass} {item.Confidence * 100:0.#}%";
            font.MeasureText(label, out var textBounds);

            var pillWidth = textBounds.Width + (paddingX * 2f);
            var pillHeight = textBounds.Height + (paddingY * 2f);
            var pillLeft = Math.Clamp(left, 0f, Math.Max(0f, imageWidth - pillWidth));
            var pillTop = top >= pillHeight ? top - pillHeight : top;
            var pillRect = new SKRect(pillLeft, pillTop, pillLeft + pillWidth, pillTop + pillHeight);

            using var pillPaint = new SKPaint
            {
                Color = color.WithAlpha(235),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRoundRect(pillRect, cornerRadius, cornerRadius, pillPaint);

            canvas.DrawText(
                label,
                pillLeft + paddingX - textBounds.Left,
                pillTop + paddingY - textBounds.Top,
                SKTextAlign.Left,
                font,
                textPaint);
        }
    }
}
