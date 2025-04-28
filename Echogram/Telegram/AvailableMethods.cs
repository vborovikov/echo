namespace Echo.Telegram;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a specific response from the Telegram Bot API.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
record ApiResponse<TResult> : ApiResponse
{
    public TResult? Result { get; init; }
}

/// <summary>
/// Represents a request to the Telegram Bot API.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
/// <param name="Method">The API method to call.</param>
public abstract record ApiRequest<TResult>(string Method);

public enum ParseMode
{
    Default,
    Markdown,
    MarkdownV2,
    Html,
}

/// <summary>
/// Describes the options used for link preview generation.
/// </summary>
public record LinkPreviewOptions
{
    /// <summary>
    /// True, if the link preview is disabled.
    /// </summary>
    public bool? IsDisabled { get; init; }
    /// <summary>
    /// URL to use for the link preview. If empty, then the first URL found in the message text will be used.
    /// </summary>
    public string? Url { get; init; }
    /// <summary>
    /// True, if the media in the link preview is supposed to be shrunk; 
    /// ignored if the URL isn't explicitly specified or media size change isn't supported for the preview.
    /// </summary>
    public bool? PreferSmallMedia { get; init; }
    /// <summary>
    /// True, if the media in the link preview is supposed to be enlarged;
    /// ignored if the URL isn't explicitly specified or media size change isn't supported for the preview
    /// </summary>
    public bool? PreferLargeMedia { get; init; }
    /// <summary>
    /// True, if the link preview must be shown above the message text; 
    /// otherwise, the link preview will be shown below the message text
    /// </summary>
    public bool? ShowAboveText { get; init; }
}

public record ReplyParameters(MessageId MessageId)
{
    //public required int MessageId { get; init; }
    public ChatId? ChatId { get; init; }
    public bool? AllowSendingWithoutReply { get; init; }
    public string? Quote { get; init; }
    public ParseMode? QuoteParseMode { get; init; }
    public MessageEntity[]? QuoteEntities { get; init; }
    public int? QuotePosition { get; init; }
}

[JsonDerivedType(typeof(InlineKeyboardMarkup))]
[JsonDerivedType(typeof(ReplyKeyboardMarkup))]
[JsonDerivedType(typeof(ReplyKeyboardRemove))]
[JsonDerivedType(typeof(ForceReplyMarkup))]
public abstract record ReplyMarkup;

public record InlineKeyboardMarkup(InlineKeyboardButton[][] InlineKeyboard) : ReplyMarkup;

public record InlineKeyboardButton(string Text)
{
    public InlineKeyboardButton(string text, string callbackData) : this(text)
    {
        this.CallbackData = callbackData;
    }

    public string? Url { get; init; }
    public string CallbackData { get; init; } = Text;
    public CopyTextButton? CopyText { get; init; }
}

public record CopyTextButton(string Text);

public record ReplyKeyboardMarkup(KeyboardButton[][] Keyboard) : ReplyMarkup
{
    public bool? IsPersistent { get; init; }
    public bool ResizeKeyboard { get; init; } = true;
    public bool? OneTimeKeyboard { get; init; }
    public string? InputFieldPlaceholder { get; init; }
    public bool? Selective { get; init; }
}

public record KeyboardButton(string Text)
{
    //public bool? RequestUsers { get; init; }
    //public bool? RequestChat { get; init; }
    public bool? RequestContact { get; init; }
    public bool? RequestLocation { get; init; }
    //public bool? RequestPoll { get; init; }
    //public bool? RequestWebApp { get; init; }
}

public record ReplyKeyboardRemove : ReplyMarkup
{
    public bool RemoveKeyboard => true;
    public bool? Selective { get; init; }
}

public record ForceReplyMarkup : ReplyMarkup
{
    public bool ForceReply => true;
    public bool? Selective { get; init; }
    public string? InputFieldPlaceholder { get; init; }
}

public record BotCommand(string Command, string Description);

public enum BotCommandScopeType
{
    Default,
    AllPrivateChats,
    AllGroupChats,
    AllChatAdministrators,
    Chat,
    ChatAdministrators,
    ChatMember,
}

public record BotCommandScope(BotCommandScopeType Type)
{
    public ChatId? ChatId { get; init; }
    public UserId? UserId { get; init; }
}

public static class Api
{
    /// <summary>
    /// Returns basic information about the bot in form of a <see cref="User"/> object.
    /// </summary>
    public sealed record GetMe() : ApiRequest<BotUser>("getMe")
    {
        public static readonly GetMe Default = new();
    }

    public static Task<BotUser> GetMeAsync(this IBot bot, CancellationToken cancellationToken)
    {
        return bot.ExecuteAsync(GetMe.Default, cancellationToken);
    }

    /// <summary>
    /// Receives incoming updates using long polling.
    /// </summary>
    internal sealed record GetUpdates() : ApiRequest<Update[]>("getUpdates")
    {
        public int? Offset { get; init; }
        public int? Limit { get; init; } = 100;
        public int? Timeout { get; init; } = 60;
        public UpdateType AllowedUpdates { get; init; }
    }

    /// <summary>
    /// Sends a message to a chat.
    /// </summary>
    public sealed record SendMessage(ChatId ChatId, string Text) : ApiRequest<Message>("sendMessage")
    {
        public ParseMode? ParseMode { get; init; }
        public MessageEntity[]? Entities { get; init; }
        public LinkPreviewOptions? LinkPreviewOptions { get; init; }
        public bool? DisableNotification { get; init; }
        public bool? ProtectContent { get; init; }
        public string? MessageEffectId { get; init; }
        /// <summary>
        /// Description of the message to reply to.
        /// </summary>
        /// <remarks>
        /// The original message will be quoted.
        /// </remarks>
        public ReplyParameters? ReplyParameters { get; init; }
        public ReplyMarkup? ReplyMarkup { get; init; }
    }

    public static Task<Message> SendMessageAsync(this IBot bot, ChatId chatId, string text, CancellationToken cancellationToken)
    {
        return bot.ExecuteAsync(new SendMessage(chatId, text), cancellationToken);
    }

    public static Task<Message> SendMessageAsync(this IBot bot, ChatId chatId, string text, ParseMode parseMode, CancellationToken cancellationToken)
    {
        return bot.ExecuteAsync(new SendMessage(chatId, text) { ParseMode = parseMode }, cancellationToken);
    }

    public static Task<Message> SendMessageAsync(this IBot bot, ChatId chatId, string text, ReplyMarkup replyMarkup, CancellationToken cancellationToken)
    {
        return bot.ExecuteAsync(new SendMessage(chatId, text) { ReplyMarkup = replyMarkup }, cancellationToken);
    }

    public sealed record SetMyCommands(BotCommand[] Commands) : ApiRequest<bool>("setMyCommands")
    {
        public BotCommandScope? Scope { get; init; }
        public string? LanguageCode { get; init; }
    }

    public static Task<bool> SetMyCommandsAsync(this IBot bot, BotCommand[] commands, CancellationToken cancellationToken)
    {
        return bot.ExecuteAsync(new SetMyCommands(commands) { Scope = new(BotCommandScopeType.Default) }, cancellationToken);
    }

    /// <summary>
    /// Sends answers to callback queries sent from inline keyboards.
    /// </summary>
    /// <remarks>The answer will be displayed to the user as a notification at the top of the chat screen or as an alert.</remarks>
    /// <param name="CallbackQueryId">Unique identifier for the query to be answered.</param>
    public sealed record AnswerCallbackQuery(string CallbackQueryId) : ApiRequest<bool>("answerCallbackQuery")
    {
        /// <summary>
        /// Text of the notification. If not specified, nothing will be shown to the user, 0-200 characters.
        /// </summary>
        public string? Text { get; init; }
        /// <summary>
        /// If True, an alert will be shown by the client instead of a notification at the top of the chat screen.
        /// </summary>
        public bool ShowAlert { get; init; }
        /// <summary>
        /// URL that will be opened by the user's client.
        /// </summary>
        public bool? Url { get; init; }
        /// <summary>
        /// The maximum amount of time in seconds that the result of the callback query may be cached client-side. 
        /// Telegram apps will support caching starting in version 3.14. Defaults to 0.
        /// </summary>
        public int CacheTime { get; init; }
    }

    public static Task<bool> AnswerCallbackQueryAsync(this IBot bot, string callbackQueryId, CancellationToken cancellationToken)
    {
        return bot.ExecuteAsync(new AnswerCallbackQuery(callbackQueryId), cancellationToken);
    }

    public static Task<bool> AnswerCallbackQueryAsync(this IBot bot, string callbackQueryId, string text, CancellationToken cancellationToken)
    {
        return bot.ExecuteAsync(new AnswerCallbackQuery(callbackQueryId) { Text = text, ShowAlert = false }, cancellationToken);
    }
}