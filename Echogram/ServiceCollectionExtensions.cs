namespace Echo;

using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Configuration options for the Telegram Bot client.
/// </summary>
public sealed class TelegramBotOptions
{
    /// <summary>
    /// Gets or sets the base endpoint URI for Telegram Bot API requests.
    /// </summary>
    /// <value>Defaults to <c>"https://api.telegram.org/"</c>.</value>
    public required string ApiEndpoint { get; set; } = "https://api.telegram.org/";

    /// <summary>
    /// Gets or sets an optional proxy server address for outbound HTTP requests.
    /// </summary>
    /// <value>
    /// A valid absolute URI (e.g., <c>"http://proxy.local:8080"</c>), or <see langword="null"/>
    /// to use the system default proxy settings.
    /// </value>
    public string? ProxyAddress { get; set; }
}

/// <summary>
/// Extension methods for registering Telegram Bot services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds TelegramBot services with a bot token and default options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the Telegram Bot services will be added.</param>
    /// <param name="botToken">The authentication token for the Telegram Bot, obtained from <c>@BotFather</c>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance, enabling fluent chaining of service registrations.</returns>
    public static IServiceCollection AddTelegramBot(this IServiceCollection services, string botToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botToken);

        services.AddOptions<TelegramBotOptions>();
        services.AddTelegramClient(botToken);

        return services;
    }

    /// <summary>
    /// Adds TelegramBot services with a bot token and configuration action.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the Telegram Bot services will be added.</param>
    /// <param name="botToken">The authentication token for the Telegram Bot, obtained from <c>@BotFather</c>.</param>
    /// <param name="configureOptions">Action to configure TelegramBotOptions.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance, enabling fluent chaining of service registrations.</returns>
    public static IServiceCollection AddTelegramBot(
        this IServiceCollection services,
        string botToken,
        Action<TelegramBotOptions> configureOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botToken);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<TelegramBotOptions>()
            .Configure(configureOptions);

        services.AddTelegramClient(botToken);
        return services;
    }

    /// <summary>
    /// Adds TelegramBot services with a bot token and IConfiguration binding.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the Telegram Bot services will be added.</param>
    /// <param name="botToken">The authentication token for the Telegram Bot, obtained from <c>@BotFather</c>.</param>
    /// <param name="configuration">Configuration section containing TelegramBotOptions.</param>
    /// <param name="configureOptions">Optional action to further configure options after binding.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance, enabling fluent chaining of service registrations.</returns>
    public static IServiceCollection AddTelegramBot(
        this IServiceCollection services,
        string botToken,
        IConfiguration configuration,
        Action<TelegramBotOptions>? configureOptions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botToken);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<TelegramBotOptions>()
            .Bind(configuration);

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        services.AddTelegramClient(botToken);
        return services;
    }

    /// <summary>
    /// Adds TelegramBot services with a bot token and pre-configured options instance.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the Telegram Bot services will be added.</param>
    /// <param name="botToken">The authentication token for the Telegram Bot, obtained from <c>@BotFather</c>.</param>
    /// <param name="userOptions">Pre-configured TelegramBotOptions instance.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance, enabling fluent chaining of service registrations.</returns>
    public static IServiceCollection AddTelegramBot(
        this IServiceCollection services,
        string botToken,
        TelegramBotOptions userOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botToken);
        ArgumentNullException.ThrowIfNull(userOptions);

        services.AddOptions<TelegramBotOptions>()
            .Configure(options =>
            {
                options.ApiEndpoint = userOptions.ApiEndpoint;
                options.ProxyAddress = userOptions.ProxyAddress;
            });

        services.AddTelegramClient(botToken);
        return services;
    }

    /// <summary>
    /// Configures TelegramBotOptions separately, returning OptionsBuilder for further chaining.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the Telegram Bot services will be added.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance, enabling fluent chaining of service registrations.</returns>
    public static OptionsBuilder<TelegramBotOptions> ConfigureTelegramBotOptions(
        this IServiceCollection services)
    {
        return services.AddOptions<TelegramBotOptions>();
    }

    /// <summary>
    /// Configures TelegramBotOptions with an action, returning OptionsBuilder for further chaining.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the Telegram Bot services will be added.</param>
    /// <param name="configureOptions">Action to configure TelegramBotOptions.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance, enabling fluent chaining of service registrations.</returns>
    public static OptionsBuilder<TelegramBotOptions> ConfigureTelegramBotOptions(
        this IServiceCollection services,
        Action<TelegramBotOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        return services.AddOptions<TelegramBotOptions>()
            .Configure(configureOptions);
    }

    /// <summary>
    /// Binds TelegramBotOptions from IConfiguration, returning OptionsBuilder for further chaining.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the Telegram Bot services will be added.</param>
    /// <param name="configuration">Configuration section containing TelegramBotOptions.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance, enabling fluent chaining of service registrations.</returns>
    public static OptionsBuilder<TelegramBotOptions> ConfigureTelegramBotOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return services.AddOptions<TelegramBotOptions>()
            .Bind(configuration);
    }

    private static IServiceCollection AddTelegramClient(this IServiceCollection services, string botToken)
    {
        services.AddHttpClient<IBot, TelegramBot>((http, sp) =>
        {
            var options = sp.GetRequiredService<IOptions<TelegramBotOptions>>();

            http.BaseAddress = new Uri(options.Value.ApiEndpoint);
            var logging = sp.GetRequiredService<ILoggerFactory>();
            return new TelegramBot(botToken, http, logging.CreateLogger<TelegramBot>());
        })
        .ConfigurePrimaryHttpMessageHandler((handler, sp) =>
        {
            var options = sp.GetRequiredService<IOptions<TelegramBotOptions>>();
            if (Uri.TryCreate(options.Value.ProxyAddress, UriKind.Absolute, out var proxyAddress))
            {
                var proxy = new WebProxy(proxyAddress, BypassOnLocal: true)
                {
                    UseDefaultCredentials = false,
                };
                if (handler is SocketsHttpHandler socketsHttpHandler)
                {
                    socketsHttpHandler.UseProxy = true;
                    socketsHttpHandler.Proxy = proxy;
                    socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
                }
                else if (handler is HttpClientHandler { SupportsProxy: true } httpClientHandler)
                {
                    httpClientHandler.UseProxy = true;
                    httpClientHandler.Proxy = proxy;
                    httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }
            }
        });

        return services;
    }
}
