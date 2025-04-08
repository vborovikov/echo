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

/// <summary>
/// Returns basic information about the bot in form of a <see cref="User"/> object.
/// </summary>
public sealed record ApiGetMe() : ApiRequest<BotUser>("getMe")
{
    public static readonly ApiGetMe Default = new();
}

/// <summary>
/// Receives incoming updates using long polling.
/// </summary>
sealed record ApiGetUpdates() : ApiRequest<Update[]>("getUpdates")
{
    public int? Offset { get; init; }
    public int? Limit { get; init; } = 100;
    public int? Timeout { get; init; } = 60;
    public UpdateType AllowedUpdates { get; init; }
}

public enum ParseMode
{
    Default,
    Markdown,
    MarkdownV2,
    Html,
}

public record LinkPreviewOptions
{
    public bool? IsDisabled { get; init; }
    public string? Url { get; init; }
    public bool? PreferSmallMedia { get; init; }
    public bool? PreferLargeMedia { get; init; }
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
    public string? Url { get; init; }
    public string? CallbackData { get; init; }
    public CopyTextButton? CopyText { get; init; }
}

public record CopyTextButton(string Text);

public record ReplyKeyboardMarkup(KeyboardButton[][] Keyboard) : ReplyMarkup
{
    public bool? IsPersistent { get; init; }
    public bool? ResizeKeyboard { get; init; }
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

/// <summary>
/// Sends a message to a chat.
/// </summary>
public sealed record ApiSendMessage(ChatId ChatId, string Text) : ApiRequest<Message>("sendMessage")
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