using Discord.WebSocket;
using hellgate.Contexts;
using hellgate.Models;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace hellgate.Services
{
    internal class AutoNewsThreadService : IHostedService
    {
        private readonly DiscordSocketClient _discord;
        private readonly GuildsSettingsContext _guildsSettingsContext;


        private GuildSettings? _globalSettings;

        public AutoNewsThreadService(
            DiscordSocketClient discord,
            GuildsSettingsContext guildsSettingsContext) 
        {
            _discord = discord;
            _guildsSettingsContext = guildsSettingsContext;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _globalSettings = _guildsSettingsContext.GuildsSettings.Find("0");
            if(_globalSettings == null)
            {
                _globalSettings = new GuildSettings()
                {
                    ServerId = "0"
                };
                _guildsSettingsContext.Add(_globalSettings);
                _guildsSettingsContext.SaveChanges();
            }
            _discord.MessageReceived += CreateThreatOnNews;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        //Add Embed Support
        private async Task CreateThreatOnNews(SocketMessage message)
        {
            GuildSettings? guildSettings;
            SocketTextChannel? channel;
            if (message.Channel is SocketGuildChannel guildChannel)
            {
                guildSettings = _guildsSettingsContext.GuildsSettings.Find(guildChannel.Guild.Id.ToString());
                channel = (SocketTextChannel)guildChannel;
            }
            else
                return;

            if(guildSettings == null)
            {
                guildSettings = _globalSettings!;
                guildSettings.ServerId = ((SocketGuildChannel)message.Channel).Guild.Id.ToString();
                _guildsSettingsContext.GuildsSettings.Add(guildSettings);
                _guildsSettingsContext.SaveChanges();
            }

            if(message.Channel.Id.ToString() == guildSettings.NewsChannelId)
            {
                string threadName = "";

                if (message.Content.Split("\n").First().Length > 100)
                {
                    threadName = Regex.Match(message.Content, @"^(?:[\p{L}+:/\d.]){1,100}\.", RegexOptions.IgnoreCase|RegexOptions.Singleline).Value;
                }
                else
                {
                    threadName = message.Content.Split("\n").First();
                }
                await channel.CreateThreadAsync(threadName);
            }
        }
    }
}
