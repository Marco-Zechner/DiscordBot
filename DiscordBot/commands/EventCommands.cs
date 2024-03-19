using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.commands
{
    public class EventCommands : ApplicationCommandModule
    {
        [SlashCommand("helpMe", "This is a basic help Command")]
        public async Task Help(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Use: !letsPlay (game) (time) [date] to schedule a game!")
                );
        }

        [SlashCommand("letsPlay", "This is a command to shedule a game to play")]
        public async Task LetsPlay(InteractionContext ctx,
            [Option("game", "The game that you want to play")] string game,
            [Option("time", "The time when you want to play")] string time,
            [Option("date", "The date when you want to play")] string date = "",
            [Option("role", "The role that should be pinged")] DiscordRole role = null,
            [Option("background", "Large Image Below")] DiscordAttachment background = null,
            [Option("icon", "Small Image Top Right")] DiscordAttachment icon = null)
        {
            await ctx.DeferAsync();

            if (string.IsNullOrEmpty(date))
            {
                date = DateTime.Now.Date.ToString("dd.MM.yyyy");
            }

            if (!DateTimeOffset.TryParse($"{date} {time}", out DateTimeOffset dateTime))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Invalid Time \"{time}\" or Date \"{date}\". (date is optional)")
                );
                return;
            }

            if (background != null && background.MediaType.Split('/')[0] != "image")
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Invalid image \"{background.FileName}\" is not an Image (image is optional)")
                );
                return;
            }

            if (icon != null && icon.MediaType.Split('/')[0] != "image")
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Invalid thumbnail \"{icon.FileName}\" is not an Image (thumbnail is optional)")
                );
                return;
            }

            string description = $"# {dateTime:HH:mm}\n{ctx.User.Mention}";

            if (role != null)
            {
                description += $"\n{role.Mention}";
            }

            var members = await ctx.Guild.GetAllMembersAsync();

            List<DateTimeOffset> dateTimes = new List<DateTimeOffset>()
            {
                dateTime,
                new DateTimeOffset(dateTime.DateTime).AddMinutes(30),
                new DateTimeOffset(dateTime.DateTime).AddMinutes(60),
                new DateTimeOffset(dateTime.DateTime).AddMinutes(120),
            };

            DiscordMember creator = ctx.User as DiscordMember;

            if (!creator.Roles.Contains(role))
                creator = null;


            var gameData = new GameData(game, dateTimes, creator, members.Where(m => m.Roles.Contains(role)).ToHashSet());

            GameManager.gameData.Add(ctx.Interaction.Id, gameData);

            var embed = GameManager.CreateEmbed(gameData, members.ToList(), background?.Url, icon?.Url);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(embed)
                .AddComponents(GameManager.CreateButtons2(dateTime))
                .AddComponents(GameManager.CreateButtons1(dateTime))
                );
        }

        [SlashCommand("Poll", "Poll stuff")]
        public async Task Poll(InteractionContext ctx,
            [Option("question", "The poll question")] string question,
            [Option("multipleAnswers", "If a user can select multipleAnswers")] bool multipleAnswers,
            [Option("option1", "options")] string option1,
            [Option("option2", "options")] string option2,
            [Option("option3", "options")] string option3 = "",
            [Option("option4", "options")] string option4 = "",
            [Option("option5", "options")] string option5 = "",
            [Option("option6", "options")] string option6 = "",
            [Option("option7", "options")] string option7 = "",
            [Option("option8", "options")] string option8 = "",
            [Option("option9", "options")] string option9 = "",
            [Option("option10", "options")] string option10 = "",
            [Option("role", "The role that should be pinged")] DiscordRole role = null,
            [Option("background", "Large Image Below")] DiscordAttachment background = null,
            [Option("icon", "Small Image Top Right")] DiscordAttachment icon = null)   
        {
            await ctx.DeferAsync();

            if (background != null && background.MediaType.Split('/')[0] != "image")
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Invalid image \"{background.FileName}\" is not an Image (image is optional)")
                );
                return;
            }

            if (icon != null && icon.MediaType.Split('/')[0] != "image")
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Invalid thumbnail \"{icon.FileName}\" is not an Image (thumbnail is optional)")
                );
                return;
            }

            List<(string, HashSet<DiscordMember>)> options = new List<(string, HashSet<DiscordMember>)>();

            if (!string.IsNullOrEmpty(option1))
                options.Add((option1, new HashSet<DiscordMember>()));
            if (!string.IsNullOrEmpty(option2))
                options.Add((option2, new HashSet<DiscordMember>()));
            if (!string.IsNullOrEmpty(option3))
                options.Add((option3, new HashSet<DiscordMember>()));
            if (!string.IsNullOrEmpty(option4))
                options.Add((option4, new HashSet<DiscordMember>()));
            if (!string.IsNullOrEmpty(option5))
                options.Add((option5, new HashSet<DiscordMember>()));
            if (!string.IsNullOrEmpty(option6))
                options.Add((option6, new HashSet<DiscordMember>()));
            if (!string.IsNullOrEmpty(option7))
                options.Add((option7, new HashSet<DiscordMember>()));
            if (!string.IsNullOrEmpty(option8))
                options.Add((option8, new HashSet<DiscordMember>()));
            if (!string.IsNullOrEmpty(option9))
                options.Add((option9, new HashSet<DiscordMember>()));
            if (!string.IsNullOrEmpty(option10))
                options.Add((option10, new HashSet<DiscordMember>()));

            var poll = PollManager.CreatePoll(question, options, multipleAnswers, background?.Url, icon?.Url);
            var selection = PollManager.CreateDropdown(options, multipleAnswers);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(poll)
                .AddComponents(selection)
                .AddComponents(new DiscordButtonComponent(ButtonStyle.Secondary, "po_remove", "Remove vote"))
            );
        }
    }
}
