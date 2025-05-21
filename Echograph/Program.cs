namespace Echo;

using System;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

record ProgramOptions
{
    public LogLevel LogLevel { get; init; } = LogLevel.Information;
}

class Program
{
    static Program()
    {
        if (Environment.UserInteractive)
        {
            Console.InputEncoding = Encoding.Default;
            Console.OutputEncoding = Encoding.Default;
        }
    }

    static async Task<int> Main(string[] args)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                e.Cancel = true;
                cts.Cancel();
            }
        };

        try
        {
            await using var sp = ConfigureApp(args);
            var bot = sp.GetRequiredService<EchoBot>();

            Console.Error.WriteLine("Bot started chatting.");
            await bot.ChatAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Bot stopped chatting.");
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            Console.Error.WriteLine($"Error: {x.Message}");
            return 2;
        }

        return 0;
    }

    private static ServiceProvider ConfigureApp(string[] args, Action<ProgramOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .AddCommandLine(args)
            .Build();

        // options
        var optionsBuilder = services.AddOptions<ProgramOptions>().Bind(configuration);
        if (configureOptions is not null) optionsBuilder.Configure(configureOptions);

        // logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.Services.AddSingleton<IConfigureOptions<LoggerFilterOptions>>(sp =>
            {
                var appOptions = sp.GetRequiredService<IOptions<ProgramOptions>>().Value;
                return new ConfigureOptions<LoggerFilterOptions>(options => options.MinLevel = appOptions.LogLevel);
            });
#if DEBUG
            builder.AddDebug();
#endif
        });

        // bot API client
        var botToken = configuration.GetConnectionString("DefaultConnection") ??
            throw new InvalidOperationException("Telegram authentication token 'DefaultConnection' not found.");
        services.AddHttpClient<IBot, TelegramBot>((http, sp) =>
        {
            http.BaseAddress = new Uri("https://api.telegram.org/");
            var logging = sp.GetRequiredService<ILoggerFactory>();
            return new TelegramBot(botToken, http, logging.CreateLogger<TelegramBot>());
        });

        services.AddSingleton<EchoBot>(sp => new(sp.GetRequiredService<IBot>(), sp.GetRequiredService<ILogger<EchoBot>>())
        {
            InactivityPeriod = TimeSpan.FromMinutes(1)
        });

        return services.BuildServiceProvider();
    }
}
