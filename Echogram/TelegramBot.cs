namespace Echo;

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
public interface IBot : IDisposable
{
    /// <summary>
    /// Executes a request to the Telegram Bot API.
    /// </summary>
    /// <param name="request">The request to execute.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the request is executed.</returns>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="TelegramBot"/> class.
    /// </summary>
    /// <param name="token">The bot token.</param>
    /// <param name="http">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public TelegramBot(string token, HttpClient http, ILogger<TelegramBot> logger)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentException.ThrowIfNullOrEmpty(token);

        this.token = token;
        this.http = http;
        this.log = logger;
    }

    internal ILogger Log => this.log;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.http.Dispose();
    }

    /// <summary>
    /// Executes a request to the Telegram Bot API.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="request">The request to execute.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the request is executed.</returns>
    /// <exception cref="TelegramBotException">Throws if the request fails.</exception>
    public async Task<TResult> ExecuteAsync<TResult>(ApiRequest<TResult> request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        this.log.LogTrace(EventIds.BotExecuting, "Executing Bot API request '{Method}'", request.Method);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri($"/bot{this.token}/{request.Method}", UriKind.Relative))
        {
            Content = JsonContent.Create(request, request.GetType(), jsonMediaType, jsonOptions)
        };
        using var httpResponse = await this.http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        httpResponse.EnsureSuccessStatusCode();

        await using var httpContent = await httpResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var response = await JsonSerializer.DeserializeAsync<ApiResponse<TResult>>(httpContent, jsonOptions, cancellationToken).ConfigureAwait(false) ??
            throw new TelegramBotException(request.Method, default, $"Failed to execute Bot API request {request.Method}: empty response.");

        if (!response.Ok || response.Result is null)
        {
            throw new TelegramBotException(request.Method, response,
                $"Failed to execute Bot API request {request.Method}: {response.Description ?? "empty result"}.");
        }

        return response.Result;
    }
}

static class TelegramBotExtensions
{
    public static async IAsyncEnumerable<Update> GetAllUpdatesAsync(this IBot bot, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var updateRequest = new Api.GetUpdates { Offset = 0 };
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var updates = Array.Empty<Update>();
            try
            {
                updates = await bot.ExecuteAsync(updateRequest, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception x) when (x is not OperationCanceledException)
            {
                (bot as TelegramBot)?.Log.LogWarning(EventIds.BotWaiting, x, "Failed executing Bot API request '{Method}'", updateRequest.Method);
                if (updateRequest.Timeout is int timeoutInSeconds)
                {
                    // slow down the bot imitating no updates
                    var delay = TimeSpan.FromSeconds(timeoutInSeconds);
                    (bot as TelegramBot)?.Log.LogWarning(EventIds.BotWaiting, "Waiting for {Delay} before making new Bot API requests", delay);
                    await Task.Delay(delay, cancellationToken);
                }
            }

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