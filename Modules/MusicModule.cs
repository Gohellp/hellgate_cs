using Discord;
using Discord.Commands;
using Lavalink4NET;
using Lavalink4NET.Rest;
using Lavalink4NET.Player;
using hellgate.Models;
using hellgate.Contexts;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using SpotifyToYoutube4Net;

namespace hellgate.Modules
{
	[RequireContext(ContextType.Guild)]
	public class MusicModule : ModuleBase<SocketCommandContext>
	{
        private readonly VoiceContext _voiceContext;
		private readonly IAudioService _audioService;
        private readonly GuildsSettingsContext _guildsSettingsContext;

		public MusicModule(
            IAudioService audioService,
            GuildsSettingsContext guildsSettingsContext,
            VoiceContext voiceContext)
		{
			_voiceContext = voiceContext;
			_guildsSettingsContext = guildsSettingsContext;
			_audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
		}

		[Command("play")]
		[Alias("p", "играй", "и")]
		[Summary("Connect and play some track")]
		public async Task PlayAsync([Summary("Link to track")]string? videoLink = null)
		{
			LavalinkTrack? track = null;
			var player = await GetPlayerAsync();
			GuildSettings guildSettings =
				_guildsSettingsContext.GuildsSettings.FirstOrDefault(gs => gs.ServerId == Context.Guild.Id.ToString())
				?? _guildsSettingsContext.GuildsSettings.FirstOrDefault(gs=>gs.ServerId=="0")!;

			if (player == null)
			{
				return;
			}

			//ResumePlayer
			if(videoLink == null)
			{
				if (player.State != PlayerState.Paused)
				{
					await ReplyAsync(embed: TrowError("Player isn't paused.\nIf you wand add a track, commant must include youtube url","MusicModule/PlayAsync/ResumePlayer"));
				}
				await player.ResumeAsync();
				await Context.Message.ReplyAsync(embed: SuccessEmbed("Player resumed"));
				return;
			}

			if (Regex.IsMatch(videoLink, "soundcloud"))
			{
				track = await _audioService.GetTrackAsync(videoLink, SearchMode.SoundCloud);
			}
			else if (Regex.IsMatch(videoLink, @"(?:.?youtu.?be)(?:.com)"))
			{
				track = await _audioService.GetTrackAsync(videoLink, SearchMode.YouTube);
			}
			else if (Regex.IsMatch(videoLink, @"^((?:https?:\/\/)?open\.spotify\.com\/track\/)?([\p{L}+:/\d.]+)(\?si\=[\p{L}+:/\d.]+)?"))
			{
				var Converter = new SpotifyToYoutube("AIzaSyDXwM8TYyshxpd-a_-n35YHGvAftW0ddio", "c4929afbe8f44f789e6c565c9ba2bd6f", "c3c7633841da4194a6fd9ded05c536bc");

				string youtubeUrl = Converter.Convert(videoLink);

                track = await _audioService.GetTrackAsync(youtubeUrl, SearchMode.YouTube);
			}
            else
            {
                await ReplyAsync(embed: TrowError("This isn't valid url", "MusicModule/PlayAsync/RegexCheck"));
            }


            if (track == null)
			{
				await ReplyAsync("😖 No results.");//TODO: Change this to embed
				return;
			}

			track.Context = Context.User;

			PlayerLoopMode loopMode = player.LoopMode;

			if (guildSettings.DefaultLoopQueue)
				loopMode = PlayerLoopMode.Queue;
			player.LoopMode = loopMode;

            int position = await player.PlayAsync(track, enqueue: true);
			if(!(player.State is PlayerState.Playing or PlayerState.Paused))
			{
				await player.SetVolumeAsync(guildSettings.PlayerVolume / 100f);
			}

			if (position == 0)
			{
				await Context.Message.ReplyAsync("🔈 Playing: " + track.Uri);//TODO: Change this to embed
			}
			else
			{
				await Context.Message.ReplyAsync("🔈 Added to queue: " + track.Uri);//TODO: Change this to embed
			}
			
			
		}

		[Command("pause")]
		[Alias("wait", "пауза", "жди", "ж")]
		[Summary("Pause the playing")]
		public async Task PauseAsync()
		{
			LavalinkPlayer player = await GetPlayerAsync();

			if (player == null)
			{
				return;
			}

			if (player.State == PlayerState.Paused)
			{
				await player.ResumeAsync();
				await Context.Message.ReplyAsync(embed: SuccessEmbed("Player unpaused!"));
			}

			if(player.CurrentTrack == null)
			{
				await Context.Message.ReplyAsync(embed: TrowError("Nothing playing!", "MusicModule/PauseAsync/PlayingCheck"));
			}

			await player.PauseAsync().ConfigureAwait(false);

			await Context.Message.ReplyAsync(embed: SuccessEmbed("Player paused"));
		}

		[Command("queue")]
		[Alias("q", "list", "список", "с")]
		[Summary("Sends queue")]
        public async Task SendQueueAsync([Summary("The offset from which to start counting the track list")]int offset = 0)
        {
            var player = await GetPlayerAsync();

			if (player == null)
			{
				return;
			}
            if (player.CurrentTrack == null)
            {
                await Context.Message.ReplyAsync(embed: TrowError("Queue is empty", "MusicModule/SendQueueAsync/CurrentTrackIsNull"));
                return;
            }
            if (offset > player.Queue.Count | offset < 0)
            {
                await Context.Message.DeleteAsync();
                await ReplyAsync(embed:TrowError("Out of range exception", "MusicModule/SendQueueAsync/OffsetOutOfRangeCheck"));
            }

            string queue = $"`now` {Format.Url(player.CurrentTrack.Title, player.CurrentTrack.Uri!.ToString())}\nAdded by {((SocketUser)player.CurrentTrack.Context!).GlobalName}\n";

			for (int i = offset; i < player.Queue.Count&&i<offset+10; i++)
			{
				LavalinkTrack track = player.Queue[i];
				queue+= $"`{i}` {Format.Url(track.Title, track.Uri!.ToString())}\nAdded by {((SocketUser)track.Context!).GlobalName}\n";
			}
			EmbedBuilder embed = new EmbedBuilder()
			{
				Title = "Queue for " + Context.Guild.GetVoiceChannel(id: (ulong)player.VoiceChannelId!).Name,
				Author = new EmbedAuthorBuilder()
				{
					Name = Context.Client.CurrentUser.Username,
					IconUrl = Context.Client.CurrentUser.GetAvatarUrl(),
				},
				Description = queue
			};
			await Context.Message.ReplyAsync(embed:embed.Build());
		}

		[Command("skip")]
		[Alias("s", "пропустить", "п")]
		[Summary("Skip n tracks")]
        public async Task SkipAsync()
		{
			VoteLavalinkPlayer player = await GetPlayerAsync();

			if(player == null)
			{
				return;
			}

			if(player.CurrentTrack == null)
			{
				await Context.Message.ReplyAsync(embed:TrowError("None to skip","MusicModule/Skip/CurrentTrackIsNull"));
				return;
			}

			IUser? user = player.CurrentTrack.Context as IUser;

			if(user == null)
			{
				await Context.Message.ReplyAsync(embed:TrowError("I forgor track owner, lol", "MusicModule/Skip/CurrentTrackContextIsNull"));
				return;
			}

			if(user.Id != Context.User.Id)
			{
				await player.VoteAsync(user.Id);
				await Context.Message.ReplyAsync(embed:SuccessEmbed("You success vote for the skip"));
				return;
			}

            EmbedBuilder embed = new EmbedBuilder()
            {
                Title = "Success",
                Author = new EmbedAuthorBuilder()
                {
                    Name = Context.Client.CurrentUser.Username,
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                },
				Description = "You skipped the track"
            };

			await player.SkipAsync();

			var _player = await GetPlayerAsync();

            if (_player.CurrentTrack != null)
            {
                embed.AddField(
                    "Now play:",
                    $"{_player.CurrentTrack.Author} - {_player.CurrentTrack.Title}\nAdded by {user.Username}");
                
			}

            await Context.Message.ReplyAsync(embed: embed.Build());
		}

		[Command("remove")]
		[Alias("rm","del", "delete", "удалить", "у")]
		[Summary("Remove the track")]
        public async Task RemoveAsync([Summary("Index of track")]int index)
		{
			var player = await GetPlayerAsync();


            if ( player == null)
			{
				return;
			}

			LavalinkTrack trackToRemove = player.Queue.ElementAt(index);

			if(trackToRemove == null)
			{
				await Context.Message.ReplyAsync(embed:TrowError("I can't find this track","MusicModule/Remove"));
				return;
			}

			Voice voiceChannel = _voiceContext.Voices.FirstOrDefault(vo=> vo.voiceId == player.VoiceChannelId.ToString())!;

			if(voiceChannel == null)
			{
				await Context.Message.ReplyAsync(embed:TrowError("I can't find VoiceInfo","MusicModule/RemoveAsync/line_176"));
				return;
			}

			if (trackToRemove.Context!= (object)Context.User.Id || voiceChannel.OwnerId==Context.User.Id.ToString())
            {
				await Context.Message.ReplyAsync(embed:TrowError("You can't remove this track", "MusicModule/RemoveAsync"));
				return;
			}

			player.Queue.Remove(trackToRemove);

			await Context.Message.ReplyAsync(embed:SuccessEmbed("Track "+trackToRemove.Author+" - "+trackToRemove.Title+" was removed"));
			
		}

		[Command("nowplaying")]
		[Alias("np", "track", "song", "проигрывается", "песня", "трек")]
		[Summary("Sends track info")]
		public async Task NowPlayingAsync()
        {
			var player = await GetPlayerAsync();
			if (player == null)
			{
				return;
			}

			LavalinkTrack? track = player.CurrentTrack;

			if(track == null || player.State is PlayerState.NotPlaying or PlayerState.Paused)
			{
				await Context.Message.ReplyAsync(embed:TrowError("Nothing playing now", "MusicModule/NowPlayingAsync/CurrentTrackIsNul"));
				return;
			}

			IUser? owner = (IUser?)track.Context;

			if (owner == null)
			{
				await Context.Message.ReplyAsync(embed:TrowError("I forgor track owner, lol", "MusicModule/Skip/CurrentTrackContextIsNull"));
				return;
			}

			EmbedBuilder embed = new EmbedBuilder()
			{
                Title = "Information",
                Color = new Color(0, 255, 255),
                Author = new EmbedAuthorBuilder()
                {
                    Name = Context.Client.CurrentUser.Username,
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
                }
            };
			embed.AddField(
                    "Now play:",
                    $"{track.Author} - {track.Title}\nAdded by {owner.Username}");
            embed.Url = track.Uri!.ToString();

            await Context.Message.ReplyAsync(embed:embed.Build());
        }

		[Command("stop")]
		[Alias("leave", "die", "disconnect", "стоп", "умри", "уйди")]
		[Summary("Stop playing and leave from channel")]
		public async Task StopAsync()
		{
			var player = await GetPlayerAsync();

			if (player == null)
			{
				return;
			}

			await player.StopAsync(true);
			await ReplyAsync(embed:SuccessEmbed("Disconnected"));
		}

		[Command("loop")]
		[Alias("повторять","повтор")]
		[Summary("Loop playing")]//Placeholder
        public async Task LoopAsync([Summary("Loop mode")]string mode = "none")
        {
			GuildSettings? guild = _guildsSettingsContext.GuildsSettings.FirstOrDefault(gs=>gs.ServerId==Context.Guild.Id.ToString());

			if (guild == null)
			{
				guild = _guildsSettingsContext.GuildsSettings.FirstOrDefault(gs=>gs.ServerId=="0")!;
				guild.ServerId=Context.Guild.Id.ToString();
				_guildsSettingsContext.GuildsSettings.Add(guild);
				_guildsSettingsContext.SaveChanges();
			}

			SocketGuildUser user = (SocketGuildUser)Context.User;

			if(guild.DJRoleId!=String.Empty && user.Roles.FirstOrDefault(r=>r.Id==Convert.ToUInt64(guild.DJRoleId))==null)
			{
				await Context.Message.ReplyAsync(embed:TrowError("You do not have permission to use this command", "MusicModule/LoopAsync"));
				return;
			}

			var player = await GetPlayerAsync();

			//Изменить под строку/массив
            if(mode is "one" or "один")
			{
				player.LoopMode = PlayerLoopMode.Track;
			}
			else if(mode is "queue" or "all" or "все" or "всё" or "список")
			{
				player.LoopMode= PlayerLoopMode.Queue;
			}
			else if(mode is "node" or "off" or "выкл" or "ничего")
			{
				player.LoopMode = PlayerLoopMode.None;
			}
			else
			{
				await Context.Message.ReplyAsync(embed:TrowError("This argument is not supported", "MusicModule/LoopAsync"));
				return;
			}

			await Context.Message.ReplyAsync(embed:SuccessEmbed("Loop mode sets to "+mode));
        }

		[Command("volume")]
		[Alias("v","громкость","г")]
		[Summary("Change the volume of player")]
		public async Task ChangeVolumeAsync([Summary("Volume level")]int volume = 85)
        {
            if (volume is > 100 or < 0)
            {
                await Context.Message.ReplyAsync(embed: TrowError("Value out of bounds", "MusicModule/VolumeAsync/OutOfBounds"));
                return;
            }

            var player = await GetPlayerAsync();

			if(player == null)
			{
				return;
			}

			await player.SetVolumeAsync(volume / 100f);
			await Context.Message.ReplyAsync(embed:SuccessEmbed("Volume changed to "+ volume));
        }

		//Some Private Functions
		private async ValueTask<VoteLavalinkPlayer> GetPlayerAsync(bool connectToVoiceChannel = true)
		{
			var player = _audioService.GetPlayer<VoteLavalinkPlayer>(Context.Guild.Id);

			if (player != null
				&& player.State != PlayerState.NotConnected
				&& player.State != PlayerState.Destroyed)
			{
				return player;
			}

			var user = Context.Guild.GetUser(Context.User.Id);

			if (!user.VoiceState.HasValue)
			{
				await ReplyAsync("You must be in a voice channel!");//TODO:change this to embed
			}

			if (!connectToVoiceChannel)
			{
				await ReplyAsync("The bot is not in a voice channel!");//TODO:change this to embed
			}

			return await _audioService.JoinAsync<VoteLavalinkPlayer>(user.Guild.Id, user.VoiceChannel.Id);
		}

		private Embed TrowError(string _description, string _blockTrowedError)
		{
			var embed = new EmbedBuilder()
			{
				Author = new EmbedAuthorBuilder()
				{
					Name = Context.Client.CurrentUser.Username,
					IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
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

		private Embed SuccessEmbed(string _description)
		{
			EmbedBuilder embed = new EmbedBuilder()
			{
				Title = "Success",
				Color = Color.Green,
				Author = new EmbedAuthorBuilder()
				{
					Name = Context.Client.CurrentUser.Username,
					IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
				},
				Description = _description
			};

			return embed.Build();
		}

		private Embed InformationEmbed(string _description)
		{
			EmbedBuilder embed = new EmbedBuilder()
			{
				Title = "Information",
				Color = new Color(0,255,255),
				Author = new EmbedAuthorBuilder()
				{
					Name = Context.Client.CurrentUser.Username,
					IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
				},
				Description = _description
			};

			return embed.Build();
		}
	}
}
