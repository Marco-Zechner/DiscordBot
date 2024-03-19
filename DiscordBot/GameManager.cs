using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace DiscordBot
{
    public class GameManager
    {
        public static readonly Dictionary<ulong, GameData> gameData = new Dictionary<ulong, GameData>();

        public static GameData GetOrCreateGameData(ulong ID, DiscordEmbed embed, List<DiscordMember> members)
        {
            if (gameData.ContainsKey(ID))
                return gameData[ID];

            string description = embed.Description.Substring(2);
            string game = description.Split('-')[0].Trim();
            string date = description.Split('-')[1].Trim();

            var fields = embed.Fields;

            List<DateTimeOffset> times = new List<DateTimeOffset>();

            HashSet<DiscordMember> users = new HashSet<DiscordMember>();

            foreach (var field in fields)
            {
                if (field.Name.Contains(':'))
                    times.Add(DateTimeOffset.Parse($"{field.Name} {date}"));

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

            GameData gameDataFromEmbed = new GameData(game, times, null, users);

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
                    gameDataFromEmbed.timePlayer[DateTimeOffset.Parse($"{field.Name} {date}")] = playerMembers;
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

        public static DiscordEmbedBuilder CreateEmbed(GameData gameData, List<DiscordMember> members, string imageUrl = null, string thumbnailUrl = null)
        {
            var embed = new DiscordEmbedBuilder
            {
                Description = $"# {gameData.Game} - {gameData.Date:dd.MM.yyyy}",
                Color = DiscordColor.Orange,
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

            foreach (var time in gameData.timePlayer.Keys)
            {
                string players = "";
                foreach (var player in gameData.timePlayer[time])
                {
                    if (assingedPlayers.Contains(player)) continue;

                    if (members.Contains(player))
                    {
                        players += $"{player.Mention}\n";
                    }
                    assingedPlayers.Add(player);
                }
                if (string.IsNullOrEmpty(players))
                    players = "-";
                embed.AddField(time.ToString("HH:mm"), players, true);
            }


            string missingPlayers = "";
            foreach (var player in gameData.expectedPlayers.Where(p => !assingedPlayers.Contains(p) && !gameData.declinedPlayers.Contains(p) && !gameData.maybePlayers.Contains(p)))
            {
                missingPlayers += $"{player.Mention}\n";
            }
            if (string.IsNullOrEmpty(missingPlayers))
                missingPlayers = "-";
            embed.AddField("Missing", missingPlayers, true);


            string declinedPlayers = "";
            foreach (var player in gameData.declinedPlayers)
            {
                declinedPlayers += $"{player.Mention}\n";
            }
            if (string.IsNullOrEmpty(declinedPlayers))
                declinedPlayers = "-";
            embed.AddField("Declined", declinedPlayers, true);            
            
            string maybePlayers = "";
            foreach (var player in gameData.maybePlayers)
            {
                maybePlayers += $"{player.Mention}\n";
            }
            if (string.IsNullOrEmpty(maybePlayers))
                maybePlayers = "-";
            embed.AddField("Maybe", maybePlayers, true);

            return embed;
        }

        public static DiscordComponent[] CreateButtons1(DateTimeOffset time)
        {
            var min30Button = new DiscordButtonComponent(ButtonStyle.Secondary, "gm_30min", time.AddMinutes(30).ToString("HH:mm"));
            var h1Button = new DiscordButtonComponent(ButtonStyle.Secondary, "gm_1h", time.AddMinutes(60).ToString("HH:mm"));
            var h2Button = new DiscordButtonComponent(ButtonStyle.Secondary, "gm_2h", time.AddMinutes(120).ToString("HH:mm"));
            var maybeButton = new DiscordButtonComponent(ButtonStyle.Secondary, "gm_maybe", "maybe");

            return new DiscordComponent[] { min30Button, h1Button, h2Button, maybeButton };
        }        
        
        public static DiscordComponent[] CreateButtons2(DateTimeOffset time)
        {
            var acceptButton = new DiscordButtonComponent(ButtonStyle.Success, "gm_accept", time.ToString("HH:mm"));
            var declineButton = new DiscordButtonComponent(ButtonStyle.Danger, "gm_decline", "Decline");

            return new DiscordComponent[] { acceptButton, declineButton };
        }

    }
}
