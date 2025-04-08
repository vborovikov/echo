#define DUMP_JSON

namespace Echo;

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Echo.Telegram;
using Microsoft.Extensions.Logging;
using Telegram;
using Telegram.Serialization;

public sealed class TelegramBot
{
    private static readonly MediaTypeHeaderValue jsonMediaType = new("application/json");
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.General)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false),
            new JsonStringArrayEnumConverter(JsonNamingPolicy.SnakeCaseLower),
        }
    };

    private readonly string token;
    private readonly HttpClient http;
    private readonly ILogger<TelegramBot> log;

    public TelegramBot(string token, HttpClient http, ILogger<TelegramBot> logger)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentException.ThrowIfNullOrEmpty(token);

        this.token = token;
        this.http = http;
        this.log = logger;
    }

    public async Task<TResult> ExecuteAsync<TResult>(ApiRequest<TResult> request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

#if DUMP_JSON
        Debug.WriteLine(JsonSerializer.Serialize(request, request.GetType(), jsonOptions));
#endif

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri($"/bot{this.token}/{request.Method}", UriKind.Relative))
        {
            Content = JsonContent.Create(request, request.GetType(), jsonMediaType, jsonOptions)
        };
        using var httpResponse = await this.http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        httpResponse.EnsureSuccessStatusCode();

#if DUMP_JSON
        var httpContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        Debug.WriteLine(httpContent);
        var response = JsonSerializer.Deserialize<ApiResponse<TResult>>(httpContent, jsonOptions) ??
            throw new TelegramBotException(request.Method, default, $"Failed to execute Bot API request {request.Method}: empty response.");
#else
        await using var httpContent = await httpResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var response = await JsonSerializer.DeserializeAsync<ApiResponse<TResult>>(httpContent, jsonOptions, cancellationToken).ConfigureAwait(false) ??
            throw new TelegramBotException(request.Method, default, $"Failed to execute Bot API request {request.Method}: empty response.");
#endif
        if (!response.Ok || response.Result is null)
        {
            throw new TelegramBotException(request.Method, response,
                $"Failed to execute Bot API request {request.Method}: {response.Description ?? "empty result"}.");
        }

        return response.Result;
    }

    public async IAsyncEnumerable<Update> GetAllUpdatesAsync(CancellationToken cancellationToken, [EnumeratorCancellation] CancellationToken enumeratorCancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, enumeratorCancellationToken);

        var updateRequest = new ApiGetUpdates { Offset = 0 };
        while (!cts.Token.IsCancellationRequested)
        {
            var updates = await ExecuteAsync(updateRequest, cts.Token).ConfigureAwait(false);

            if (updates.Length > 0)
            {
                var offset = 0;
                foreach (var update in updates)
                {
                    yield return update;
                    offset = Math.Max(offset, update.UpdateId);
                }

                updateRequest = updateRequest with { Offset = offset + 1 };
            }
        }
    }
}