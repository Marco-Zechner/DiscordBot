using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot
{
    public class GameManager
    {
        public static readonly Dictionary<ulong, GameData> gameData = new Dictionary<ulong, GameData>();

        public static GameData GetOrCreateGameData(ulong ID, DiscordEmbed embed, DateTimeOffset time, List<DiscordMember> members)
        {
            if (gameData.ContainsKey(ID))
                return gameData[ID];
            string game = embed.Description.Substring(2).Trim();
            string date = embed.Title.Trim();

            var fields = embed.Fields;

            HashSet<DiscordMember> users = new HashSet<DiscordMember>();

            foreach (var field in fields)
            {
                if (field.Value == "-") continue;

                string value = field.Value.TrimEnd('\n');
                var players = value.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                HashSet<DiscordMember> playerMembers = new HashSet<DiscordMember>();

                for (int i = players.Count - 1; i >= 0; i--)
                {
                    if (!members.Exists(m => m.Mention == players[i]))
                    {
                        players.RemoveAt(i);
                    }

                    playerMembers.Add(members.Find(m => m.Mention == players[i]));
                }

                users.UnionWith(playerMembers);
            }


            GameData gameDataFromEmbed = new GameData(game, time, null, users);

            foreach (var field in fields)
            {
                if (field.Value == "-") continue;
                
                string value = field.Value.TrimEnd('\n');
                var players = value.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                HashSet<DiscordMember> playerMembers = new HashSet<DiscordMember>();

                for (int i = players.Count - 1; i >= 0; i--)
                {
                    if (!members.Exists(m => m.Mention == players[i]))
                    {
                        players.RemoveAt(i);
                    }

                    playerMembers.Add(members.Find(m => m.Mention == players[i]));
                }

                if (field.Name.Contains(':'))
                {
                    if (field.Name.Contains('.'))
                        gameDataFromEmbed.TimePlayer[DateTimeOffset.Parse($"{field.Name}")] = playerMembers;
                    else
                        gameDataFromEmbed.TimePlayer[DateTimeOffset.Parse($"{field.Name} {date}")] = playerMembers;
                }
                else if (field.Name == "Declined")
                {
                    gameDataFromEmbed.declinedPlayers = playerMembers;
                }
                else if (field.Name == "Maybe")
                {
                    gameDataFromEmbed.maybePlayers = playerMembers;
                }
            }

            return gameDataFromEmbed;
        }

        public static DiscordEmbedBuilder CreateEmbed(GameData gameData, List<DiscordMember> members, List<string> data, string imageUrl = null, string thumbnailUrl = null)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = $"{gameData.DefaultDateTime:dd.MM.yyyy}",
                Description = $"# {gameData.Game}",
                Color = DiscordColor.Orange,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = string.Join("°o°", data)
                }
            };

            if (string.IsNullOrEmpty(imageUrl) == false)
            {
                embed.ImageUrl = imageUrl;
            }

            if (string.IsNullOrEmpty(thumbnailUrl) == false)
            {
                embed.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                {
                    Url = thumbnailUrl
                };
            }

            HashSet<DiscordMember> assingedPlayers = new HashSet<DiscordMember>();

            foreach (var time in gameData.TimePlayer.Keys)
            {
                string players = "";
                foreach (var player in gameData.TimePlayer[time])
                {
                    if (assingedPlayers.Contains(player)) continue;

                    if (members.Contains(player))
                    {
                        players += $"{player.Mention}\n";
                    }

                    assingedPlayers.Add(player);
                }

                string fieldName = time.ToString("HH:mm");

                if (time.Date != gameData.DefaultDateTime.Date)
                    fieldName += time.ToString(" dd.MM");

                if (!string.IsNullOrEmpty(players))
                    embed.AddField(fieldName, players, true);
            }

            string missingPlayers = "";
            foreach (var player in gameData.expectedPlayers.Where(p => !assingedPlayers.Contains(p) && !gameData.declinedPlayers.Contains(p) && !gameData.maybePlayers.Contains(p)))
            {
                missingPlayers += $"{player.Mention}\n";
            }
            if (!string.IsNullOrEmpty(missingPlayers))
                embed.AddField("Missing", missingPlayers, true);



            string declinedPlayers = "";
            foreach (var player in gameData.declinedPlayers)
            {
                declinedPlayers += $"{player.Mention}\n";
            }
            if (!string.IsNullOrEmpty(declinedPlayers))
                embed.AddField("Declined", declinedPlayers, true);

            string maybePlayers = "";
            foreach (var player in gameData.maybePlayers)
            {
                maybePlayers += $"{player.Mention}\n";
            }
            if (!string.IsNullOrEmpty(maybePlayers))
                embed.AddField("Maybe", maybePlayers, true);

            return embed;
        }      
        
        public static DiscordComponent[] CreateButtons1(DateTimeOffset time)
        {
            var minus30Min = new DiscordButtonComponent(ButtonStyle.Secondary, "gm_minus30", "<<< 30");
            var acceptButton = new DiscordButtonComponent(ButtonStyle.Success, "gm_accept", time.ToString("HH:mm"));
            var plus30Min = new DiscordButtonComponent(ButtonStyle.Secondary, "gm_plus30", "30 >>>");
            var declineButton = new DiscordButtonComponent(ButtonStyle.Danger, "gm_decline", "Decline");
            var maybeButton = new DiscordButtonComponent(ButtonStyle.Primary, "gm_maybe", "maybe");

            return new DiscordComponent[] { minus30Min, acceptButton, plus30Min, maybeButton, declineButton };
        }

        public static DiscordSelectComponent CreateDropdown(DateTimeOffset dateTime, int hoursToDisplay)
        {
            hoursToDisplay *= 2;
            hoursToDisplay--;
            int stepsBack = hoursToDisplay - 2;
            DateTimeOffset startTime = dateTime.AddMinutes(-stepsBack * 30);

            var optionsDropdown = new List<DiscordSelectComponentOption>();

            for (int i = 0; i < hoursToDisplay * 2; i++)
            {
                string option = startTime.ToString("HH:mm");

                if (startTime.Date != dateTime.Date)
                    option += " " + startTime.ToString("dd.MM");

                if (startTime >= DateTimeOffset.Now)
                    optionsDropdown.Add(new DiscordSelectComponentOption(option, option));

                startTime = startTime.AddMinutes(30);
            }

            var dropdown = new DiscordSelectComponent("gm_time", "Select an option", optionsDropdown, false, 0, 1);

            return dropdown;
        }
    }
}
