namespace Echo;

using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram;

public interface IBotDialogFactory
{
    public IBotDialog Create(ChatId chatId, IChat chat);
}

/// <summary>
/// Represents a bot dialog with a user in a specific chat.
/// </summary>
public interface IBotDialog
{
    Task HandleAsync(Exception error, CancellationToken cancellationToken);
    Task HandleAsync(Message message, CancellationToken cancellationToken);
    Task HandleAsync(CallbackQuery callback, CancellationToken cancellationToken);
}

//public abstract class BotDialog
//{
//    public BotDialog(ChatId chatId, IChat chat)
//    {
//        this.ChatId = chatId;
//        this.Chat = chat;
//    }

//    public ChatId ChatId { get; }
//    public IChat Chat { get; }

//    public abstract Task HandleAsync(Message message, CancellationToken cancellationToken);

//    public abstract Task HandleAsync(CallbackQuery callback, CancellationToken cancellationToken);
//}