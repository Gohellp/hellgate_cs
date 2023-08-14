using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Interactions;
using hellgate.Models;
using hellgate.Contexts;
using hellgate.Services;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.MemoryCache;
using Lavalink4NET.Logging.Microsoft;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddEnvironmentVariables()
        /*.AddJsonFile("./config.json")*/;
    })  
    .ConfigureServices(services =>
    {
        using (var db = new GuildsSettingsContext())
        {
            db.Database.Migrate();

            GuildSettings? GlobalSettings = db.GuildsSettings.FirstOrDefault(g => g.ServerId == "0");
            if (GlobalSettings == null)
            {
                GlobalSettings = new GuildSettings() { ServerId = "0" };
                db.GuildsSettings.Add(GlobalSettings);
                db.SaveChanges();
            }
            services.AddSingleton(GlobalSettings);
        }

        services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.Guilds
            | GatewayIntents.GuildVoiceStates
            | GatewayIntents.GuildMembers
            | GatewayIntents.GuildMessages
            | GatewayIntents.MessageContent
        }));       // Add the discord client to services
        services.AddSingleton<InteractionService>();        // Add the interaction service to services
        services.AddSingleton<CommandService>();
        services.AddHostedService<InteractionHandlingService>();    // Add the slash command handler
        services.AddHostedService<DiscordStartupService>();         // Add the discord startup service
        services.AddHostedService<VoiceStateHandlingService>();

        //MisicPart
        services.AddHostedService<TextBasedCommandHandlingService>();

        services.AddSingleton<IAudioService, LavalinkNode>();
        services.AddSingleton<IDiscordClientWrapper, DiscordClientWrapper>();
        services.AddMicrosoftExtensionsLavalinkLogging();

        services.AddSingleton(new LavalinkNodeOptions
        {
            RestUri = "http://127.0.0.1:2333/",
            WebSocketUri = "ws://127.0.0.1:2333/",
            Password = "youshallnotpass"
        });
        services.AddSingleton<ILavalinkCache, LavalinkCache>();

        services.AddDbContext<VoiceContext>();
        services.AddDbContext<GuildsSettingsContext>();

    })
    .Build();
using (var db = new VoiceContext())
{
    db.Database.Migrate();
}

await host.RunAsync();