namespace Echo;

using System;
using Echo.Telegram;

public class TelegramBotException : Exception
{
    public TelegramBotException() : this(default) { }

    public TelegramBotException(string? message) : base(message)
    {
        this.Method = string.Empty;
    }

    public TelegramBotException(string? message, Exception? innerException) : base(message, innerException)
    {
        this.Method = string.Empty;
    }

    public TelegramBotException(string method, ApiResponse? response) : this(method, response, response?.Description) { }

    public TelegramBotException(string method, ApiResponse? response, string? message) : this(method, response, message, default) { }

    public TelegramBotException(string method, ApiResponse? response, string? message, Exception? innerException) : base(message, innerException)
    {
        this.Method = method;

        if (response is not null)
        {
            this.Data.Add(nameof(ApiResponse.Description), response.Description ?? string.Empty);
            this.Data.Add(nameof(ApiResponse.ErrorCode), response.ErrorCode);
            if (response.Parameters is not null)
            {
                this.Data.Add(nameof(ResponseParameters.RetryAfter), response.Parameters.RetryAfter);
                this.Data.Add(nameof(ResponseParameters.MigrateToChatId), response.Parameters.MigrateToChatId);
            }
        }
    }

    public string Method { get; }
}
