using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using DMQCore;
using Lambda;
using System.Text.Json;

var handler = (Event input, ILambdaContext context) =>
{
    Console.WriteLine("Text: " + input.Text);
    Console.WriteLine("base64 length: " + input.ImageBase64.Length);

    DMQMaker maker = new();

    using MemoryStream finalBytes = new();
    var imageBytes = Convert.FromBase64String(input.ImageBase64);
    maker.LoadImage(imageBytes);
    maker.Text = input.Text;
    maker.MakeImage();

    maker.FinalImage!.CopyToStream(finalBytes);
    var result = Convert.ToBase64String(finalBytes.ToArray());
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
