using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using hellgate.Contexts;
using hellgate.Models;
using Lavalink4NET;
using System.Data.Entity;
using static hellgate.Modules.GuildSettingsModule;

namespace hellgate.Modules
{
    [Group("settings","Change settings params")]
    public class GuildSettingsModule : InteractionModuleBase
    {

        [EnabledInDm(false)]
        [RequireContext(ContextType.Guild)]
        [Group("guild", "Change guild settings params")]
        public class GuildSetting : InteractionModuleBase
        {
            private readonly GuildsSettingsContext _guildsSettingsContext;

            public GuildSetting(GuildsSettingsContext guildsSettingsContext)
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


            private GuildSettings GuildCheckAndGet()
            {
                GuildSettings? _guild = _guildsSettingsContext.GuildsSettings.FirstOrDefault(gs=>gs.ServerId==Context.Guild.Id.ToString());

                if(_guild == null)
                {
                    _guild = _guildsSettingsContext.GuildsSettings.Find("0")??throw new ArgumentNullException(nameof(_guild)); ;
                    _guild!.ServerId = Context.Guild.Id.ToString();
                    _guildsSettingsContext.Add(_guild);
                    _guildsSettingsContext.SaveChanges();
                }

                return _guild;
            }
        }


        [RequireOwner]
        [Group("global", "Change global settings params")]
        public class GlobalSettings : InteractionModuleBase
        {
            private GuildSettings _globalSettings;
            private readonly GuildsSettingsContext _guildsSettingsContext;

            public GlobalSettings(GuildsSettingsContext guildsSettingsContext, GuildSettings globalSettings)
            {
                _guildsSettingsContext = guildsSettingsContext;
                _globalSettings = globalSettings;
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
