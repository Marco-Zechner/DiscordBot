using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot
{
    public class GameData
    {
        public string Game;
        public DateTimeOffset Date;
        public readonly Dictionary<DateTimeOffset, HashSet<DiscordMember>> timePlayer = new Dictionary<DateTimeOffset, HashSet<DiscordMember>>();
        public HashSet<DiscordMember> expectedPlayers = new HashSet<DiscordMember>();
        public HashSet<DiscordMember> declinedPlayers = new HashSet<DiscordMember>();
        public HashSet<DiscordMember> maybePlayers = new HashSet<DiscordMember>();

        public GameData(string game, List<DateTimeOffset> dateTimes, DiscordMember creator, HashSet<DiscordMember> expectedUsers)
        {
            Game = game;
            Date = dateTimes[0];
            for (int i = 0; i < dateTimes.Count; i++)
            {
                timePlayer.Add(dateTimes[i], new HashSet<DiscordMember>());

                if (i == 0 && creator != null)
                {
                    timePlayer[dateTimes[i]].Add(creator);
                }
            }
            this.expectedPlayers = expectedUsers;
        }

        public void MovePlayer(DiscordMember player, DateTimeOffset time)
        {
            var keys = timePlayer.Where(x => x.Value.Contains(player)).Select(x => x.Key);
            foreach (var key in keys)
            {
                timePlayer[key].Remove(player);
            }

            declinedPlayers.Remove(player);
            maybePlayers.Remove(player);

            timePlayer[time].Add(player);
        }

        public void DeclinePlayer(DiscordMember player)
        {
            var keys = timePlayer.Where(x => x.Value.Contains(player)).Select(x => x.Key);
            foreach (var key in keys)
            {
                timePlayer[key].Remove(player);
            }

            maybePlayers.Remove(player);

            declinedPlayers.Add(player);
        }        
        
        public void MaybePlayer(DiscordMember player)
        {
            var keys = timePlayer.Where(x => x.Value.Contains(player)).Select(x => x.Key);
            foreach (var key in keys)
            {
                timePlayer[key].Remove(player);
            }

            declinedPlayers.Remove(player);

            maybePlayers.Add(player);
        }
    }
}
