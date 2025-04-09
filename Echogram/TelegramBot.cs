#define DUMP_JSON

namespace Echo;

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Telegram;
using Telegram.Serialization;

/// <summary>
/// Provides access to the Telegram Bot API.
/// </summary>
public interface IBot
{
    Task<TResult> ExecuteAsync<TResult>(ApiRequest<TResult> request, CancellationToken cancellationToken);
}

/// <summary>
/// Telegram Bot API client.
/// </summary>
public sealed class TelegramBot : IBot
{
    private static readonly MediaTypeHeaderValue jsonMediaType = new("application/json");
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.General)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringArrayEnumConverter(JsonNamingPolicy.SnakeCaseLower),
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false),
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

        this.log.LogTrace(EventIds.BotExecuting, "Executing Bot API request '{Method}'", request.Method);

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
        while (true)
        {
            cts.Token.ThrowIfCancellationRequested();

            var updates = Array.Empty<Update>();
            try
            {
                this.log.LogDebug(EventIds.BotWaiting, "Waiting for updates");
                updates = await ExecuteAsync(updateRequest, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                throw;
            }

            if (updates.Length > 0)
            {
                this.log.LogTrace(EventIds.BotWaiting, "Received {Count} updates", updates.Length);

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