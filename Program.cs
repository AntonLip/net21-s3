using System.Net;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using S3FileApi.Models;
using S3FileApi.Options;
using S3FileApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOptions<S3Options>()
    .Bind(builder.Configuration.GetSection(S3Options.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IAmazonS3>(serviceProvider =>
{
    var options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<S3Options>>()
        .Value;

    var config = new AmazonS3Config
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region),
        ForcePathStyle = options.ForcePathStyle
    };

    if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
    {
        config.ServiceURL = options.ServiceUrl;
    }

    return new AmazonS3Client(config);
});
builder.Services.AddScoped<S3FileService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var files = app.MapGroup("/api/files");

files.MapGet("/", async (
    string? prefix,
    string? continuationToken,
    S3FileService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.ListAsync(prefix, continuationToken, cancellationToken);
    return Results.Ok(result);
});

files.MapPost("/", async (
    IFormFile file,
    S3FileService service,
    CancellationToken cancellationToken) =>
{
    if (file.Length == 0)
    {
        return Results.BadRequest(new { error = "Файл не должен быть пустым." });
    }

    try
    {
        var result = await service.UploadAsync(file, cancellationToken);
        var location = $"/api/files/{Uri.EscapeDataString(result.Key)}";
        return Results.Created(location, result);
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
}).Accepts<IFormFile>("multipart/form-data").DisableAntiforgery();

files.MapGet("/{**key}", async (
    string key,
    S3FileService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var file = await service.DownloadAsync(key, cancellationToken);
        return Results.File(
            file.Content,
            file.ContentType,
            Path.GetFileName(key),
            enableRangeProcessing: true);
    }
    catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
    {
        return Results.NotFound(new { error = $"Файл '{key}' не найден." });
    }
});

files.MapDelete("/{**key}", async (
    string key,
    S3FileService service,
    CancellationToken cancellationToken) =>
{
    await service.DeleteAsync(key, cancellationToken);
    return Results.NoContent();
});

app.Run();
