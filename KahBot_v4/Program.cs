using DataStore.EF.Data;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using KahBot_v4.Controllers;
using KahBot_v4.Helpers;
using KahBot_v4.Middlewares;
using KahBot_v4.Modules;
using KahBot_v4.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic;
using System;
using System.ComponentModel.Design;
using System.Reflection;
using System.Threading.Channels;
using System.Windows.Input;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static System.Runtime.InteropServices.JavaScript.JSType;


ServiceProvider _serviceProvider = CreateServices();
DiscordSocketClient _client = _serviceProvider.GetRequiredService<DiscordSocketClient>(); ;
InteractionService _interactionService = _serviceProvider.GetRequiredService<InteractionService>();
IConfigurationRoot _config = _serviceProvider.GetRequiredService<IConfigurationRoot>();
LoggingService _logger = _serviceProvider.GetRequiredService<LoggingService>();
CountDownTimerHelper _countDownTimerHelper = _serviceProvider.GetRequiredService<CountDownTimerHelper>();

Main();
void Main() => RunAsync(args).GetAwaiter().GetResult();

async Task RunAsync(string[] args)
{
    _client.Ready += _serviceProvider.GetRequiredService<ServerGeneralHelper>().InitializeModel;
    await _serviceProvider.GetRequiredService<CommandHandler>().InstallCommandsAsync();
    await _serviceProvider.GetRequiredService<InteractionHandler>().InitializeAsync();
    _client.Ready += ReadyAsync;
    _client.InteractionCreated += _serviceProvider.GetRequiredService<InteractionMiddlewares>().HandleInteraction;

    await _client.LoginAsync(TokenType.Bot, _config.GetSection("Keys").GetSection("BotKey").Value);
    await _client.StartAsync();
    
    await Task.Delay(Timeout.Infinite);

}

async Task BotReady()
{
    ServerGeneralHelper serverGeneralHelper = _serviceProvider.GetService<ServerGeneralHelper>()!;

    string autoCheckRequirementsValue = _config.GetSection("AutoCheckRequirements").Value ?? "false";
    bool autoCheckRequirements = !string.IsNullOrEmpty(autoCheckRequirementsValue) && Convert.ToBoolean(autoCheckRequirementsValue);

    if (!autoCheckRequirements)
        return;

    foreach (var guild in _client.Guilds)
    {
        SocketTextChannel generalChannel = guild.DefaultChannel as SocketTextChannel;

        if (!serverGeneralHelper.CheckRequirements())
        {
            await generalChannel.SendMessageAsync("Bot is now online!");
        }
        else
        {
            await generalChannel.SendMessageAsync(_serviceProvider.GetService<ResourceHelper>().GetString(ResourceFiles.GeneralMessages, GeneralMessages.CheckRequirements.ToString()));
        }
    }
}

#region [ DI ]

static ServiceProvider CreateServices()
{
    // Create configuration
    var appsettings = new ConfigurationBuilder()
               .SetBasePath(AppContext.BaseDirectory)
               .AddJsonFile(path: $"appsettings.{(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development")}.json").Build();

    var discordBotConfig = new DiscordSocketConfig()
    {
        GatewayIntents = GatewayIntents.All,
        LogLevel = LogSeverity.Verbose,
        AlwaysDownloadUsers = true,
        MessageCacheSize = 200
    };

    var commandService = new CommandService(new CommandServiceConfig
    {
        LogLevel = LogSeverity.Debug,
        CaseSensitiveCommands = false,
    });

    var collection = new ServiceCollection()
        .AddSingleton(appsettings)
        .AddSingleton<DiscordSocketClient>()
        .AddTransient<CounterController>()
        .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
        .AddSingleton<InteractionHandler>()
        .AddSingleton(commandService)
        .AddSingleton<CommandHandler>()
        .AddSingleton(discordBotConfig)
        .AddSingleton<LoggingService>()
        .AddSingleton<CountDownTimerHelper>()
        .AddSingleton<ServerGeneralHelper>()
        .AddSingleton<System.Reflection.Assembly>(System.Reflection.Assembly.GetExecutingAssembly())
        .AddTransient<ResourceHelper>()
        .AddTransient<InteractionMiddlewares>()
        .AddDbContext<KahBotDbContext>(options =>
        {
            options.UseSqlServer(appsettings.GetConnectionString("DefaultConnection"));
        });

    return collection.BuildServiceProvider();
}

async Task ReadyAsync()
{
    _interactionService = new InteractionService(_client);
    await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
    await _interactionService.RegisterCommandsGloballyAsync();
    await _countDownTimerHelper.ContinueUnfinishedCountdownsAsync(_client);
    await _client.SetActivityAsync(new Game("over KahPeler", ActivityType.Watching, ActivityProperties.PartyPrivacyVoiceChannel));
    await BotReady();
}

#endregion