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
        private readonly GuildsSettingsContext _guildsSettingsContext;
        private readonly GuildSettings _globalSettings;

        private SocketGuild? _nithGuild;
        private ITextChannel? _botChannel;

        public TextBasedCommandHandlingService(
            DiscordSocketClient discord,
            CommandService commands,
            IConfiguration config,
            IServiceProvider services,
            IAudioService audioService,
            GuildsSettingsContext guildsSettingsContext,
            GuildSettings globalSettings)
        {
            _discord = discord;
            _config = config;
            _services = services;
            _commands = commands;
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _guildsSettingsContext = guildsSettingsContext;
            _globalSettings = globalSettings;
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

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('+', ref argPos) ||
                message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_discord, message);

            GuildSettings guildSettings = _guildsSettingsContext.GuildsSettings.FirstOrDefault(gs => gs.ServerId == context.Guild.Id.ToString()) ?? _globalSettings;
            List<UserSetting>? globalBans = _globalSettings.Users.Where(us => (us.UserId == context.User.Id.ToString() && us.AllowUseCommands == false)).ToList();
            List<UserSetting>? guildBans = guildSettings.Users.Where(us => (us.UserId == context.User.Id.ToString() && us.AllowUseCommands == false)).ToList();

            if (guildSettings.OnlyInBotChannel && context.Channel.Id != _botChannel!.Id)
            {
                await context.Message.DeleteAsync();
                return;
            }
            if(globalBans.Count != 0 || guildBans.Count != 0)
            {
                EmbedBuilder embedBuilder = new EmbedBuilder()
                {
                    Title="Information",
                    Color=new Color(0,255,255),
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = _discord.CurrentUser.Username,
                        IconUrl = _discord.CurrentUser.GetAvatarUrl()
                    },
                    Description = "Sorry, you are not allowed to use the commands of this bot 😉"
                };
                await message.ReplyAsync(embed:embedBuilder.Build());
                return;
            }
            

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: _services);
        }
    }
}
