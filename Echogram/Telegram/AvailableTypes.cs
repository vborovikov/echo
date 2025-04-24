namespace Echo.Telegram;

using System.Text.Json.Serialization;
using Serialization;

/// <summary>
/// Represents a response from the Telegram Bot API.
/// </summary>
public record ApiResponse
{
    public required bool Ok { get; init; }
    public string? Description { get; init; }
    public int? ErrorCode { get; init; }
    public ResponseParameters? Parameters { get; init; }
}

/// <summary>
/// Describes why a request was unsuccessful.
/// </summary>
public record ResponseParameters
{
    /// <summary>
    /// In case of exceeding flood control, the number of seconds left to wait before the request can be repeated.
    /// </summary>
    public int? RetryAfter { get; init; }
    /// <summary>
    /// The group has been migrated to a supergroup with the specified identifier.
    /// </summary>
    public ChatId? MigrateToChatId { get; init; }
}

/// <summary>
/// Represents a Telegram user.
/// </summary>
public record User
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public required UserId Id { get; init; }
    /// <summary>
    /// True, if this user is a bot.
    /// </summary>
    public required bool IsBot { get; init; }
    /// <summary>
    /// User's or bot's first name.
    /// </summary>
    public required string FirstName { get; init; }
    /// <summary>
    /// User's or bot's last name.
    /// </summary>
    public string? LastName { get; init; }
    /// <summary>
    /// User's or bot's username.
    /// </summary>
    public string? Username { get; init; }
    /// <summary>
    /// IETF language tag of the user's language.
    /// </summary>
    public string? LanguageCode { get; init; }
    /// <summary>
    /// True, if this user is a Telegram Premium user.
    /// </summary>
    public bool? IsPremium { get; init; }
    /// <summary>
    /// True, if this user added the bot to the attachment menu.
    /// </summary>
    public bool? AddedToAttachmentMenu { get; init; }
}

/// <summary>
/// Represents a Telegram bot.
/// </summary>
public record BotUser : User
{
    /// <summary>
    /// True, if the bot can be invited to groups.
    /// </summary>
    public bool? CanJoinGroups { get; init; }
    /// <summary>
    /// True, if privacy mode is disabled for the bot.
    /// </summary>
    public bool? CanReadAllGroupMessages { get; init; }
    /// <summary>
    /// True, if the bot supports inline queries.
    /// </summary>
    public bool? SupportsInlineQueries { get; init; }
    /// <summary>
    /// True, if the bot can be connected to a Telegram Business account to receive its messages.
    /// </summary>
    public bool? CanConnectToBusiness { get; init; }
    /// <summary>
    /// True, if the bot has a main Web App.
    /// </summary>
    public bool? HasMainWebApp { get; init; }
}

/// <summary>
/// Represents the type of a chat.
/// </summary>
public enum ChatType
{
    Private,
    Group,
    Supergroup,
    Channel,
}

/// <summary>
/// Represents a Telegram chat.
/// </summary>
public record Chat
{
    /// <summary>
    /// Unique identifier for this chat.
    /// </summary>
    public required ChatId Id { get; init; }
    /// <summary>
    /// Type of the chat.
    /// </summary>
    public required ChatType Type { get; init; }
    /// <summary>
    /// Title, for supergroups, channels and group chats.
    /// </summary>
    public string? Title { get; init; }
    /// <summary>
    /// Username, for private chats, supergroups and channels if available.
    /// </summary>
    public string? Username { get; init; }
    /// <summary>
    /// First name of the other party in a private chat.
    /// </summary>
    public string? FirstName { get; init; }
    /// <summary>
    /// Last name of the other party in a private chat.
    /// </summary>
    public string? LastName { get; init; }
    /// <summary>
    /// True, if the supergroup chat is a forum (has topics enabled).
    /// </summary>
    public bool? IsForum { get; init; }
}

/// <summary>
/// Represents a chat photo.
/// </summary>
public record ChatPhoto
{
    /// <summary>
    /// File identifier of small (160x160) chat photo.
    /// </summary>
    /// <remarks>
    /// This file_id can be used only for photo download and only for as long as the photo is not changed.
    /// </remarks>
    public required string SmallFileId { get; init; }
    /// <summary>
    /// Unique file identifier of small (160x160) chat photo, which is supposed to be the same over time and for different bots.
    /// </summary>
    /// <remarks>
    /// Can't be used to download or reuse the file.
    /// </remarks>
    public required string SmallFileUniqueId { get; init; }
    /// <summary>
    /// File identifier of big (640x640) chat photo.
    /// </summary>
    /// <remarks>
    /// This file_id can be used only for photo download and only for as long as the photo is not changed.
    /// </remarks>
    public required string BigFileId { get; init; }
    /// <summary>
    /// Unique file identifier of big (640x640) chat photo, which is supposed to be the same over time and for different bots.
    /// </summary>
    /// <remarks>
    /// Can't be used to download or reuse the file.
    /// </remarks>
    public required string BigFileUniqueId { get; init; }
}

/// <summary>
/// Describes the birthdate of a user.
/// </summary>
public record Birthdate
{
    /// <summary>
    /// Day of the user's birth; 1-31.
    /// </summary>
    public required int Day { get; init; }
    /// <summary>
    /// Month of the user's birth; 1-12.
    /// </summary>
    public required int Month { get; init; }
    /// <summary>
    /// Year of the user's birth.
    /// </summary>
    public int? Year { get; init; }
}

[Flags]
public enum ReactionType
{
    Default = 0 << 0,
    Emoji = 1 << 0,
    CustomEmoji = 1 << 1,
    Paid = 1 << 2,
}

/// <summary>
/// Describes actions that a non-administrator user is allowed to take in a chat.
/// </summary>
public record ChatPermissions
{
    /// <summary>
    /// True, if the user is allowed to send text messages, contacts, locations and venues.
    /// </summary>
    public bool? CanSendMessages { get; init; }
    /// <summary>
    /// True, if the user is allowed to send audios.
    /// </summary>
    public bool? CanSendAudios { get; init; }
    /// <summary>
    /// True, if the user is allowed to send documents.
    /// </summary>
    public bool? CanSendDocuments { get; init; }
    /// <summary>
    /// True, if the user is allowed to send photos.
    /// </summary>
    public bool? CanSendPhotos { get; init; }
    /// <summary>
    /// True, if the user is allowed to send videos.
    /// </summary>
    public bool? CanSendVideos { get; init; }
    /// <summary>
    /// True, if the user is allowed to send video notes.
    /// </summary>
    public bool? CanSendVideoNotes { get; init; }
    /// <summary>
    /// True, if the user is allowed to send voice notes.
    /// </summary>
    public bool? CanSendVoiceNotes { get; init; }
    /// <summary>
    /// True, if the user is allowed to send polls.
    /// </summary>
    public bool? CanSendPolls { get; init; }
    /// <summary>
    /// True, if the user is allowed to send animations, games, stickers and use inline bots.
    /// </summary>
    public bool? CanSendOtherMessages { get; init; }
    /// <summary>
    /// True, if the user is allowed to add web page previews to their messages.
    /// </summary>
    public bool? CanAddWebPagePreviews { get; init; }
    /// <summary>
    /// True, if the user is allowed to change the chat title, photo and other settings.
    /// </summary>
    /// <remarks>
    /// Ignored in public supergroups.
    /// </remarks>
    public bool? CanChangeInfo { get; init; }
    /// <summary>
    /// True, if the user is allowed to invite new users to the chat.
    /// </summary>
    public bool? CanInviteUsers { get; init; }
    /// <summary>
    /// True, if the user is allowed to pin messages.
    /// </summary>
    /// <remarks>
    /// Ignored in public supergroups.
    /// </remarks>
    public bool? CanPinMessages { get; init; }
    /// <summary>
    /// True, if the user is allowed to create forum topics.
    /// </summary>
    /// <remarks>
    ///  If omitted defaults to the value of <see cref="CanPinMessages"/>.
    /// </remarks>
    public bool? CanManageTopics { get; init; }
}

/// <summary>
/// Represents a location to which a chat is connected.
/// </summary>
public record ChatLocation
{
    /// <summary>
    /// Location address.
    /// </summary>
    /// <remarks>
    /// 1-64 characters, as defined by the chat owner.
    /// </remarks>
    public required string Address { get; init; }
    /// <summary>
    /// The location to which the supergroup is connected.
    /// </summary>
    /// <remarks>
    /// Can't be a live location.
    /// </remarks>
    public required Location Location { get; init; }
}

/// <summary>
/// Represents a point on the map.
/// </summary>
public record Location
{
    /// <summary>
    /// Longitude as defined by sender.
    /// </summary>
    public required float Longitude { get; init; }
    /// <summary>
    /// Latitude as defined by sender.
    /// </summary>
    public required float Latitude { get; init; }
    /// <summary>
    /// The radius of uncertainty for the location, measured in meters; 0-1500.
    /// </summary>
    public float? HorizontalAccuracy { get; init; }
    /// <summary>
    /// Time relative to the message sending date, during which the location can be updated, in seconds.
    /// </summary>
    /// <remarks>
    /// For active live locations only.
    /// </remarks>
    public int? LivePeriod { get; init; }
    /// <summary>
    /// The direction in which user is moving, in degrees; 1-360.
    /// </summary>
    /// <remarks>
    /// For active live locations only.
    /// </remarks>
    public int? Heading { get; init; }
    /// <summary>
    /// Maximum distance for proximity alerts about approaching another chat member, in meters.
    /// </summary>
    /// <remarks>
    /// For sent live locations only.
    /// </remarks>
    public int? ProximityAlertRadius { get; init; }
}

/// <summary>
/// Contains full information about a chat.
/// </summary>
public record ChatFullInfo : Chat
{
    /// <summary>
    /// Identifier of the accent color for the chat name and backgrounds of the chat photo, reply header, and link preview.
    /// </summary>
    public int AccentColorId { get; init; }
    /// <summary>
    /// The maximum number of reactions that can be set on a message in the chat.
    /// </summary>
    public int MaxReactionCount { get; init; }
    /// <summary>
    /// Chat photo.
    /// </summary>
    public ChatPhoto? Photo { get; init; }
    /// <summary>
    /// If non-empty, the list of all active chat usernames; for private chats, supergroups and channels.
    /// </summary>
    public string[] ActiveUsernames { get; init; } = [];
    /// <summary>
    /// For private chats, the date of birth of the user.
    /// </summary>
    public Birthdate? Birthdate { get; init; }
    //public BusinessIntro? BusinessIntro { get; init; }
    //public BusinessLocation? BusinessLocation { get; init; }
    //public BusinessOpeningHours? BusinessOpeningHours { get; init; }
    /// <summary>
    /// For private chats, the personal channel of the user.
    /// </summary>
    public Chat? PersonalChat { get; init; }
    /// <summary>
    /// List of available reactions allowed in the chat. If omitted, then all emoji reactions are allowed.
    /// </summary>
    public ReactionType AvailableReactions { get; init; } = ReactionType.Emoji;
    /// <summary>
    /// Custom emoji identifier of the emoji chosen by the chat for the reply header and link preview background.
    /// </summary>
    public string? BackgroundCustomEmojiId { get; init; }
    /// <summary>
    /// Identifier of the accent color for the chat's profile background.
    /// </summary>
    public int? ProfileAccentColorId { get; init; }
    /// <summary>
    /// Custom emoji identifier of the emoji chosen by the chat for its profile background.
    /// </summary>
    public string? ProfileBackgroundCustomEmojiId { get; init; }
    /// <summary>
    /// Custom emoji identifier of the emoji status of the chat or the other party in a private chat.
    /// </summary>
    public string? EmojiStatusCustomEmojiId { get; init; }
    /// <summary>
    /// Expiration date of the emoji status of the chat or the other party in a private chat, in Unix time, if any.
    /// </summary>
    [JsonConverter(typeof(UnixDateTimeOffsetConverter))]
    public DateTimeOffset? EmojiStatusExpirationDate { get; init; }
    /// <summary>
    /// Bio of the other party in a private chat.
    /// </summary>
    public string? Bio { get; init; }
    /// <summary>
    /// True, if privacy settings of the other party in the private chat allows to use 'tg://user?id=<user_id>' links only in chats with the user.
    /// </summary>
    public bool? HasPrivateForwards { get; init; }
    /// <summary>
    /// True, if the privacy settings of the other party restrict sending voice and video note messages in the private chat.
    /// </summary>
    public bool? HasRestrictedVoiceAndVideoMessages { get; init; }
    /// <summary>
    /// True, if users need to join the supergroup before they can send messages.
    /// </summary>
    public bool? JoinToSendMessages { get; init; }
    /// <summary>
    /// True, if all users directly joining the supergroup without using an invite link need to be approved by supergroup administrators.
    /// </summary>
    public bool? JoinByRequest { get; init; }
    /// <summary>
    /// Description, for groups, supergroups and channel chats.
    /// </summary>
    public string? Description { get; init; }
    /// <summary>
    /// Primary invite link, for groups, supergroups and channel chats.
    /// </summary>
    public string? InviteLink { get; init; }
    /// <summary>
    /// The most recent pinned message (by sending date).
    /// </summary>
    public Message? PinnedMessage { get; init; }
    /// <summary>
    /// Default chat member permissions, for groups and supergroups.
    /// </summary>
    public ChatPermissions? Permissions { get; init; }
    /// <summary>
    /// True, if gifts can be sent to the chat.
    /// </summary>
    public bool? CanSendGift { get; init; }
    /// <summary>
    /// True, if paid media messages can be sent or forwarded to the channel chat. The field is available only for channel chats.
    /// </summary>
    public bool? CanSendPaidMedia { get; init; }
    /// <summary>
    /// For supergroups, the minimum allowed delay between consecutive messages sent by each unprivileged user; in seconds.
    /// </summary>
    public int? SlowModeDelay { get; init; }
    /// <summary>
    /// For supergroups, the minimum number of boosts that a non-administrator user needs to add in order to ignore slow mode and chat permissions.
    /// </summary>
    public int? UnrestrictBoostCount { get; init; }
    /// <summary>
    /// The time after which all messages sent to the chat will be automatically deleted; in seconds.
    /// </summary>
    public int? MessageAutoDeleteTime { get; init; }
    /// <summary>
    /// True, if aggressive anti-spam checks are enabled in the supergroup.
    /// </summary>
    /// <remarks>
    /// The field is only available to chat administrators.
    /// </remarks>
    public bool? HasAggressiveAntiSpamEnabled { get; init; }
    /// <summary>
    /// True, if non-administrators can only get the list of bots and administrators in the chat.
    /// </summary>
    public bool? HasHiddenMembers { get; init; }
    /// <summary>
    /// True, if messages from the chat can't be forwarded to other chats.
    /// </summary>
    public bool? HasProtectedContent { get; init; }
    /// <summary>
    /// True, if new chat members will have access to old messages.
    /// </summary>
    /// /// <remarks>
    /// The field is only available to chat administrators.
    /// </remarks>
    public bool? HasVisibleHistory { get; init; }
    /// <summary>
    /// For supergroups, name of the group sticker set.
    /// </summary>
    public string? StickerSetName { get; init; }
    /// <summary>
    /// True, if the bot can change the group sticker set.
    /// </summary>
    public bool? CanSetStickerSet { get; init; }
    /// <summary>
    /// For supergroups, the name of the group's custom emoji sticker set.
    /// </summary>
    /// <remarks>
    /// Custom emoji from this set can be used by all users and bots in the group.
    /// </remarks>
    public string? CustomEmojiStickerSetName { get; init; }
    /// <summary>
    /// Unique identifier for the linked chat, i.e. the discussion group identifier for a channel and vice versa.
    /// </summary>
    /// <remarks>
    /// For supergroups and channel chats.
    /// </remarks>
    public ChatId? LinkedChatId { get; init; }
    /// <summary>
    /// For supergroups, the location to which the supergroup is connected
    /// </summary>
    public ChatLocation? Location { get; init; }
}

public enum MessageOriginType
{
    User,
    HiddenUser,
    Chat,
    Channel
}

/// <summary>
/// Describes the origin of a message.
/// </summary>
public record MessageOrigin
{
    /// <summary>
    /// Type of the message origin.
    /// </summary>
    public required MessageOriginType Type { get; init; }
    /// <summary>
    /// Date the message was sent originally in Unix time.
    /// </summary>
    [JsonConverter(typeof(UnixDateTimeOffsetConverter))]
    public required DateTimeOffset Date { get; init; }
}

public enum MessageEntityType
{
    Mention,
    Hashtag,
    Cashtag,
    BotCommand,
    Url,
    Email,
    PhoneNumber,
    Bold,
    Italic,
    Underline,
    Strikethrough,
    Spoiler,
    Blockquote,
    ExpandableBlockquote,
    Code,
    Pre,
    TextLink,
    TextMention,
    CustomEmoji,
}

/// <summary>
/// Represents one special entity in a text message.
/// </summary>
public record MessageEntity
{
    /// <summary>
    /// Type of the entity.
    /// </summary>
    public required MessageEntityType Type { get; init; }
    /// <summary>
    /// Offset in UTF-16 code units to the start of the entity.
    /// </summary>
    public required int Offset { get; init; }
    /// <summary>
    /// Length of the entity in UTF-16 code units.
    /// </summary>
    public required int Length { get; init; }
    /// <summary>
    /// For <see cref="MessageEntityType.TextLink"/> only, URL that will be opened after user taps on the text.
    /// </summary>
    public string? Url { get; init; }
    /// <summary>
    /// For <see cref="MessageEntityType.TextMention"/> only, the mentioned user.
    /// </summary>
    public User? User { get; init; }
    /// <summary>
    /// For <see cref="MessageEntityType.Pre"/> only, the programming language of the entity text.
    /// </summary>
    public string? Language { get; init; }
    /// <summary>
    /// For <see cref="MessageEntityType.CustomEmoji"/> only, the unique identifier of the custom emoji.
    /// </summary>
    public string? CustomEmojiId { get; init; }
}

/// <summary>
/// Represents a Telegram message.
/// </summary>
public record Message
{
    /// <summary>
    /// Unique message identifier inside this chat.
    /// </summary>
    /// <remarks>
    /// In specific instances (e.g., message containing a video sent to a big chat),
    /// the server might automatically schedule a message instead of sending it immediately.
    /// In such cases, this field will be 0 and the relevant message will be unusable until it is actually sent.
    /// </remarks>
    public required MessageId MessageId { get; init; }
    /// <summary>
    /// Unique identifier of a message thread to which the message belongs.
    /// </summary>
    /// <remarks>
    /// For supergroups only.
    /// </remarks>
    public int? MessageThreadId { get; init; }
    /// <summary>
    /// Sender of the message.
    /// </summary>
    /// <remarks>
    /// May be empty for messages sent to channels.
    /// For backward compatibility, if the message was sent on behalf of a chat,
    /// the field contains a fake sender user in non-channel chats.
    /// </remarks>
    public User? From { get; init; }
    /// <summary>
    /// Sender of the message when sent on behalf of a chat.
    /// </summary>
    /// <remarks>
    /// For example, the supergroup itself for messages sent by its anonymous administrators or
    /// a linked channel for messages automatically forwarded to the channel's discussion group.
    /// For backward compatibility, if the message was sent on behalf of a chat,
    /// the field from contains a fake sender user in non-channel chats.
    /// </remarks>
    public Chat? SenderChat { get; init; }
    /// <summary>
    /// If the sender of the message boosted the chat, the number of boosts added by the user.
    /// </summary>
    public int? SenderBoostCount { get; init; }
    /// <summary>
    /// The bot that actually sent the message on behalf of the business account.
    /// </summary>
    /// <remarks>
    /// Available only for outgoing messages sent on behalf of the connected business account.
    /// </remarks>
    public User? SenderBusinessBot { get; init; }
    /// <summary>
    /// Date the message was sent in Unix time.
    /// </summary>
    [JsonConverter(typeof(UnixDateTimeOffsetConverter))]
    public required DateTimeOffset Date { get; init; }
    /// <summary>
    /// Unique identifier of the business connection from which the message was received.
    /// </summary>
    /// <remarks>
    /// If non-empty, the message belongs to a chat of the corresponding business account
    /// that is independent from any potential bot chat which might share the same identifier.
    /// </remarks>
    public string? BusinessConnectionId { get; init; }
    /// <summary>
    /// Chat the message belongs to.
    /// </summary>
    public required Chat Chat { get; init; }
    /// <summary>
    /// Information about the original message for forwarded messages.
    /// </summary>
    public MessageOrigin? ForwardOrigin { get; init; }
    /// <summary>
    /// rue, if the message is sent to a forum topic.
    /// </summary>
    public bool? IsTopicMessage { get; init; }
    /// <summary>
    /// True, if the message is a channel post that
    /// was automatically forwarded to the connected discussion group.
    /// </summary>
    public bool? IsAutomaticForward { get; init; }
    /// <summary>
    /// For replies in the same chat and message thread, the original message.
    /// </summary>
    /// <remarks>
    /// Note that the Message object in this field will not contain
    /// further <see cref="ReplyToMessage"/> fields even if it itself is a reply.
    /// </remarks>
    public Message? ReplyToMessage { get; init; }
    //public ExternalReplyInfo? ExternalReply { get; init; }
    //public TextQuote? Quote { get; init; }
    //public Story? ReplyToStory { get; init; }
    /// <summary>
    /// Bot through which the message was sent.
    /// </summary>
    public User? ViaBot { get; init; }
    /// <summary>
    /// Date the message was last edited in Unix time.
    /// </summary>
    [JsonConverter(typeof(UnixDateTimeOffsetConverter))]
    public DateTimeOffset? EditDate { get; init; }
    /// <summary>
    /// True, if the message can't be forwarded.
    /// </summary>
    public bool? HasProtectedContent { get; init; }
    /// <summary>
    /// True, if the message was sent by an implicit action,
    /// for example, as an away or a greeting business message,
    /// or as a scheduled message.
    /// </summary>
    public bool? IsFromOffline { get; init; }
    /// <summary>
    /// The unique identifier of a media message group this message belongs to.
    /// </summary>
    public string? MediaGroupId { get; init; }
    /// <summary>
    /// Signature of the post author for messages in channels,
    /// or the custom title of an anonymous group administrator.
    /// </summary>
    public string? AuthorSignature { get; init; }
    /// <summary>
    /// For text messages, the actual UTF-8 text of the message.
    /// </summary>
    public string Text { get; init; } = string.Empty;
    /// <summary>
    /// For text messages, special entities like usernames, URLs, bot commands, etc. that appear in the text.
    /// </summary>
    public MessageEntity[] Entities { get; init; } = [];
    //public LinkPreviewOptions? LinkPreviewOptions { get; init; }
    /// <summary>
    /// Unique identifier of the message effect added to the message.
    /// </summary>
    public string? EffectId { get; init; }
}

[Flags]
public enum UpdateType
{
    Default = 0 << 0,
    Message = 1 << 0,
    EditedMessage = 1 << 1,
    ChannelPost = 1 << 2,
    EditedChannelPost = 1 << 3,
    BusinessConnection = 1 << 4,
    BusinessMessage = 1 << 5,
    EditedBusinessMessage = 1 << 6,
    DeletedBusinessMessages = 1 << 7,
    MessageReaction = 1 << 8,
    MessageReactionCount = 1 << 9,
    InlineQuery = 1 << 10,
    ChosenInlineResult = 1 << 11,
    CallbackQuery = 1 << 12,
    ShippingQuery = 1 << 13,
    PreCheckoutQuery = 1 << 14,
    PurchasedPaidMedia = 1 << 15,
    Poll = 1 << 16,
    PollAnswer = 1 << 17,
    MyChatMember = 1 << 18,
    ChatMember = 1 << 19,
    ChatJoinRequest = 1 << 20,
    ChatBoost = 1 << 21,
    RemovedChatBoost = 1 << 22,
}

public record Update
{
    public required int UpdateId { get; init; }
    public Message? Message { get; init; }
    public Message? EditedMessage { get; init; }
    public Message? ChannelPost { get; init; }
    public Message? EditedChannelPost { get; init; }
    //public BusinessConnection? BusinessConnection { get; init; }
    //public Message? BusinessMessage { get; init; }
    //public Message? EditedBusinessMessage { get; init; }
    //public Message[]? DeletedBusinessMessages { get; init; }
    //public MessageReaction? MessageReaction { get; init; }
    //public MessageReactionCount? MessageReactionCount { get; init; }
    //public InlineQuery? InlineQuery { get; init; }
    //public ChosenInlineResult? ChosenInlineResult { get; init; }
    public CallbackQuery? CallbackQuery { get; init; }
    //public ShippingQuery? ShippingQuery { get; init; }
    //public PreCheckoutQuery? PreCheckoutQuery { get; init; }
    //public PurchasedPaidMedia? PurchasedPaidMedia { get; init; }
    //public Poll? Poll { get; init; }
    //public PollAnswer? PollAnswer { get; init; }
    //public ChatMemberUpdated? MyChatMember { get; init; }
    //public ChatMemberUpdated? ChatMember { get; init; }
    //public ChatJoinRequest? ChatJoinRequest { get; init; }
    //public ChatBoostUpdated? ChatBoost { get; init; }
    //public ChatBoostRemoved? RemovedChatBoost { get; init; }
}

public record CallbackQuery
{
    public required string Id { get; init; }
    public required User From { get; init; }
    public Message? Message { get; init; }
    public string? InlineMessageId { get; init; }
    public string? ChatInstance { get; init; }
    public string? Data { get; init; }
    public string? GameShortName { get; init; }
}
