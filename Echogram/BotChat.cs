namespace Echo;

using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram;

/// <summary>
/// Produces new bot chats.
/// </summary>
/// <typeparam name="TBotChat">The concrete type of the bot chat.</typeparam>
public interface IBotForum<TBotChat> where TBotChat : IBotChat<TBotChat>
{
    /// <summary>
    /// Creates a new bot chat.
    /// </summary>
    /// <param name="bot">The bot operator.</param>
    /// <param name="chatId">The unique identifier of the chat.</param>
    /// <returns>A new bot chat.</returns>
    public TBotChat Create(IBotOperator bot, ChatId chatId);
}

/// <summary>
/// Represents the bot interaction with a user in a specific chat.
/// </summary>
public interface IBotChat : IDisposable
{
    /// <summary>
    /// The unique identifier of the chat.
    /// </summary>
    ChatId ChatId { get; }

    /// <summary>
    /// Begins the bot interaction with a user.
    /// </summary>
    /// <param name="user">The user who begun the chat, or <c>null</c> if the chat was begun by the bot.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task BeginAsync(User? user, CancellationToken cancellationToken);

    /// <summary>
    /// Ends the bot interaction with a user.
    /// </summary>
    /// <param name="user">The user who is ending the chat, or <c>null</c> if the chat was forced to end.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task EndAsync(User? user, CancellationToken cancellationToken);

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
/// Defines the bot general initialization and termination.
/// </summary>
/// <typeparam name="TSelf">The type that implements the bot interaction.</typeparam>
public interface IBotChat<TSelf> : IBotChat
    where TSelf : IBotChat<TSelf>
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
