using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using hellgate.Models;
using hellgate.Contexts;

namespace hellgate.Services
{
	internal class VoiceStateHandlingService : IHostedService
	{
		private readonly DiscordSocketClient _discord;
		private readonly IConfiguration _config;
		private readonly ILogger<InteractionService> _logger;
		private readonly ulong _startChannelId;
		private readonly VoiceContext _voiceContext;

		private SocketGuild? _nithGuild;
		private ITextChannel? _botChannel;

		public VoiceStateHandlingService(
			DiscordSocketClient discord,
			IConfiguration config,
			ILogger<InteractionService> logger,
			VoiceContext voiceContext)
		{
			_discord = discord;
			_config = config;
			_logger = logger;
			_startChannelId = Convert.ToUInt64(_config["voiceCreateId"]);
			_voiceContext = voiceContext;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			_discord.UserVoiceStateUpdated += UserVoiceStateUpdated;
			_discord.Ready += OnReady;

			await Task.CompletedTask;
		}
		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		private async Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState oldVoiceState, SocketVoiceState newVoiceState)
		{
			if (newVoiceState.VoiceChannel is not null && oldVoiceState.VoiceChannel is not null && newVoiceState.VoiceChannel.Id == oldVoiceState.VoiceChannel.Id)
			{
				if (newVoiceState.IsStreaming && !oldVoiceState.IsStreaming)
				{
					_voiceContext.VoiceLogs.Add(new VoiceLog()
					{
						VoiceName = newVoiceState.VoiceChannel.Name,
						Event = "streamStart",
						EventEmitterId = user.Id.ToString(),
						Description = "User "+user.Username+" start the stream in "+ newVoiceState.VoiceChannel.Name
                    });
				}
				if(!newVoiceState.IsStreaming && oldVoiceState.IsStreaming)
				{
                    _voiceContext.VoiceLogs.Add(new VoiceLog()
                    {
                        VoiceName = newVoiceState.VoiceChannel.Name,
                        Event = "streamEnd",
                        EventEmitterId = user.Id.ToString(),
                        Description = "User " + user.Username + " stop the stream in " + newVoiceState.VoiceChannel.Name
                    });
                }
				if(!oldVoiceState.IsSelfMuted && newVoiceState.IsSelfMuted)
				{
                    _voiceContext.VoiceLogs.Add(new VoiceLog()
                    {
                        VoiceName = newVoiceState.VoiceChannel.Name,
                        Event = "muted",
                        EventEmitterId = user.Id.ToString(),
                        Description = "User " + user.Username + " mute in " + newVoiceState.VoiceChannel.Name
                    });
                }
				if(oldVoiceState.IsMuted && !newVoiceState.IsMuted)
				{
                    _voiceContext.VoiceLogs.Add(new VoiceLog()
                    {
                        VoiceName = newVoiceState.VoiceChannel.Name,
                        Event = "unmuted",
                        EventEmitterId = user.Id.ToString(),
                        Description = "User " + user.Username + " unmute in " + newVoiceState.VoiceChannel.Name
                    });
                }
				if(!oldVoiceState.IsSelfDeafened && newVoiceState.IsSelfDeafened)
				{
					_voiceContext.VoiceLogs.Add(new VoiceLog()
                    {
                        VoiceName = newVoiceState.VoiceChannel.Name,
                        Event = "defened",
                        EventEmitterId = user.Id.ToString(),
                        Description = "User " + user.Username + " was been defened in " + newVoiceState.VoiceChannel.Name
                    });
				}
				if(oldVoiceState.IsSelfMuted && newVoiceState.IsSelfDeafened)
				{
					_voiceContext.VoiceLogs.Add(new VoiceLog()
                    {
                        VoiceName = newVoiceState.VoiceChannel.Name,
                        Event = "undefened",
                        EventEmitterId = user.Id.ToString(),
                        Description = "User " + user.Username + " was been undefened in " + newVoiceState.VoiceChannel.Name
                    });
				}
			}
			else if (oldVoiceState.VoiceChannel is not null )
			{
				_voiceContext.VoiceLogs.Add(new VoiceLog()
				{
					VoiceName = oldVoiceState.VoiceChannel.Name,
					Event = "disconnect",
					EventEmitterId = user.Id.ToString(),
					Description = "User "+user.Username+" disconnected from "+oldVoiceState.VoiceChannel.Name,
				});
				if(newVoiceState.VoiceChannel is not null)
				{
					_voiceContext.VoiceLogs.Add(new VoiceLog()
					{
						VoiceName = newVoiceState.VoiceChannel.Name,
						Event = "connect",
						EventEmitterId = user.Id.ToString(),
						Description = "User " + user.Username + " connected to " + newVoiceState.VoiceChannel.Name,
					});
				}
				_voiceContext.SaveChanges();
				
			}
			else if (newVoiceState.VoiceChannel is not null)
			{
				_voiceContext.VoiceLogs.Add(new VoiceLog()
				{
					VoiceName = newVoiceState.VoiceChannel.Name,
					Event = "connect",
					EventEmitterId = user.Id.ToString(),
					Description = "User " + user.Username + " connected to " + newVoiceState.VoiceChannel.Name,
				});
				_voiceContext.SaveChanges();
				
			}

			//Creatin' channels
			if(newVoiceState.VoiceChannel is not null && newVoiceState.VoiceChannel.Id == _startChannelId)//Connected
			{
				if (oldVoiceState.VoiceChannel is not null)
				{
					if(oldVoiceState.VoiceChannel.ConnectedUsers.Count>0){
						EditChannel(oldVoiceState.VoiceChannel);
					}
					else
					{
						await oldVoiceState.VoiceChannel.DeleteAsync();

						Voice? VoiceInfo = _voiceContext.Voices.FirstOrDefault(vo => vo.voiceId == oldVoiceState.VoiceChannel.Id.ToString());
							
						if(VoiceInfo != null)
						{
                            _voiceContext.Voices.Remove(VoiceInfo);
							_voiceContext.SaveChanges();
						}
						else
						{
							TrowError(
								"",
								"Connected/ChannelHasMembers/Gettin' VoiceInfo"
							);
						}
						
					}
				}


				IVoiceChannel createdChannel = _nithGuild!.CreateVoiceChannelAsync(user.GlobalName + "'s channel", channel =>
				{
					List<Overwrite> permissionOverwrites = new List<Overwrite>
						{
							new Overwrite(
								user.Id,
								PermissionTarget.User,
								new OverwritePermissions(
									manageChannel: PermValue.Allow,
									muteMembers: PermValue.Allow,
									deafenMembers: PermValue.Allow
								)
							)
						};

					channel.PermissionOverwrites = permissionOverwrites;
					channel.CategoryId = newVoiceState.VoiceChannel.CategoryId;
				}).Result;

				await _nithGuild.MoveAsync(_nithGuild.GetUser(user.Id), createdChannel);

				_voiceContext.Voices.Add(new Voice()
				{
					OwnerId = user.Id.ToString(),
					voiceId = createdChannel.Id.ToString()
				});
				_voiceContext.SaveChanges();
				
			}//Connected
			else if (oldVoiceState.VoiceChannel is not null && oldVoiceState.VoiceChannel.Id != _startChannelId)//Disconnected
			{
				Voice? VoiceInfo = _voiceContext.Voices.FirstOrDefault( vo => vo.voiceId == oldVoiceState.VoiceChannel.Id.ToString());

				if (VoiceInfo is null)
				{
					await _botChannel!.SendMessageAsync(
						embed: TrowError(
							"I can't find row with " + MentionUtils.MentionChannel(oldVoiceState.VoiceChannel.Id) + "'s info.",
							"Disconnected/Gettin' VoiceInfo"
						)
					);
					return;
				}
				if (VoiceInfo.OwnerId == user.Id.ToString())
				{
					if (oldVoiceState.VoiceChannel.ConnectedUsers.Count > 0)
					{
						EditChannel(oldVoiceState.VoiceChannel);
					}
					else
					{
                        await oldVoiceState.VoiceChannel.DeleteAsync();

						_voiceContext.Remove(VoiceInfo);
						_voiceContext.SaveChanges();
					}
				}
					
				
			}//Disconnected
		}
		private async void EditChannel(SocketVoiceChannel channel)
		{
			Random random = new Random();
			SocketGuildUser newOwner = channel.ConnectedUsers.ElementAt(random.Next(channel.ConnectedUsers.Count));

			await channel.ModifyAsync(_channel =>
			{
				List<Overwrite> permissionOverwrites = new List<Overwrite>
				{
					new Overwrite(
						newOwner.Id,
						PermissionTarget.User,
						new OverwritePermissions(
							manageChannel: PermValue.Allow,
							muteMembers: PermValue.Allow,
							deafenMembers: PermValue.Allow
						)
					)
				};

				_channel.Name = newOwner.Username + "'s channel";
				_channel.PermissionOverwrites = permissionOverwrites;

			});

			
			Voice? voice = _voiceContext.Voices.FirstOrDefault(_vo => _vo.voiceId == channel.Id.ToString());

			if(voice is not null)
			{ 
				voice.OwnerId = newOwner.Id.ToString();

				_voiceContext.SaveChanges();
			}
			else
			{
				await _botChannel!.SendMessageAsync(
					embed: TrowError(
						"I can't find row with " + MentionUtils.MentionChannel(channel.Id) + "'s info.",
						"EditChannel/Gettin' VoiceInfo"
					)
				);
			}
			
		}

		private async Task OnReady()
		{
			Random random = new Random();

			_nithGuild = _discord.GetGuild(Convert.ToUInt64(_config["nithGuildId"]));
			_botChannel = _nithGuild.GetTextChannel(Convert.ToUInt64(_config["botChannelId"]));

			SocketVoiceChannel CreateChannel = _nithGuild!.GetVoiceChannel(_startChannelId);

			if (CreateChannel.ConnectedUsers.Count > 0)
			{
				SocketGuildUser newOwner = CreateChannel.ConnectedUsers.ElementAt(random.Next(CreateChannel.ConnectedUsers.Count));

				IVoiceChannel newChannel = _nithGuild!.CreateVoiceChannelAsync(newOwner.GlobalName + "'s channel", channel =>
				{
					List<Overwrite> permissionOverwrites = new List<Overwrite>
						{
							new Overwrite(
								newOwner.Id,
								PermissionTarget.User,
								new OverwritePermissions(
									manageChannel: PermValue.Allow,
									muteMembers: PermValue.Allow,
									deafenMembers: PermValue.Allow
								)
							)
						};

					channel.PermissionOverwrites = permissionOverwrites;
					channel.CategoryId = CreateChannel.CategoryId;
				}).Result;

				for (int i = 0; i < CreateChannel.ConnectedUsers.Count; i++)
				{
					await _nithGuild.MoveAsync(CreateChannel.ConnectedUsers.ElementAt(i), newChannel).ConfigureAwait(false);
				}


                _voiceContext.Voices.Add(new Voice()
				{
					OwnerId = newOwner.Id.ToString(),
					voiceId = newChannel.Id.ToString(),
				});
                _voiceContext.SaveChanges();
				
			}
		}

		private Embed TrowError(string _description, string _blockTrowedError)
		{
			var embed = new EmbedBuilder()
			{
				Author = new EmbedAuthorBuilder()
				{
					Name = _discord.CurrentUser.Username,
					IconUrl = _discord.CurrentUser.GetAvatarUrl()
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
	}
}
