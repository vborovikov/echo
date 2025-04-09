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
    /// <summary>
    /// Creates a new bot dialog.
    /// </summary>
    /// <param name="bot">The bot operator.</param>
    /// <param name="chatId">The unique identifier of the chat.</param>
    /// <returns>A new bot dialog.</returns>
    public TBotDialog Create(IBotOperator bot, ChatId chatId);
}

/// <summary>
/// Represents a bot dialog with a user in a specific chat.
/// </summary>
public interface IBotDialog
{
    /// <summary>
    /// The unique identifier of the chat.
    /// </summary>
    ChatId ChatId { get; }

    /// <summary>
    /// Begins the bot dialog.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task BeginAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Ends the bot dialog.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task EndAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Handles an incoming chat message.
    /// </summary>
    /// <param name="message">The incoming message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task HandleAsync(Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Handles a callback query.
    /// </summary>
    /// <param name="callback">The callback query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task HandleAsync(CallbackQuery callback, CancellationToken cancellationToken);

    /// <summary>
    /// Handles a bot error.
    /// </summary>
    /// <param name="error">The exception.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task HandleAsync(Exception error, CancellationToken cancellationToken);
}

/// <summary>
/// Defines the bot dialog general initialization and termination.
/// </summary>
/// <typeparam name="TSelf">The type that implements the bot dialog.</typeparam>
public interface IBotDialog<TSelf> : IBotDialog
    where TSelf : IBotDialog<TSelf>
{
    /// <summary>
    /// Initializes the bot.
    /// </summary>
    /// <param name="bot">The bot API client.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    static abstract Task StartAsync(IBot bot, CancellationToken cancellationToken);

    /// <summary>
    /// Terminates the bot.
    /// </summary>
    /// <param name="bot">The bot API client.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    static abstract Task StopAsync(IBot bot, CancellationToken cancellationToken);
}
