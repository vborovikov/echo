namespace Echo;

using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Telegram;

public interface IBotOperator : IBot
{
    Task StopAsync(IBotDialog dialog, CancellationToken cancellationToken);
}

public class BotOperator<TBotDialog> : IBotOperator
    where TBotDialog : IBotDialog<TBotDialog>
{
    private readonly IBotDialogFactory<TBotDialog> factory;
    private readonly TelegramBot bot;
    private readonly ILogger<BotOperator<TBotDialog>> log;
    private readonly ConcurrentDictionary<ChatId, TBotDialog> dialogs;

    public BotOperator(IBotDialogFactory<TBotDialog> factory, TelegramBot bot, ILogger<BotOperator<TBotDialog>> logger)
    {
        this.factory = factory;
        this.bot = bot;
        this.log = logger;
        this.dialogs = new();
    }

    Task<TResult> IBot.ExecuteAsync<TResult>(ApiRequest<TResult> request, CancellationToken cancellationToken) =>
        this.bot.ExecuteAsync(request, cancellationToken);

    async Task IBotOperator.StopAsync(IBotDialog dialog, CancellationToken cancellationToken)
    {
        if (this.dialogs.TryRemove(dialog.ChatId, out var removedDialog))
        {
            await removedDialog.EndAsync(cancellationToken);
        }
    }

    public async Task ChatAsync(CancellationToken cancellationToken)
    {
        try
        {
            await TBotDialog.StartAsync(this, cancellationToken).ConfigureAwait(false);
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
            foreach (var (_, dialog) in this.dialogs)
            {
                await dialog.EndAsync(default).ConfigureAwait(false);
            }
            this.dialogs.Clear();

            throw;
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.BotFailed, x, "Bot operation failed");
        }
        finally
        {
            await TBotDialog.StopAsync(this, default).ConfigureAwait(false);
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
                    var dialog = GetDialog(message.Chat.Id, out var maybeNew);
                    try
                    {
                        if (maybeNew)
                        {
                            await dialog.BeginAsync(cancellationToken).ConfigureAwait(false);
                        }
                        await dialog.HandleAsync(message, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception x) when (x is not OperationCanceledException)
                    {
                        this.log.LogWarning(EventIds.BotConfused, x, "Failed to handle message {MessageId} from {ChatId}", message.MessageId, message.Chat.Id);
                        await dialog.HandleAsync(x, cancellationToken).ConfigureAwait(false);
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
                    var dialog = GetDialog((ChatId)callback.From.Id, out _);
                    try
                    {
                        await dialog.HandleAsync(callback, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception x) when (x is not OperationCanceledException)
                    {
                        this.log.LogWarning(EventIds.BotConfused, x, "Failed to handle callback {CallbackId} from {UserId}", callback.Id, callback.From.Id);
                        await dialog.HandleAsync(x, cancellationToken).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.BotFailed, x, "Failed to handle callback queries");
            throw;
        }
    }

    private TBotDialog GetDialog(ChatId chatId, out bool maybeNew)
    {
        if (this.dialogs.TryGetValue(chatId, out var dialog))
        {
            maybeNew = false;
            return dialog;
        }

        maybeNew = true;
        return this.dialogs.GetOrAdd(chatId, (chatId, botOperator) => botOperator.factory.Create(botOperator, chatId), this);
    }
}