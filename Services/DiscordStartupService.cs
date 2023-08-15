using Discord;
using Discord.WebSocket;
using hellgate.Contexts;
using hellgate.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace hellgate.Services
{
    public class DiscordStartupService : IHostedService
    {
        private readonly IConfiguration _config;
        private readonly DiscordSocketClient _discord;
        private readonly ILogger<DiscordSocketClient> _logger;
        private readonly GuildsSettingsContext _guildsSettingsContext;


        private GuildSettings? _globalSettings;

        public DiscordStartupService(
            IConfiguration config,
            DiscordSocketClient discord,
            ILogger<DiscordSocketClient> logger,
            GuildsSettingsContext guildsSettingsContext)
        {
            _discord = discord;
            _config = config;
            _logger = logger;
            _guildsSettingsContext = guildsSettingsContext;

            _discord.Log += msg => LogHelper.OnLogAsync(_logger, msg);
            _discord.JoinedGuild += OnJoinedGuild;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _globalSettings = _guildsSettingsContext.GuildsSettings.Find("0");
            if (_globalSettings == null)
            {
                _globalSettings = new GuildSettings()
                {
                    ServerId = "0"
                };
                _guildsSettingsContext.Add(_globalSettings);
                _guildsSettingsContext.SaveChanges();
            }

            await _discord.LoginAsync(TokenType.Bot, _config["hellgateToken"]);
            await _discord.StartAsync();
            await _discord.SetGameAsync("C# v1.1.0, bitches!");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _discord.LogoutAsync();
            await _discord.StopAsync();
        }

        public async Task OnJoinedGuild(SocketGuild guild)
        {
            GuildSettings? newGuild = await _guildsSettingsContext.GuildsSettings.FindAsync(guild.Id.ToString());

            if (newGuild == null)
            {
                newGuild = _globalSettings!;
                newGuild.ServerId = guild.Id.ToString();
                _guildsSettingsContext.GuildsSettings.Add(newGuild);
                _guildsSettingsContext.SaveChanges();
            }
        }

    }
}
