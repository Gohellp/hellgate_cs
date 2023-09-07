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

        public DiscordStartupService(
            IConfiguration config,
            DiscordSocketClient discord,
            ILogger<DiscordSocketClient> logger)
        {
            _discord = discord;
            _config = config;
            _logger = logger;

            _discord.Log += msg => LogHelper.OnLogAsync(_logger, msg);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {

            await _discord.LoginAsync(TokenType.Bot, _config["hellgateToken"]);
            await _discord.StartAsync();
            await _discord.SetGameAsync("C# v1.3.0, bitches!");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _discord.LogoutAsync();
            await _discord.StopAsync();
        }

    }
}
