using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot
{
    public class GameData
    {
        public string Game;
        public DateTimeOffset DefaultDateTime;

        public Dictionary<DateTimeOffset, HashSet<DiscordMember>> TimePlayer
        {
            get
            {
                List<DateTimeOffset> Keys = timePlayer.Keys.ToList();
                foreach(var key in Keys)
                {
                    if (timePlayer[key].Count == 0)
                    {
                        timePlayer.Remove(key);
                    }
                }
                return timePlayer;
            }
        }

        private readonly Dictionary<DateTimeOffset, HashSet<DiscordMember>> timePlayer = new Dictionary<DateTimeOffset, HashSet<DiscordMember>>();
        public HashSet<DiscordMember> expectedPlayers = new HashSet<DiscordMember>();
        public HashSet<DiscordMember> declinedPlayers = new HashSet<DiscordMember>();
        public HashSet<DiscordMember> maybePlayers = new HashSet<DiscordMember>();

        public GameData(string game, DateTimeOffset dateTime, DiscordMember creator, HashSet<DiscordMember> expectedUsers)
        {
            Game = game;
            DefaultDateTime = dateTime;
            timePlayer.Add(dateTime, new HashSet<DiscordMember>());
            if (creator != null)
            {
                timePlayer[dateTime].Add(creator);
            }
            expectedPlayers = expectedUsers;
        }

        public void MovePlayer(DiscordMember player, DateTimeOffset time)
        {
            RemoveInAll(player);

            if (timePlayer.Keys.Contains(time) != false)
            {
                timePlayer[time].Add(player);
                return;
            }

            timePlayer.Add(time, new HashSet<DiscordMember>() { player });
        }

        public void OffsetPlayer(DiscordMember player, bool forward)
        {
            DateTimeOffset current = DefaultDateTime;
            if (timePlayer.Any(x => x.Value.Contains(player)))
            {
                current = timePlayer.First(x => x.Value.Contains(player)).Key;
            }

            current = current.AddMinutes(forward ? 30 : -30);


            MovePlayer(player, current);
        }

        public void DeclinePlayer(DiscordMember player)
        {
            RemoveInAll(player);

            declinedPlayers.Add(player);
        }        
        
        public void MaybePlayer(DiscordMember player)
        {
            RemoveInAll(player);

            maybePlayers.Add(player);
        }

        private void RemoveInAll(DiscordMember player)
        {
            maybePlayers.Remove(player);
            declinedPlayers.Remove(player);
            expectedPlayers.Remove(player);
            var keys = timePlayer.Where(x => x.Value.Contains(player)).Select(x => x.Key);
            foreach (var key in keys)
            {
                timePlayer[key].Remove(player);
            }
        }
    }
}
