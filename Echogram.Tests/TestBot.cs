namespace Echogram.Tests;

using Echo;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Text;

/// <summary>
/// Helpers for creating bots with stubbed HTTP handlers and inspecting their requests.
/// </summary>
public static class TestBot
{
    public const string Token = "123456789:AAHtesttoken";

    public static (TelegramBot Bot, StubHttpMessageHandler Handler) CreateBot(string result)
    {
        var responseBody = $$"""{"ok":true,"result":{{result}}}""";

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        });

        return (new TelegramBot(Token, CreateHttpClient(handler), NullLogger<TelegramBot>.Instance), handler);
    }

    public static HttpClient CreateHttpClient(HttpMessageHandler handler) => new(handler)
    {
        BaseAddress = new Uri("https://api.telegram.org/")
    };

    public static Dictionary<string, (string Headers, string Body)> ParseMultipart(string body, string boundary)
    {
        var fields = new Dictionary<string, (string Headers, string Body)>();
        foreach (var part in body.Split($"--{boundary}", StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.Trim('\r', '\n') is "" or "--") continue;

            var separatorIndex = part.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            var headers = part[..separatorIndex];
            var content = part[(separatorIndex + 4)..];
            if (content.EndsWith("\r\n", StringComparison.Ordinal))
            {
                content = content[..^2];
            }

            var nameLine = headers.Split('\n').First(l => l.Contains("name=", StringComparison.Ordinal));
            var name = nameLine.Split("name=")[1].Split(';')[0].Trim().Trim('"');
            fields[name] = (headers, content);
        }

        return fields;
    }

    public sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => this.responder = responder;

        public HttpRequestMessage? LastRequest { get; private set; }
        public byte[]? LastRequestBody { get; private set; }
        public string? LastRequestContentType { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.LastRequest = request;
            this.LastRequestContentType = request.Content?.Headers.ContentType?.ToString();
            if (request.Content is not null)
            {
                this.LastRequestBody = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            }

            return this.responder(request);
        }
    }
}
