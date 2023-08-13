using Discord.Commands;
using Lavalink4NET;
using Lavalink4NET.Rest;
using Lavalink4NET.Player;
using System.Text.RegularExpressions;
using Discord;
using hellgate.Contexts;
using hellgate.Models;
using Discord.WebSocket;

namespace hellgate.Modules
{
	[RequireContext(ContextType.Guild)]
	public class MusicModule : ModuleBase<SocketCommandContext>
	{
        private readonly VoiceContext _voiceContext;
		private readonly IAudioService _audioService;
		private readonly GuildSettings _globalSettings;
        private readonly GuildsSettingsContext _guildsSettingsContext;

		public MusicModule(IAudioService audioService, GuildsSettingsContext guildsSettingsContext, GuildSettings globalSettings, VoiceContext voiceContext)
		{
			_voiceContext = voiceContext;
			_globalSettings = globalSettings;
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
			GuildSettings guildSettings = _guildsSettingsContext.GuildsSettings.FirstOrDefault(gs => gs.ServerId == Context.Guild.Id.ToString())??_globalSettings;

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
				await ReplyAsync(embed: SuccessEmbed(""));
				return;
			}

			if(Regex.IsMatch(videoLink, @"(?:(?:.?youtu.?be)|(?:soundcloud))(?:.com)?", RegexOptions.Singleline))
			{
				if (Regex.IsMatch(videoLink, "soundcloud"))
				{
					track = await _audioService.GetTrackAsync(videoLink, SearchMode.SoundCloud);
				}
				else if (Regex.IsMatch(videoLink, @"(?:.?youtu.?be)(?:.com)"))
				{
					track = await _audioService.GetTrackAsync(videoLink, SearchMode.YouTube);
				}

				
				if (track == null)
				{
					await ReplyAsync("😖 No results.");//TODO: Change this to embed
					return;
				}

				track.Context = Context.User.Id;

				int position = await player.PlayAsync(track, enqueue: true);
				await player.SetVolumeAsync(guildSettings.PlayerVolume);

				if (position == 0)
				{
					await ReplyAsync("🔈 Playing: " + track.Uri);//TODO: Change this to embed
				}
				else
				{
					await ReplyAsync("🔈 Added to queue: " + track.Uri);//TODO: Change this to embed
				}
			}
			else
			{
				await ReplyAsync(embed: TrowError("This isn't youtube or soundcloud url","MusicModule/PlayAsync/RegexCheck"));
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
				await ReplyAsync(embed: SuccessEmbed("Player unpaused!"));
			}

			if(player.CurrentTrack == null)
			{
				await ReplyAsync(
					embed: TrowError("Nothing playing!", "MusicModule/PauseAsync/PlayingCheck")
				);
			}

			await player.PauseAsync().ConfigureAwait(false);

			await ReplyAsync(embed: SuccessEmbed("Player paused"));
		}

		[Command("queue")]
		[Alias("q", "list", "список", "с")]
		[Summary("Sends queue")]
		public async Task SendQueueAsync()
		{
			QueuedLavalinkPlayer player = await GetPlayerAsync();

			if (player == null)
			{
				return;
			}



			await Task.CompletedTask;
		}

		[Command("skip")]
		[Alias("s", "пропустить", "п")]
		[Summary("Skip n tracks")]//Add Usercheck
		public async Task SkipAsync([Summary("count to skip")]int count = 1)
		{
			VoteLavalinkPlayer player = await GetPlayerAsync();

			if(player == null)
			{
				return;
			}

			await player.SkipAsync(count);
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
				await ReplyAsync(embed:TrowError("I can't find this track","MusicModule/Remove"));
				return;
			}

			Voice voiceChannel = _voiceContext.Voices.FirstOrDefault(vo=> vo.voiceId == player.VoiceChannelId.ToString())!;

			if(voiceChannel == null)
			{
				await ReplyAsync(embed:TrowError("I can't find VoiceInfo","MusicModule/RemoveAsync/line_176"));
				return;
			}

			if (trackToRemove.Context!= (object)Context.User.Id || voiceChannel.OwnerId==Context.User.Id.ToString())
            {
				await ReplyAsync(embed:TrowError("You can't remove this track", "MusicModule/RemoveAsync"));
				return;
			}

			player.Queue.Remove(trackToRemove);

			await ReplyAsync(embed:SuccessEmbed("Track "+trackToRemove.Author+" - "+trackToRemove.Title+" was removed"));
			
		}

		[Command("nowplaying")]
		[Alias("np", "track", "song", "проигрывается", "песня", "трек")]
		[Summary("Sends track info")]
		public async Task NowPlayingAsync()
		{

			await Task.CompletedTask;
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
			await ReplyAsync("Disconnected.");
		}

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

		/*private bool AuthorizeCheck(QueuedLavalinkPlayer Player)
		{
			
		}*/
	}
}
