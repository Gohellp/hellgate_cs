using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using hellgate.Contexts;
using hellgate.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureServices(services =>
    {
        DiscordSocketClient _client = new DiscordSocketClient(new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.Guilds
            | GatewayIntents.GuildVoiceStates
            | GatewayIntents.GuildMembers
            | GatewayIntents.GuildMessages
        });
        services.AddSingleton(_client);       // Add the discord client to services
        services.AddSingleton<InteractionService>();        // Add the interaction service to services
        services.AddHostedService<InteractionHandlingService>();    // Add the slash command handler
        services.AddHostedService<DiscordStartupService>();         // Add the discord startup service
        services.AddHostedService<VoiceStateHandlingService>();
    })
    .Build();
using (var db = new VoiceContext())
{
    db.Database.Migrate();
}

await host.RunAsync();