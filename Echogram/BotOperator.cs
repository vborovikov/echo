namespace Echo;

using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Telegram;

/// <summary>
/// Operates the bot chats.
/// </summary>
public interface IBotOperator : IBot
{
    /// <summary>
    /// Stops the bot conversation with the user.
    /// </summary>
    /// <param name="chat">The chat to stop.</param>
    /// <param name="user">The user who decided to stop the chat, or <c>null</c> if the chat was forced to stop.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the chat is stopped.</returns>
    Task StopAsync(IBotChat chat, User? user, CancellationToken cancellationToken);
}

/// <summary>
/// Operates the typed bot chats.
/// </summary>
public interface IBotOperator<TBotChat> : IBotOperator
    where TBotChat : IBotChat<TBotChat>
{
    /// <summary>
    /// Starts the bot conversation with the user.
    /// </summary>
    /// <param name="chatId">The chat ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the chat is started.</returns>
    Task<TBotChat> StartAsync(ChatId chatId, CancellationToken cancellationToken);
}

/// <summary>
/// Operates the bot chats.
/// </summary>
public abstract class BotOperator<TBotChat> : IBotOperator<TBotChat>
    where TBotChat : IBotChat<TBotChat>
{
    private readonly IBot bot;
    private readonly ILogger log;
    private readonly ConcurrentDictionary<ChatId, ChatSession> chats;
    private bool isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BotOperator{TBotChat}"/> class.
    /// </summary>
    /// <param name="bot">The bot to operate.</param>
    /// <param name="logger">The logger.</param>
    protected BotOperator(IBot bot, ILogger logger)
    {
        this.bot = bot;
        this.log = logger;
        this.chats = new();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs the cleanup operations for the <see cref="BotOperator{TBotChat}"/> object.
    /// </summary>
    /// <param name="disposing">
    /// Indicates whether the method is called from <see cref="Dispose()"/> (its value is <c>true</c>)
    /// or from a finalizer (its value is <c>false</c>).
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (this.isDisposed) return;

        if (disposing)
        {
            foreach (var (_, session) in this.chats)
            {
                session.Dispose();
            }
            this.chats.Clear();
            this.bot.Dispose();
        }

        this.isDisposed = true;
    }

    Task<TResult> IBot.ExecuteAsync<TResult>(ApiRequest<TResult> request, CancellationToken cancellationToken) =>
        this.bot.ExecuteAsync(request, cancellationToken);

    async Task<TBotChat> IBotOperator<TBotChat>.StartAsync(ChatId chatId, CancellationToken cancellationToken)
    {
        var session = GetChatSession(chatId, out var maybeNew);
        if (maybeNew)
        {
            //todo: do we need here to care about the session lifetime?
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.Lifetime);
            await session.Chat.BeginAsync(default, cts.Token).ConfigureAwait(false);
        }

        return session.Chat;
    }

    async Task IBotOperator.StopAsync(IBotChat chat, User? user, CancellationToken cancellationToken)
    {
        if (this.chats.TryRemove(chat.ChatId, out var removedSession))
        {
            await removedSession.StopAsync(user, cancellationToken).ConfigureAwait(false);
            await removedSession.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Runs the bot operation.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous bot operation.</returns>
    public async Task ChatAsync(CancellationToken cancellationToken)
    {
        try
        {
            await TBotChat.StartAsync(this, cancellationToken).ConfigureAwait(false);
            this.log.LogInformation(EventIds.BotStarted, "Bot operation started");

            var messageChannel = Channel.CreateUnbounded<Message>(new()
            {
                SingleWriter = true,
                SingleReader = true,
            });
            var callbackChannel = Channel.CreateUnbounded<CallbackQuery>(new()
            {
                SingleWriter = true,
                SingleReader = true,
            });

            await Task.WhenAll(
                ReceiveUpdatesAsync(messageChannel, callbackChannel, cancellationToken),
                HandleMessagesAsync(messageChannel, cancellationToken),
                HandleCallbacksAsync(callbackChannel, cancellationToken)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            foreach (var (_, session) in this.chats)
            {
                await session.StopAsync().ConfigureAwait(false);
                await session.DisposeAsync().ConfigureAwait(false);
            }
            this.chats.Clear();

            throw;
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.BotFailed, x, "Bot operation failed");
        }
        finally
        {
            await TBotChat.StopAsync(this, default).ConfigureAwait(false);
            this.log.LogInformation(EventIds.BotStopped, "Bot operation stopped");
        }
    }

    private async Task ReceiveUpdatesAsync(ChannelWriter<Message> messageWriter, ChannelWriter<CallbackQuery> callbackWriter, CancellationToken cancellationToken)
    {
        try
        {
            await Parallel.ForEachAsync(this.bot.GetAllUpdatesAsync(), cancellationToken,
                async (update, cancellationToken) =>
                {
                    var message = update.Message ?? update.EditedMessage ?? update.ChannelPost ?? update.EditedChannelPost;
                    if (message is not null)
                    {
                        await messageWriter.WriteAsync(message, cancellationToken).ConfigureAwait(false);
                    }
                    else if (update.CallbackQuery is CallbackQuery callback)
                    {
                        await callbackWriter.WriteAsync(callback, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        this.log.LogWarning(EventIds.BotConfused, "Received unknown update: {Update}", update);
                    }
                }).ConfigureAwait(false);
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            messageWriter.Complete(x);
            callbackWriter.Complete(x);

            this.log.LogError(EventIds.BotFailed, x, "Failed to receive updates");
            throw;
        }
        finally
        {
            messageWriter.TryComplete();
            callbackWriter.TryComplete();
        }
    }

    private async Task HandleMessagesAsync(ChannelReader<Message> messageReader, CancellationToken cancellationToken)
    {
        try
        {
            await Parallel.ForEachAsync(messageReader.ReadAllAsync(), cancellationToken,
                async (message, cancellationToken) =>
                {
                    var session = GetChatSession(message.Chat.Id, out var maybeNew);
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.Lifetime);
                    try
                    {
                        if (maybeNew)
                        {
                            await session.Chat.BeginAsync(message.From, cts.Token).ConfigureAwait(false);
                        }
                        await session.Chat.HandleAsync(message, cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException x) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    {
                        this.log.LogWarning(EventIds.BotConfused, x, "Handling message {MessageId} from {ChatId} was taking too much time",
                            message.MessageId, message.Chat.Id);
                    }
                    catch (Exception x) when (x is not OperationCanceledException)
                    {
                        this.log.LogError(EventIds.BotConfused, x, "Failed to handle message {MessageId} from {ChatId}", message.MessageId, message.Chat.Id);
                        await session.Chat.HandleAsync(x, cts.Token).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.BotFailed, x, "Failed to handle messages");
            throw;
        }
    }

    private async Task HandleCallbacksAsync(ChannelReader<CallbackQuery> callbackReader, CancellationToken cancellationToken)
    {
        try
        {
            await Parallel.ForEachAsync(callbackReader.ReadAllAsync(), cancellationToken,
                async (callback, cancellationToken) =>
                {
                    var session = GetChatSession((ChatId)callback.From.Id, out _);
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.Lifetime);
                    try
                    {
                        await session.Chat.HandleAsync(callback, cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException x) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    {
                        this.log.LogWarning(EventIds.BotConfused, x, "Handling callback {CallbackId} from {UserId} was taking too much time",
                            callback.Id, callback.From.Id);
                    }
                    catch (Exception x) when (x is not OperationCanceledException)
                    {
                        this.log.LogError(EventIds.BotConfused, x, "Failed to handle callback {CallbackId} from {UserId}", callback.Id, callback.From.Id);
                        await session.Chat.HandleAsync(x, cts.Token).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.BotFailed, x, "Failed to handle callback queries");
            throw;
        }
    }

    private ChatSession GetChatSession(ChatId chatId, out bool maybeNew)
    {
        if (this.chats.TryGetValue(chatId, out var session))
        {
            maybeNew = false;
            return session;
        }

        maybeNew = true;
        return this.chats.GetOrAdd(chatId, (chatId, botOperator) => new ChatSession(botOperator.CreateChat(chatId)), this);
    }

    /// <summary>
    /// Creates a new bot chat.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat.</param>
    /// <returns>A new bot chat.</returns>
    protected abstract TBotChat CreateChat(ChatId chatId);

    private sealed class ChatSession : IDisposable, IAsyncDisposable
    {
        private readonly CancellationTokenSource lifetime;
        private bool isDisposed;

        public ChatSession(TBotChat chat)
        {
            this.Chat = chat;
            this.lifetime = new CancellationTokenSource();
        }

        public TBotChat Chat { get; }

        public CancellationToken Lifetime => this.lifetime.Token;

        public async Task StopAsync(User? user = default, CancellationToken cancellationToken = default)
        {
            await this.Chat.EndAsync(user, cancellationToken).ConfigureAwait(false);

            // the provided cancellation token can be linked with the lifetime token
            // cancel the lifetime last
            await this.lifetime.CancelAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);

            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (this.isDisposed) return;

            if (disposing)
            {
                if (!this.lifetime.IsCancellationRequested)
                {
                    this.lifetime.Cancel();
                }

                this.Chat.Dispose();
                this.lifetime.Dispose();
            }

            this.isDisposed = true;
        }

        private async ValueTask DisposeAsyncCore()
        {
            if (this.isDisposed) return;

            if (!this.lifetime.IsCancellationRequested)
            {
                await this.lifetime.CancelAsync().ConfigureAwait(false);
            }

            if (this.Chat is IAsyncDisposable asyncDisposableChat)
            {
                await asyncDisposableChat.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                this.Chat.Dispose();
            }
            this.lifetime.Dispose();

            this.isDisposed = true;
        }
    }
}
