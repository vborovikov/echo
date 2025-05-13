namespace Echo;

using Microsoft.Extensions.Logging;

static class EventIds
{
    public static readonly EventId BotStarted = new(600, nameof(BotStarted));
    public static readonly EventId BotStopped = new(601, nameof(BotStopped));
    public static readonly EventId BotFailed = new(602, nameof(BotFailed));
    public static readonly EventId BotExecuting = new(603, nameof(BotExecuting));
    public static readonly EventId BotWaiting = new(604, nameof(BotWaiting));
    public static readonly EventId BotConfused = new(605, nameof(BotConfused));
}
