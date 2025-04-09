namespace Echo;

using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram;

/// <summary>
/// Produces new bot dialogs.
/// </summary>
/// <typeparam name="TBotDialog">The concrete type of the bot dialog.</typeparam>
public interface IBotDialogFactory<TBotDialog> where TBotDialog : IBotDialog<TBotDialog>
{
    public TBotDialog Create(IBotOperator bot, ChatId chatId);
}

/// <summary>
/// Represents a bot dialog with a user in a specific chat.
/// </summary>
public interface IBotDialog
{
    ChatId ChatId { get; }

    Task HandleAsync(Message message, CancellationToken cancellationToken);
    Task HandleAsync(CallbackQuery callback, CancellationToken cancellationToken);
    Task HandleAsync(Exception error, CancellationToken cancellationToken);
}

/// <summary>
/// Defines the bot dialog general initialization and termination.
/// </summary>
/// <typeparam name="TSelf">The type that implements the bot dialog.</typeparam>
public interface IBotDialog<TSelf> : IBotDialog
    where TSelf : IBotDialog<TSelf>
{
    static abstract Task StartAsync(IBot bot, CancellationToken cancellationToken);
    static abstract Task StopAsync(IBot bot, CancellationToken cancellationToken);
}
