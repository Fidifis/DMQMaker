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

var handler = (APIGatewayProxyRequest request, ILambdaContext context) =>
{
    string body = request.IsBase64Encoded
        ? Encoding.UTF8.GetString(Convert.FromBase64String(request.Body))
        : request.Body;
    var input = JsonSerializer.Deserialize<Event>(body);
    Log.Information("Incoming Text: " + input.Text);

    DMQMaker maker = new();

    using MemoryStream finalBytes = new();
    var imageBytes = Convert.FromBase64String(input.ImageBase64);
    var image = Image.Load(imageBytes);
    var finalImage = maker.MakeImage(image, input.Text, new DMQParams());

    finalImage.Save(finalBytes, PngFormat.Instance);
    var result = Convert.ToBase64String(finalBytes.ToArray());
    Log.Debug("Serializing and sending response");
    return new APIGatewayProxyResponse
    {
        StatusCode = 200,
        Headers = new Dictionary<string, string> {{"Content-Type", "application/json"}},
        Body = JsonSerializer.Serialize(new
        {
            resultBase64 = result,
        }),
    };
};

await LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer())
        .Build()
        .RunAsync();
