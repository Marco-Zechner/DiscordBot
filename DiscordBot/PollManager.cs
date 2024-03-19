using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class PollManager
    {
        public const int width = 25;
        
        public static DiscordEmbedBuilder CreatePoll(string question, List<(string, HashSet<DiscordMember>)> options, bool multipleAnswers, string imageUrl = null, string thumbnailUrl = null)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle(question)
                .WithColor(DiscordColor.Gold)
                .WithImageUrl(imageUrl)
                .WithThumbnail(thumbnailUrl)
                .WithTimestamp(DateTime.Now)
                .WithFooter("Multiple Answers: " + multipleAnswers);
                                

            int totalVotes = options.Sum(x => x.Item2.Count);

            foreach (var option in options)
            {
                string bar = "";
                if (totalVotes > 0)
                    bar = new string('█', (int)Math.Round((double)option.Item2.Count / totalVotes * width));
                bar = bar.PadRight(width, '░');
                string votedBy = "";
                foreach (var member in option.Item2)
                {
                    votedBy += $"{member.Mention},";
                }
                votedBy.TrimEnd(',');
                embed.AddField(option.Item1, $"|{bar}| {option.Item2.Count} votes\n{votedBy}", false);
            }
            return embed;
        }

        public static DiscordSelectComponent CreateDropdown(List<(string, HashSet<DiscordMember>)> options, bool multipleAnswers)
        {
            var optionsDropdown = new List<DiscordSelectComponentOption>();

            foreach (var option in options)
            {
                optionsDropdown.Add(new DiscordSelectComponentOption(
                    option.Item1,
                    option.Item1
                    ));
            }

            var dropdown = new DiscordSelectComponent("po_poll", "Select an option", optionsDropdown, false, 0, multipleAnswers ? options.Count : 1);

            return dropdown;
        }
    }
}
