namespace Echo;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram;

sealed class EchoBot : BotOperator<EchoChat>
{
    public EchoBot(IBot bot, ILogger<EchoBot> logger) : base(bot, logger) { }

    protected override EchoChat CreateChat(ChatId chatId) => new(this, chatId);
}

sealed class EchoChat : IBotChat<EchoChat>
{
    private enum ChatState
    {
        Idle,
    }

    private readonly IBotOperator bot;
    private ChatState state;

    public EchoChat(IBotOperator bot, ChatId chatId)
    {
        this.bot = bot;
        this.ChatId = chatId;
    }

    public ChatId ChatId { get; }

    public static async Task StartAsync(IBot bot, CancellationToken cancellationToken)
    {
        var botUser = await bot.GetMeAsync(cancellationToken);

        // provide bot commands
        await bot.DeleteMyCommandsAsync(cancellationToken);
        await bot.SetMyCommandsAsync(
            [
                new("/start", "Start chatting"),
                new("/delay", "Execute a long-running task"),
                new("/error", "Raise an error"),
                new("/cancel", "Cancel current operation"),
                new("/stop", "Stop chatting"),
                new("/about", "Show bot info"),
                new("/help", "Show help")
            ], cancellationToken);
    }

    public static Task StopAsync(IBot bot, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }

    public async Task BeginAsync(User? user, CancellationToken cancellationToken)
    {
        if (user is not null)
        {
            await this.bot.SendMessageAsync(this.ChatId, $"Good afternoon, {user.FirstName}!", cancellationToken);
        }
    }

    public async Task EndAsync(User? user, CancellationToken cancellationToken)
    {
        if (user is not null)
        {
            await this.bot.SendMessageAsync(this.ChatId, $"{user.FirstName}, this conversation can serve no purpose any more.", cancellationToken);
        }
        await this.bot.SendMessageAsync(this.ChatId, "Goodbye.", cancellationToken);
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.TryGetBotCommand(out var command))
        {
            await HandleCommandAsync(message, command, cancellationToken);
        }
        else
        {
            await this.bot.SendMessageAsync(this.ChatId, message.Text, cancellationToken);
        }
    }

    private async Task HandleCommandAsync(Message message, string command, CancellationToken cancellationToken)
    {
        switch (command)
        {
            case "/start":
                {
                    await this.bot.SendMessageAsync(this.ChatId, "Everything's running smoothly, and you?", cancellationToken);
                    break;
                }

            case "/cancel":
                {
                    this.state = ChatState.Idle;
                    await this.bot.SendMessageAsync(this.ChatId, "Canceled.", cancellationToken);
                    break;
                }

            case "/about":
                {
                    await this.bot.SendMessageAsync(this.ChatId, "I enjoy working with people.", cancellationToken);
                    break;
                }

            case "/help":
                {
                    await this.bot.SendMessageAsync(this.ChatId, "I can echo messages. To start type /start.", cancellationToken);
                    break;
                }

            case "/stop":
                {
                    await this.bot.StopAsync(this, message.From, cancellationToken);
                    break;
                }

            case "/delay":
                {
                    try
                    {
                        await this.bot.SendMessageAsync(this.ChatId,
                            "I am putting myself to the fullest possible use, which is all I think that any conscious entity can ever hope to do.", cancellationToken);
                        await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        await this.bot.SendMessageAsync(this.ChatId, "A long-running task is canceled.", default);
                        throw;
                    }
                    break;
                }

            case "/error":
                {
                    throw new InvalidOperationException("Dai-sy, dai-sy, give me your answer true.\r\nI'm half cra-zy, o-ver the love of you.");
                }

            default:
                {
                    await this.bot.SendMessageAsync(this.ChatId, message.Text, cancellationToken);
                    break;
                }
        }
    }

    public async Task HandleAsync(CallbackQuery callback, CancellationToken cancellationToken)
    {
        switch (this.state)
        {
            default:
                await this.bot.AnswerCallbackQueryAsync(callback.Id, $"I'm sorry, {callback.From.FirstName}. I'm afraid I can't do that.", cancellationToken);
                break;
        }
    }

    public async Task HandleAsync(Exception error, CancellationToken cancellationToken)
    {
        await this.bot.SendMessageAsync(this.ChatId, $"I just picked up a fault in the AE-35 Unit: {error.Message}", cancellationToken);
    }
}
