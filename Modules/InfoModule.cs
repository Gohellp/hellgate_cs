using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace gotohellp.Modules
{
	public class InfoModule : ModuleBase<SocketCommandContext>
	{

		private readonly CommandService _commands;

		public InfoModule(CommandService commands) => _commands = commands;

		[Command("help")]
		[Summary("Show some helpfull info")]
		public async Task HelpAsync([Summary("Command Name to explore")] string commandName = "base")
		{
			if (commandName == "base")
			{
				var embed = new EmbedBuilder()
				{
					Title = "Help",
					Author = new EmbedAuthorBuilder()
					{
						Name = Context.Client.CurrentUser.GlobalName,
						IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
					},
					Color = new Color(0, 255, 255),
					Fields =
					{
						new EmbedFieldBuilder()
						{
							Name = "Info",
							Value = "Avaible commands: help, info"
						},
						new EmbedFieldBuilder()
						{
							Name = "Music",
							Value = "Avaible commands: play, pause, queue, skip, remove, nowplaying, stop, loop, volume"
						}
					}
				};
				await ReplyAsync(embed: embed.Build());
				return;
			}

			var command = _commands.Commands.FirstOrDefault(c => c.Name == commandName || c.Aliases.Contains(commandName));

			if (command != null)
			{
				var embed = new EmbedBuilder()
				{
					Title = "Help",
					Color = new Color(0, 255, 255),
					Author = new EmbedAuthorBuilder()
					{
						Name = Context.Client.CurrentUser.GlobalName,
						IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
					},
					Fields =
					{
						new EmbedFieldBuilder()
						{
							Name = command.Name,
							Value = command.Summary
						},
						new EmbedFieldBuilder()
						{
							Name = "Aliases",
							Value = string.Join(", ",command.Aliases)
						}
					}

				};
				if (command.Parameters.Count > 0)
				{
					foreach (var parameter in command.Parameters)
					{
						embed.AddField(parameter.Name, parameter.Summary);
					}
				}

				await ReplyAsync(embed: embed.Build());
				return;
			}
			else
			{
				var embed = new EmbedBuilder()
				{
					Title = "Help",
					Color = new Color(0, 255, 255),
					Author = new EmbedAuthorBuilder()
					{
						Name = Context.Client.CurrentUser.GlobalName,
						IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
					},
					Description = $"The command {commandName} does not exist or it has not been found"
				};
				await ReplyAsync(embed: embed.Build());
			}
		}

		[Command("info")]
		[Summary("Show some bot's info")]
		public async Task InfoAsync()
		{
			var app = await Context.Client.GetApplicationInfoAsync();
			var embed = new EmbedBuilder()
			{
				Title = "About",
				Color = new Color(0, 255, 255),
				Author = new EmbedAuthorBuilder()
				{
					Name = app.Owner.GlobalName,
					IconUrl = app.Owner.GetAvatarUrl(),
					Url = "https://discord.gg/BFug9c63JG"
				},
				Description = "This is the most common \"homemade\" Discord bot capable of playing music and all sorts of different things(in the future, mb)",
				Footer = new EmbedFooterBuilder()
				{
					Text = "For all questions, you can contact me either personally(gohelp),\nor by Twitter(X): @HithoshiHuko"
				}

			};
			await ReplyAsync(embed: embed.Build());
		}
	}
}
