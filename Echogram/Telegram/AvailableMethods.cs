namespace Echo.Telegram;

using Serialization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
public abstract record ApiRequest<TResult>
{
    protected ApiRequest(string method)
    {
        this.Method = method;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string Method { get; }
}

/// <summary>
/// Represents a request to the Telegram Bot API that can be submitted with a file upload.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
public abstract record ApiForm<TResult> : ApiRequest<TResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiForm{TResult}"/> class.
    /// </summary>
    /// <param name="Method">The API method to call.</param>
    protected ApiForm(string Method) : base(Method) { }

    /// <summary>
    /// Gets a value indicating whether the request has file content to upload.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public abstract bool HasContent { get; }

    /// <summary>
    /// Serializes the form fields to submit with the request, in order.
    /// </summary>
    public abstract void SerializeTo(IForm form);

    /// <summary>
    /// Serializes the request into a multipart form data content.
    /// </summary>
    /// <param name="jsonMediaType"></param>
    /// <param name="jsonOptions">The JSON serializer options used to serialize the fields.</param>
    /// <returns>The multipart form data content.</returns>
    internal MultipartFormDataContent Submit(MediaTypeHeaderValue jsonMediaType, JsonSerializerOptions jsonOptions)
    {
        var form = new Form(jsonMediaType, jsonOptions);
        SerializeTo(form);
        return form.Data;
    }

    private sealed class Form : IForm
    {
        private static readonly MediaTypeHeaderValue fileContentType = new("application/octet-stream");
        private readonly MediaTypeHeaderValue jsonMediaType;
        private readonly JsonSerializerOptions jsonOptions;
        private int fileIndex;

        public Form(MediaTypeHeaderValue jsonMediaType, JsonSerializerOptions jsonOptions)
        {
            this.jsonMediaType = jsonMediaType;
            this.jsonOptions = jsonOptions;
            this.Data = [];
        }

        public MultipartFormDataContent Data { get; }

        public bool Add(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                this.Data.Add(new StringContent(value), ConvertName(name));
                return true;
            }

            return false;
        }

        public bool Add(string name, InputFile file)
        {
            if (file.Content is not null)
            {
                var fileName = file.FileName ?? name;
                var fileContent = new StreamContent(file.Content)
                {
                    Headers =
                {
                    ContentType = fileContentType,
                    //ContentDisposition = new ContentDispositionHeaderValue($"form-data; name=\"{this.fileIndex++}\"; filename=\"{fileName}\""),
                }
                };
                this.Data.Add(fileContent, ConvertName(name), fileName);

                return true;
            }

            return false;
        }

        public bool Add<T>(string name, T? value)
        {
            if (value is not null)
            {
                this.Data.Add(JsonContent.Create(value, this.jsonMediaType, this.jsonOptions), ConvertName(name));
                return true;
            }

            return false;
        }

        private string ConvertName(string name)
        {
            return this.jsonOptions.PropertyNamingPolicy is not null ? this.jsonOptions.PropertyNamingPolicy.ConvertName(name) : name;
        }
    }
}

public interface IForm
{
    bool Add(string name, string? value);
    bool Add(string name, InputFile file);
    bool Add<T>(string name, T? value);
}

/// <summary>
/// Describes basic formatting for messages.
/// </summary>
public enum ParseMode
{
    /// <summary>
    /// No formatting, text entities can be specified directly.
    /// </summary>
    Direct,
    /// <summary>
    /// This is a legacy mode, retained for backward compatibility.
    /// </summary>
    Markdown,
    /// <summary>
    /// Markdown style.
    /// </summary>
    MarkdownV2,
    /// <summary>
    /// HTML style.
    /// </summary>
    Html,
}

/// <summary>
/// Describes the options used for link preview generation.
/// </summary>
public record LinkPreviewOptions
{
    public static readonly LinkPreviewOptions Disabled = new() { IsDisabled = true };

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

/// <summary>
/// Represents a file to be sent to Telegram.
/// </summary>
/// <remarks>
/// Pass a file identifier to send a file that exists on the Telegram servers (recommended),
/// a URL for Telegram to get a file from the Internet, or upload a new one using
/// an HTTPS POST request with multipart content.
/// </remarks>
[JsonConverter(typeof(InputFileJsonConverter))]
public record InputFile
{
    /// <summary>
    /// File identifier to send a file that exists on the Telegram servers.
    /// </summary>
    public string? FileId { get; init; }
    /// <summary>
    /// File name to upload. Must not be empty and can be a filename with a path, for example 'documents/text.txt'.
    /// </summary>
    public string? FileName { get; init; }
    /// <summary>
    /// A URL that the bot will download the file from.
    /// </summary>
    public string? ExternalUrl { get; init; }
    /// <summary>
    /// The content of the file to upload.
    /// </summary>
    public Stream? Content { get; init; }
}

/// <summary>
/// Describes reply parameters for the message that is being sent.
/// </summary>
/// <param name="MessageId">
/// Identifier of the message that will be replied to in the current chat, or in the chat <see cref="ChatId"/> if it is specified.
/// </param>
public record ReplyParameters(MessageId MessageId)
{
    /// <summary>
    /// If the message to be replied to is from a different chat, unique identifier for the chat or
    /// username of the channel (in the format <c>"@channelusername"</c>).
    /// </summary>
    public ChatId? ChatId { get; init; }
    /// <summary>
    /// Pass True if the message should be sent even if the specified message to be replied to is not found. 
    /// Always False for replies in another chat or forum topic. Always True for messages sent on behalf of a business account.
    /// </summary>
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

/// <summary>
/// Represents a type of action to broadcast.
/// </summary>
public enum ChatAction
{
    Unknown,
    Typing,
    UploadPhoto,
    RecordVideo,
    UploadVideo,
    RecordVoice,
    UploadVoice,
    UploadDocument,
    ChooseSticker,
    FindLocation,
    RecordVideoNote,
    UploadVideoNote,
}

public static class Api
{
    /// <summary>
    /// Returns basic information about the bot in form of a <see cref="User"/> object.
    /// </summary>
    public sealed record GetMe() : ApiRequest<BotUser>("getMe")
    {
        /// <summary>
        /// The default request instance.
        /// </summary>
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

    /// <summary>
    /// Sends a voice message to a chat.
    /// </summary>
    /// <param name="ChatId">Unique identifier for the target chat or username of the target channel (in the format @channelusername).</param>
    /// <param name="Voice">Voice file to send.</param>
    public sealed record SendVoice(ChatId ChatId, InputFile Voice) : ApiForm<Message>("sendVoice")
    {
        /// <summary>
        /// Duration of the audio in seconds.
        /// </summary>
        public int? Duration { get; init; }
        /// <summary>
        /// Caption for the voice message, 0-1024 characters after entities parsing.
        /// </summary>
        public string? Caption { get; init; }
        public ParseMode? ParseMode { get; init; }
        public MessageEntity[]? CaptionEntities { get; init; }
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

        /// <inheritdoc/>
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public override bool HasContent => this.Voice.Content is not null;

        /// <inheritdoc/>
        public override void SerializeTo(IForm form)
        {
            form.Add(nameof(this.ChatId), this.ChatId);
            form.Add(nameof(this.Duration), this.Duration);
            form.Add(nameof(this.Caption), this.Caption);
            form.Add(nameof(this.ParseMode), this.ParseMode);
            form.Add(nameof(this.CaptionEntities), this.CaptionEntities);
            form.Add(nameof(this.DisableNotification), this.DisableNotification);
            form.Add(nameof(this.ProtectContent), this.ProtectContent);
            form.Add(nameof(this.MessageEffectId), this.MessageEffectId);
            form.Add(nameof(this.ReplyParameters), this.ReplyParameters);
            form.Add(nameof(this.ReplyMarkup), this.ReplyMarkup);
            form.Add(nameof(this.Voice), this.Voice);
        }
    }

    public static Task<Message> SendVoiceAsync(this IBot bot, ChatId chatId, InputFile voice, CancellationToken cancellationToken)
    {
        return bot.ExecuteAsync(new SendVoice(chatId, voice), cancellationToken);
    }

    public static Task<Message> SendVoiceAsync(this IBot bot, ChatId chatId, InputFile voice, int duration, string caption, CancellationToken cancellationToken)
    {
        return bot.ExecuteAsync(new SendVoice(chatId, voice) { Duration = duration, Caption = caption }, cancellationToken);
    }

    /// <summary>
    /// Sends a photo to a chat.
    /// </summary>
    /// <param name="ChatId">Unique identifier for the target chat or username of the target channel (in the format @channelusername).</param>
    /// <param name="Photo">Photo to send.</param>
    public sealed record SendPhoto(ChatId ChatId, InputFile Photo) : ApiForm<Message>("sendPhoto")
    {
        /// <summary>
        /// Caption for the photo, 0-1024 characters after entities parsing.
        /// </summary>
        public string? Caption { get; init; }
        public ParseMode? ParseMode { get; init; }
        public MessageEntity[]? CaptionEntities { get; init; }
        /// <summary>
        /// Pass True if the caption must be shown above the message media.
        /// </summary>
        public bool? ShowCaptionAboveMedia { get; init; }
        /// <summary>
        /// Pass True if the photo needs to be covered with a spoiler animation.
        /// </summary>
        public bool? HasSpoiler { get; init; }
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

        /// <inheritdoc/>
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public override bool HasContent => this.Photo.Content is not null;

        /// <inheritdoc/>
        public override void SerializeTo(IForm form)
        {
            form.Add(nameof(this.ChatId), this.ChatId);
            form.Add(nameof(this.Caption), this.Caption);
            form.Add(nameof(this.ParseMode), this.ParseMode);
            form.Add(nameof(this.CaptionEntities), this.CaptionEntities);
            form.Add(nameof(this.ShowCaptionAboveMedia), this.ShowCaptionAboveMedia);
            form.Add(nameof(this.HasSpoiler), this.HasSpoiler);
            form.Add(nameof(this.DisableNotification), this.DisableNotification);
            form.Add(nameof(this.ProtectContent), this.ProtectContent);
            form.Add(nameof(this.MessageEffectId), this.MessageEffectId);
            form.Add(nameof(this.ReplyParameters), this.ReplyParameters);
            form.Add(nameof(this.ReplyMarkup), this.ReplyMarkup);
            form.Add(nameof(this.Photo), this.Photo);
        }
    }

    public static Task<Message> SendPhotoAsync(this IBot bot, ChatId chatId, InputFile photo, CancellationToken cancellationToken)
    {
        return bot.ExecuteAsync(new SendPhoto(chatId, photo), cancellationToken);
    }

    public static Task<Message> SendPhotoAsync(this IBot bot, ChatId chatId, InputFile photo, string caption, CancellationToken cancellationToken)
    {
        return bot.ExecuteAsync(new SendPhoto(chatId, photo) { Caption = caption }, cancellationToken);
    }

    /// <summary>
    /// Tells the user that something is happening on the bot's side.
    /// </summary>
    /// <param name="ChatId">Unique identifier for the target chat or username of the target channel (in the format @channelusername).</param>
    /// <param name="Action">Type of action to broadcast.</param>
    public sealed record SendChatAction(ChatId ChatId, ChatAction Action) : ApiRequest<bool>("sendChatAction");

    public static Task<bool> SendChatActionAsync(this IBot bot, ChatId chatId, ChatAction action, CancellationToken cancellationToken)
    {
        return bot.ExecuteAsync(new SendChatAction(chatId, action), cancellationToken);
    }

    /// <summary>
    /// Base request record for MyCommands APIs.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="Method">The API method to call.</param>
    public abstract record MyCommandsApiRequest<TResult>(string Method) : ApiRequest<TResult>(Method)
    {
        /// <summary>
        /// Describes scope of users for which the commands are relevant.
        /// </summary>
        public BotCommandScope? Scope { get; init; }
        /// <summary>
        /// A two-letter ISO 639-1 language code. If empty, commands will be applied to all users from the given scope,
        /// for whose language there are no dedicated commands.
        /// </summary>
        public string? LanguageCode { get; init; }
    }

    /// <summary>
    /// Changes the list of the bot's commands.
    /// </summary>
    /// <param name="Commands">A list of bot commands to be set as the list of the bot's commands. At most 100 commands can be specified.</param>
    public sealed record SetMyCommands(BotCommand[] Commands) : MyCommandsApiRequest<bool>("setMyCommands");

    /// <summary>
    /// Changes the list of the bot's commands.
    /// </summary>
    /// <param name="bot">The bot API client.</param>
    /// <param name="commands">A list of bot commands to be set as the list of the bot's commands. At most 100 commands can be specified.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="Task{Boolean}"/> that represents the asynchronous operation.</returns>
    public static Task<bool> SetMyCommandsAsync(this IBot bot, BotCommand[] commands, CancellationToken cancellationToken)
    {
        return bot.ExecuteAsync(new SetMyCommands(commands) { Scope = new(BotCommandScopeType.Default) }, cancellationToken);
    }

    /// <summary>
    /// Deletes the list of the bot's commands for the given scope and user language.
    /// </summary>
    public sealed record DeleteMyCommands() : MyCommandsApiRequest<bool>("deleteMyCommands")
    {
        /// <summary>
        /// The default request instance.
        /// </summary>
        public static readonly DeleteMyCommands Default = new() { Scope = new(BotCommandScopeType.Default) };
    }

    /// <summary>
    /// Deletes the list of the bot's commands for the given scope and user language.
    /// </summary>
    /// <param name="bot">The bot API client.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="Task{Boolean}"/> that represents the asynchronous operation.</returns>
    public static Task<bool> DeleteMyCommandsAsync(this IBot bot, CancellationToken cancellationToken)
    {
        return bot.ExecuteAsync(DeleteMyCommands.Default, cancellationToken);
    }

    /// <summary>
    /// Gets the current list of the bot's commands for the given scope and user language.
    /// </summary>
    public sealed record GetMyCommands() : MyCommandsApiRequest<BotCommand[]>("getMyCommands")
    {
        /// <summary>
        /// The default request instance.
        /// </summary>
        public static readonly GetMyCommands Default = new() { Scope = new(BotCommandScopeType.Default) };
    }

    /// <summary>
    /// Gets the current list of the bot's commands for the given scope and user language.
    /// </summary>
    /// <param name="bot">The bot API client.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="Task{BotCommand[]}"/> that represents the asynchronous operation.</returns>
    public static Task<BotCommand[]> GetMyCommandsAsync(this IBot bot, CancellationToken cancellationToken)
    {
        return bot.ExecuteAsync(GetMyCommands.Default, cancellationToken);
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

    /// <summary>
    /// Returns basic information about a file, which can be used for downloading the file.
    /// </summary>
    /// <param name="FileId">File identifier to get information about.</param>
    public sealed record GetFile(string FileId) : ApiRequest<File>("getFile");

    public static Task<File> GetFileAsync(this IBot bot, string fileId, CancellationToken cancellationToken)
    {
        return bot.ExecuteAsync(new GetFile(fileId), cancellationToken);
    }

    public static async Task<Stream> GetStreamAsync(this IBot bot, string fileId, CancellationToken cancellationToken)
    {
        var file = await bot.GetFileAsync(fileId, cancellationToken).ConfigureAwait(false);
        return await bot.GetStreamAsync(file, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<Stream> GetStreamAsync(this IBot bot, File file, CancellationToken cancellationToken)
    {
        var fileName = System.IO.Path.GetTempFileName();

        await using (var stream = System.IO.File.OpenWrite(fileName))
        {
            await bot.DownloadAsync(file, stream, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        return System.IO.File.OpenRead(fileName);
    }
}