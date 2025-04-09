namespace Echo;

using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Telegram;

public interface IChat
{
    TelegramBot Bot { get; }
}

public class BotManager : IChat
{
    private readonly BotUser id;
    private readonly TelegramBot bot;
    private readonly IBotDialogFactory factory;
    private readonly ILogger<BotManager> log;
    private readonly ConcurrentDictionary<ChatId, IBotDialog> dialogs;

    private BotManager(BotUser id, TelegramBot bot, IBotDialogFactory dialogFactory, ILogger<BotManager> logger)
    {
        this.id = id;
        this.bot = bot;
        this.factory = dialogFactory;
        this.log = logger;
        this.dialogs = new();
    }

    public TelegramBot Bot => this.bot;

    public static async Task<BotManager> CreateAsync(TelegramBot bot, IBotDialogFactory dialogFactory, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var botUser = await bot.ExecuteAsync(ApiGetMe.Default, cancellationToken);
        return new(botUser, bot, dialogFactory, loggerFactory.CreateLogger<BotManager>());
    }

    public async Task ChatAsync(CancellationToken cancellationToken)
    {
        try
        {
            this.log.LogInformation(EventIds.BotStarted, "Bot {BotUsername} started", this.id.Username);

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
                HandleCallbacksAsync(callbackChannel, cancellationToken));
        }
        catch (Exception x) when (x is not OperationCanceledException ocx || ocx.CancellationToken != cancellationToken)
        {
            this.log.LogError(EventIds.BotFailed, x, "Bot {BotUsername} failed", this.id.Username);
        }
        finally
        {
            this.log.LogInformation(EventIds.BotStopped, "Bot {BotUsername} stopped", this.id.Username);
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
                    await messageWriter.WriteAsync(message, cancellationToken);
                }
                else if (update.CallbackQuery is CallbackQuery callback)
                {
                    await callbackWriter.WriteAsync(callback, cancellationToken);
                }
                else
                {
                    this.log.LogWarning(EventIds.BotConfused, "Received unknown update: {Update}", update);
                }
            });
        }
        catch (Exception x) when (x is not OperationCanceledException ocx || ocx.CancellationToken != cancellationToken)
        {
            messageWriter.Complete(x);
            callbackWriter.Complete(x);

            throw;
        }
        finally
        {
            messageWriter.TryComplete();
            callbackWriter.TryComplete();
        }
    }

    private Task HandleMessagesAsync(ChannelReader<Message> messageReader, CancellationToken cancellationToken)
    {
        return Parallel.ForEachAsync(messageReader.ReadAllAsync(cancellationToken), cancellationToken,
            async (message, cancellationToken) =>
            {
                var dialog = GetDialog(message.Chat.Id);
                try
                {
                    await dialog.HandleAsync(message, cancellationToken);
                }
                catch (Exception x) when (x is not OperationCanceledException ocx || ocx.CancellationToken != cancellationToken)
                {
                    this.log.LogWarning(EventIds.BotConfused, x, "Failed to handle message {MessageId} from {ChatId}", message.MessageId, message.Chat.Id);
                    await dialog.HandleAsync(x, cancellationToken);
                }
            });
    }

    private Task HandleCallbacksAsync(ChannelReader<CallbackQuery> callbackReader, CancellationToken cancellationToken)
    {
        return Parallel.ForEachAsync(callbackReader.ReadAllAsync(cancellationToken), cancellationToken,
            async (callback, cancellationToken) =>
            {
                var dialog = GetDialog((ChatId)callback.From.Id);
                try
                {
                    await dialog.HandleAsync(callback, cancellationToken);
                }
                catch (Exception x) when (x is not OperationCanceledException ocx || ocx.CancellationToken != cancellationToken)
                {
                    this.log.LogWarning(EventIds.BotConfused, x, "Failed to handle callback {CallbackId} from {UserId}", callback.Id, callback.From.Id);
                    await dialog.HandleAsync(x, cancellationToken);
                }
            });
    }

    private IBotDialog GetDialog(ChatId chatId)
    {
        return this.dialogs.GetOrAdd(chatId, (chatId, manager) => manager.factory.Create(chatId, manager), this);
    }
}