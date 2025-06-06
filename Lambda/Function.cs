using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using DMQCore;
using Lambda;
using Amazon.S3;
using Amazon.S3.Model;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

var logConfig = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug();
Log.Logger = logConfig.CreateLogger();

var s3Client = new AmazonS3Client();

var downloadFont = async (string s3Bucket, string s3Key, string localFilePath) =>
{
    var request = new GetObjectRequest
    {
        BucketName = s3Bucket,
        Key = s3Key,
    };

    using var response = await s3Client.GetObjectAsync(request);
    using var responseStream = response.ResponseStream;
    using var fileStream = File.Create(localFilePath);
    await responseStream.CopyToAsync(fileStream);
};

var handler = async (Event input, ILambdaContext context) =>
{
    Log.Information("Incoming Text: " + input.text);

    DMQParams paramz = new();
    DMQMaker maker = new();
    FontParam? dmqFont = null;

    if (input.fontS3Key != null) {
      Log.Debug($"Downloading font file {input.fontS3Key}");
      if (input.fontS3Bucket == null) {
        var err = $"fontS3Bucket not specified. Both fontS3Bucket and fontS3Key must be specified.";
        Log.Error(err);
        throw new ArgumentException(err);
      }
      var tempFontPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
      await downloadFont(input.fontS3Bucket, input.fontS3Key, tempFontPath);
      dmqFont = new() {
        UseBuildIn = false,
        FontFilePath = tempFontPath,
      };
    }

    Log.Debug("Downloading image");

    var request = new GetObjectRequest
    {
        BucketName = input.s3Bucket,
        Key = input.s3Key,
    };

    using var response = await s3Client.GetObjectAsync(request);
    using Stream responseStream = response.ResponseStream;

    using var image = Image.Load(response.ResponseStream);

    var res = input.resolution ?? [paramz.ResolutionX, paramz.ResolutionY];

    if (res.Length != 2)
    {
        var err = $"Too many dimensions in resolutions, expected 2, received {res.Length}";
        Log.Error(err);
        throw new ArgumentException(err);
    }
    Log.Information($"Making resolution: {res[0]}x{res[1]}");

    var paramsWithRes = paramz;
    paramsWithRes.ResolutionX = res[0];
    paramsWithRes.ResolutionY = res[1];

    var finalImage = maker.MakeImage(image, input.text, paramsWithRes, dmqFont);

    using var outputStream = new MemoryStream();
    finalImage.Save(outputStream, PngFormat.Instance);
    
    Log.Debug($"finished {res[0]}x{res[1]}");

    Log.Debug("Saving to S3");
    outputStream.Position = 0;

    var putRequest = new PutObjectRequest
    {
        BucketName = input.s3Bucket,
        Key = input.resultS3Key,
        InputStream = outputStream,
    };

    await s3Client.PutObjectAsync(putRequest);
};

await LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer())
        .Build()
        .RunAsync();
