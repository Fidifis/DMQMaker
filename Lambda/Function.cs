using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using DMQCore;
using Lambda;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System.Text;
using System.Text.Json;

var logConfig = new LoggerConfiguration().WriteTo.Console();
Log.Logger = logConfig.CreateLogger();

var apiError = (string errMsg) => new APIGatewayProxyResponse
{
    StatusCode = 400,
    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
    Body = JsonSerializer.Serialize(new
    {
        error = errMsg
    }),
};

var handler = (APIGatewayProxyRequest request, ILambdaContext context) =>
{
    Event input;
    try
    {
        string body = request.IsBase64Encoded
            ? Encoding.UTF8.GetString(Convert.FromBase64String(request.Body))
            : request.Body;
        input = JsonSerializer.Deserialize<Event>(body);
    }
    catch (Exception e)
    {
        return apiError("Invalid request");
    }

    Log.Information("Incoming Text: " + input.Text);
    Log.Information("Requested resolutions: " + input.Resolutions?.ToString());

    DMQParams paramz = new();
    DMQMaker maker = new();

    Dictionary<string, string> results = new();

    Log.Debug("Decoding image");
    using MemoryStream finalBytes = new();
    var imageBytes = Convert.FromBase64String(input.ImageBase64);
    var image = Image.Load(imageBytes);

    foreach (var res in input.Resolutions ?? [[paramz.ResolutionX, paramz.ResolutionY]])
    {
        Log.Information("Making resolution: " + res.ToString());
        if (res.Length != 2)
        {
            var err = $"Too many dimensions in resolutions, expected 2, received {res.Length}";
            Log.Error(err);
            return apiError(err);
        }

        var paramsWithRes = paramz;
        paramsWithRes.ResolutionX = res[0];
        paramsWithRes.ResolutionY = res[1];

        var finalImage = maker.MakeImage(image, input.Text, paramsWithRes);

        finalImage.Save(finalBytes, PngFormat.Instance);
        var result = Convert.ToBase64String(finalBytes.ToArray());
        results.Add($"image{res[0]}x{res[1]}Base64", result);
        Log.Debug("finished " + res.ToString());
    }

    Log.Debug("Serializing and sending response");
    return new APIGatewayProxyResponse
    {
        StatusCode = 200,
        Headers = new Dictionary<string, string> {{"Content-Type", "application/json"}},
        Body = JsonSerializer.Serialize(results),
    };
};

await LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer())
        .Build()
        .RunAsync();
