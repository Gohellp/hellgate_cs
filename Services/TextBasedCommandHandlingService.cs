using Discord;
using Discord.Commands;
using Discord.WebSocket;
using hellgate.Contexts;
using hellgate.Models;
using Lavalink4NET;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace hellgate.Services
{
    internal class TextBasedCommandHandlingService : IHostedService
    {
        private readonly DiscordSocketClient _discord;
        private readonly IConfiguration _config;
        private readonly IServiceProvider _services;
        private readonly CommandService _commands;
        private readonly IAudioService _audioService;

        private SocketGuild? _nithGuild;
        private ITextChannel? _botChannel;

        public TextBasedCommandHandlingService(
            DiscordSocketClient discord,
            CommandService commands,
            IConfiguration config,
            IServiceProvider services,
            IAudioService audioService)
        {
            _discord = discord;
            _config = config;
            _services = services;
            _commands = commands;
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _discord.Ready += OnReady;
            _discord.MessageReceived += HandleCommandAsync;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _audioService.Dispose();

            await Task.CompletedTask;
        }

        private async Task OnReady()
        {
            _nithGuild = _discord.GetGuild(Convert.ToUInt64(_config["nithGuildId"]));
            _botChannel = _nithGuild.GetTextChannel(Convert.ToUInt64(_config["botChannelId"]));
            //Place to HandleCommandAsync() and change to _botChannel = context.Guild.GetTextChannel(guildSettings.BotChannelId)
            //And add check for null

            await _audioService.InitializeAsync();
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_discord, message);
            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('+', ref argPos) ||
                message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: _services);
        }
    }
}
