namespace Echo.Telegram;

using System;
using System.Diagnostics.CodeAnalysis;

public static class MessageExtensions
{
    public static string GetValue(this MessageEntity entity, Message message)
    {
        if (string.IsNullOrWhiteSpace(message.Text) ||
            entity.Offset < 0 || entity.Offset > message.Text.Length || entity.Length < 0 || entity.Length > message.Text.Length ||
            entity.Offset > entity.Length || (entity.Length - entity.Offset) > message.Text.Length)
        {
            return string.Empty;
        }

        return message.Text.Substring(entity.Offset, entity.Length);
    }

    public static bool HasBotCommand(this Message message)
    {
        return Array.Exists(message.Entities, e => e.Type == MessageEntityType.BotCommand) ||
            (message.Text.StartsWith('/') && message.Text.IndexOf(' ') is < 0 or > 2);
    }

    public static bool TryGetBotCommand(this Message message, [NotNullWhen(true)] out string? command)
    {
        var commandEntity = Array.Find(message.Entities, e => e.Type == MessageEntityType.BotCommand);
        if (commandEntity != null)
        {
            command = commandEntity.GetValue(message).ToLowerInvariant();
            return true;
        }

        if (message.Text.StartsWith('/'))
        {
            var spaceIndex = message.Text.IndexOf(' ');
            command = (spaceIndex > 1 ? message.Text[..spaceIndex] : message.Text).ToLowerInvariant();
            return true;
        }

        command = default;
        return false;
    }
}
