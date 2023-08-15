using Discord;
using Discord.Interactions;
using hellgate.Contexts;
using hellgate.Models;
using System.Data.Entity;

namespace hellgate.Modules
{
    [Group("settings","Change settings params")]
    public class SettingsModule : InteractionModuleBase
    {

        [EnabledInDm(false)]
        [RequireContext(ContextType.Guild)]
        [Group("guild", "Change guild settings params")]
        public class GuildSettingsModule : InteractionModuleBase
        {
            private readonly GuildsSettingsContext _guildsSettingsContext;

            public GuildSettingsModule(GuildsSettingsContext guildsSettingsContext)
            {
                _guildsSettingsContext = guildsSettingsContext;
            }

            [RequireUserPermission(GuildPermission.ManageRoles)]
            [SlashCommand("set_dj","Sets the DJ's role")]
            public async Task SetDJAsync(IRole djRole)
            {
                GuildSettings guildSettings = GuildCheckAndGet();

                guildSettings.DJRoleId = djRole.Id.ToString();
                _guildsSettingsContext.SaveChanges();
                
                await RespondAsync(embed: SuccessEmbed($"DJRole value changed to {djRole.Name} {Format.Spoiler(djRole.Id.ToString())}", Context), ephemeral:true);
            }

            [RequireUserPermission(GuildPermission.ManageChannels)]
            [SlashCommand("set_volume", "Change default volume")]
            public async Task SetGlobalVolumeAsync(int volume)
            {
                if (volume is > 100 or < 0)
                {
                    await RespondAsync(embed: TrowError("Value out of bounds", "GuildSettingsModule/GlobalSettings/SetGlobalVolumeAsync/OutOfBounds", Context), ephemeral:true);
                    return;
                }

                GuildSettings guildSettings = GuildCheckAndGet();
                
                guildSettings.PlayerVolume = volume;

                _guildsSettingsContext.SaveChanges();

                await RespondAsync(embed: SuccessEmbed("Volume sets to " + volume, Context), ephemeral:true);
            }

            [RequireUserPermission(GuildPermission.ManageGuild)]
            [SlashCommand("set_prefix","Change prefix")]
            public async Task SetPrefixAsync(string prefix)
            {
                GuildSettings guild = GuildCheckAndGet();

                guild.TextCommandPrefix = prefix;
                _guildsSettingsContext.SaveChanges();

                await RespondAsync(embed: SuccessEmbed("Prefix changed to "+prefix,Context));
            }

            [RequireUserPermission(GuildPermission.ManageChannels)]
            [SlashCommand("set_botchannel", "Sets the bots channel")]
            public async Task SetBotChannelAsync(ITextChannel channel)
            {
                GuildSettings guild = GuildCheckAndGet();

                OverwritePermissions? perms = channel.GetPermissionOverwrite(Context.Client.CurrentUser);

                if (perms == null)
                {
                    perms = channel.GetPermissionOverwrite(channel.Guild.EveryoneRole);
                }
                if(perms!=null && perms.Value.ViewChannel == PermValue.Deny)
                {
                    await RespondAsync(embed:TrowError("I cant read this channel", "SettingsModule/GuildSettingsModule/SetBotChannelAsync", Context));
                    return;
                }

                guild.BotChannelId = channel.Id.ToString();
                _guildsSettingsContext.SaveChanges();

                await RespondAsync(embed:SuccessEmbed("BotChannelId setts to "+guild.BotChannelId,Context));
            }

            [RequireUserPermission(GuildPermission.ManageChannels)]
            [SlashCommand("set_newschannel", "Sets the news channel")]
            public async Task SetNewsChannelAsync(ITextChannel channel)
            {
                GuildSettings guildSettings = GuildCheckAndGet();
                OverwritePermissions? perms = channel.GetPermissionOverwrite(Context.Client.CurrentUser);

                if (perms == null)
                {
                    perms = channel.GetPermissionOverwrite(channel.Guild.EveryoneRole);
                }
                if (perms != null && perms.Value.ViewChannel == PermValue.Deny)
                {
                    await RespondAsync(embed: TrowError("I cant read this channel", "SettingsModule/GuildSettingsModule/SetBotChannelAsync", Context));
                    return;
                }

                guildSettings.NewsChannelId = channel.Id.ToString();
                _guildsSettingsContext.SaveChanges();

                await RespondAsync(embed: SuccessEmbed("NewsChannelId setts to " + guildSettings.BotChannelId, Context));
            }

            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [SlashCommand("change_loopqueue","")]
            public async Task ChangeLoopQueueAsync()
            {
                GuildSettings guildSettings = GuildCheckAndGet();

                guildSettings.DefaultLoopQueue = !guildSettings.DefaultLoopQueue;

                _guildsSettingsContext.SaveChanges();

                await RespondAsync(embed: SuccessEmbed($"You sets DefaultLoopQueue to {guildSettings.DefaultLoopQueue}", Context), ephemeral: true);
            }

            [RequireUserPermission(GuildPermission.ManageGuild)]
            [SlashCommand("change_command_perm", "Change the permission to use the command")]
            public async Task ChangeCommandPermissionAsync(IUser? user = null, string? userId = null)
            {
                string? _userId = null;

                if (userId != null && user == null)
                    _userId = userId;
                else if (user != null && userId == null)
                    _userId = user.Id.ToString();
                else if (userId == null && user == null)
                {
                    await RespondAsync(embed: TrowError("You must specify one of the values", "SettingsModule/GlobalSettings/ChangeCommandPermissionAsync", Context));
                    return;
                }
                else
                {
                    await RespondAsync(embed: TrowError("You must specify ONE of the values", "SettingsModule/GlobalSettings/ChangeCommandPermissionAsync", Context));
                    return;
                }

                GuildSettings guild = GuildCheckAndGet();

                UserSetting? _user = guild.Users.FirstOrDefault(us => us.UserId == _userId);
                if (_user == null)
                {
                    _user = new UserSetting() { UserId = _userId };
                    guild.Users.Add(_user);
                }

                _user.AllowUseCommands = !_user.AllowUseCommands;
                _guildsSettingsContext.SaveChanges();

                await RespondAsync(embed: SuccessEmbed($"You sets AllowUseCommands to {_user.AllowUseCommands} for {MentionUtils.MentionUser(Convert.ToUInt64(_userId))}", Context), ephemeral: true);
            }


            private GuildSettings GuildCheckAndGet()
            {
                GuildSettings? _guild = _guildsSettingsContext.GuildsSettings.FirstOrDefault(gs => gs.ServerId == Context.Guild.Id.ToString());
                if(_guild == null)
                {
                    _guild = _guildsSettingsContext.GuildsSettings.Include(gs => gs.Users).Where(gs=>gs.ServerId =="0").First();
                    _guild!.ServerId = Context.Guild.Id.ToString();
                    _guildsSettingsContext.Add(_guild);
                    _guildsSettingsContext.SaveChanges();
                }
                _guildsSettingsContext.UserSettings.Where(us => us.GuildId == _guild.ServerId).Load();

                return _guild;
            }
        }


        [RequireOwner]
        [Group("global", "Change global settings params")]
        public class GlobalSettingsModule : InteractionModuleBase
        {
            private GuildSettings _globalSettings;
            private readonly GuildsSettingsContext _guildsSettingsContext;

            public GlobalSettingsModule(GuildsSettingsContext guildsSettingsContext)
            {
                _guildsSettingsContext = guildsSettingsContext;
                _globalSettings = _guildsSettingsContext.GuildsSettings.Include(gs => gs.Users).Where(gs => gs.ServerId == "0").FirstOrDefault() ?? throw new ArgumentNullException(nameof(_globalSettings));
            }


            [SlashCommand("set_volume","Change default volume")]
            public async Task SetGlobalVolumeAsync(int volume)
            {
                if(volume is > 100 or < 0)
                {
                    await RespondAsync(embed:TrowError("Value out of bounds", "GuildSettingsModule/GlobalSettings/SetGlobalVolumeAsync/OutOfBounds", Context),ephemeral:true);
                    return;
                }

                _globalSettings.PlayerVolume = volume;
                _guildsSettingsContext.SaveChanges();

                await RespondAsync(embed: SuccessEmbed("Default value changed to " + volume, Context), ephemeral: true);
            }

            [SlashCommand("change_command_perm", "Change the permission to use the command")]
            public async Task ChangeCommandPermissionAsync(IUser? user=null, string? userId=null)
            {
                string? _userId = null;

                if (userId != null&&user==null)
                    _userId = userId;
                else if(user!=null&&userId==null)
                    _userId = user.Id.ToString();
                else if(userId == null && user == null)
                {
                    await RespondAsync(embed: TrowError("You must specify one of the values", "SettingsModule/GlobalSettings/ChangeCommandPermissionAsync", Context));
                    return;
                }
                else
                {
                    await RespondAsync(embed: TrowError("You must specify ONE of the values", "SettingsModule/GlobalSettings/ChangeCommandPermissionAsync", Context));
                    return;
                }
                
                UserSetting? _user = _globalSettings.Users.FirstOrDefault(us=>us.UserId==_userId);

                if(_user == null)
                {
                    _user = new UserSetting()
                    {
                        UserId = _userId,
                        Guild = _globalSettings,
                        GuildId = "0"
                    };
                    _guildsSettingsContext.UserSettings.Add(_user);
                    _guildsSettingsContext.SaveChanges();
                }

                _user.AllowUseCommands = !_user.AllowUseCommands;
                _guildsSettingsContext.SaveChanges();

                await RespondAsync(embed:SuccessEmbed($"You sets AllowUseCommands to {_user.AllowUseCommands} for {MentionUtils.MentionUser(Convert.ToUInt64(_userId))}", Context), ephemeral:true);
            }

        }


        private static Embed TrowError(string _description, string _blockTrowedError, IInteractionContext context )
        {
            var embed = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = context.Client.CurrentUser.Username,
                    IconUrl = context.Client.CurrentUser.GetAvatarUrl()
                },
                Title = "ERROR",
                Color = Color.Red,
                Description = _description
            };
            embed.AddField(
                name: "Block trowed error",
                value: _blockTrowedError
            );

            return embed.Build();
        }

        private static Embed SuccessEmbed(string _description, IInteractionContext context)
        {
            EmbedBuilder embed = new EmbedBuilder()
            {
                Title = "Success",
                Color = Color.Green,
                Author = new EmbedAuthorBuilder()
                {
                    Name = context.Client.CurrentUser.Username,
                    IconUrl = context.Client.CurrentUser.GetAvatarUrl()
                },
                Description = _description
            };

            return embed.Build();
        }
    }

    
}
