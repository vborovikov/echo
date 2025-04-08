namespace Echo;

using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram;

public interface IBotDialogFactory
{
    public BotDialog Create(ChatId chatId, IChat chat);
}

/// <summary>
/// Represents a bot dialog with a user in a specific chat.
/// </summary>
public class BotDialog
{
    private readonly ChatId chatId;

    public BotDialog(ChatId chatId, IChat chat)
    {
        this.chatId = chatId;
        this.Chat = chat;
    }

    public IChat Chat { get; }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task HandleAsync(CallbackQuery callback, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}