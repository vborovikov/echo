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
    private readonly ConcurrentDictionary<ChatId, TBotChat> chats;

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
        foreach (var (_, chat) in this.chats)
        {
            chat.Dispose();
        }
        this.chats.Clear();
        this.bot.Dispose();
    }

    Task<TResult> IBot.ExecuteAsync<TResult>(ApiRequest<TResult> request, CancellationToken cancellationToken) =>
        this.bot.ExecuteAsync(request, cancellationToken);

    async Task<TBotChat> IBotOperator<TBotChat>.StartAsync(ChatId chatId, CancellationToken cancellationToken)
    {
        var chat = GetChat(chatId, out var maybeNew);
        if (maybeNew)
        {
            await chat.BeginAsync(default, cancellationToken).ConfigureAwait(false);
        }

        return chat;
    }

    async Task IBotOperator.StopAsync(IBotChat chat, User? user, CancellationToken cancellationToken)
    {
        if (this.chats.TryRemove(chat.ChatId, out var removedChat))
        {
            await removedChat.EndAsync(user, cancellationToken).ConfigureAwait(false);
            removedChat.Dispose();
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
            foreach (var (_, chat) in this.chats)
            {
                await chat.EndAsync(default, default).ConfigureAwait(false);
            }

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
            await Parallel.ForEachAsync(this.bot.GetAllUpdatesAsync(cancellationToken), cancellationToken,
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
            await Parallel.ForEachAsync(messageReader.ReadAllAsync(cancellationToken), cancellationToken,
                async (message, cancellationToken) =>
                {
                    var chat = GetChat(message.Chat.Id, out var maybeNew);
                    try
                    {
                        if (maybeNew)
                        {
                            await chat.BeginAsync(message.From, cancellationToken).ConfigureAwait(false);
                        }
                        await chat.HandleAsync(message, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception x) when (x is not OperationCanceledException)
                    {
                        this.log.LogWarning(EventIds.BotConfused, x, "Failed to handle message {MessageId} from {ChatId}", message.MessageId, message.Chat.Id);
                        await chat.HandleAsync(x, cancellationToken).ConfigureAwait(false);
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
            await Parallel.ForEachAsync(callbackReader.ReadAllAsync(cancellationToken), cancellationToken,
                async (callback, cancellationToken) =>
                {
                    var chat = GetChat((ChatId)callback.From.Id, out _);
                    try
                    {
                        await chat.HandleAsync(callback, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception x) when (x is not OperationCanceledException)
                    {
                        this.log.LogWarning(EventIds.BotConfused, x, "Failed to handle callback {CallbackId} from {UserId}", callback.Id, callback.From.Id);
                        await chat.HandleAsync(x, cancellationToken).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.BotFailed, x, "Failed to handle callback queries");
            throw;
        }
    }

    private TBotChat GetChat(ChatId chatId, out bool maybeNew)
    {
        if (this.chats.TryGetValue(chatId, out var chat))
        {
            maybeNew = false;
            return chat;
        }

        maybeNew = true;
        return this.chats.GetOrAdd(chatId, (chatId, botOperator) => botOperator.CreateChat(chatId), this);
    }

    /// <summary>
    /// Creates a new bot chat.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat.</param>
    /// <returns>A new bot chat.</returns>
    protected abstract TBotChat CreateChat(ChatId chatId);
}

/// <summary>
/// Operates the bot chats.
/// </summary>
public class BotForumOperator<TBotChat> : BotOperator<TBotChat>
    where TBotChat : IBotChat<TBotChat>
{
    private readonly IBotForum<TBotChat> forum;

    /// <summary>
    /// Initializes a new instance of the <see cref="BotForumOperator{TBotChat}"/> class.
    /// </summary>
    /// <param name="bot">The bot to operate.</param>
    /// <param name="forum">The forum (a factory object) to create chats.</param>
    /// <param name="logger">The logger.</param>
    public BotForumOperator(IBot bot, IBotForum<TBotChat> forum, ILogger logger) : base(bot, logger)
    {
        this.forum = forum;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BotForumOperator{TBotChat}"/> class.
    /// </summary>
    /// <param name="bot">The bot to operate.</param>
    /// <param name="forum">The forum (a factory object) to create chats.</param>
    /// <param name="logger">The logger.</param>
    public BotForumOperator(IBot bot, IBotForum<TBotChat> forum, ILogger<BotForumOperator<TBotChat>> logger)
        : this(bot, forum, (ILogger)logger) { }

    /// <inheritdoc/>
    protected override TBotChat CreateChat(ChatId chatId) => this.forum.Create(this, chatId);
}
