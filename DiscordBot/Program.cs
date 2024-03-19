using DiscordBot.commands;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using System;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using System.Linq;
using System.Collections.Generic;

namespace DiscordBot
{
    internal class Program
    {
        private static DiscordClient Client { get; set; }

        static async Task Main(string[] args)
        {
            var jsonReader = new JSONReader();
            await jsonReader.ReadJson();

            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
            };

            Client = new DiscordClient(discordConfig);

            Client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(2)
            });

            Client.Ready += Client_Ready;
            Client.ComponentInteractionCreated += Client_ComponentInteractionCreated;
            Client.MessageDeleted += Client_MessageDeleted;

            var slashCommandsConfig = Client.UseSlashCommands();

            slashCommandsConfig.RegisterCommands<EventCommands>();

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static async Task Client_MessageDeleted(DiscordClient sender, MessageDeleteEventArgs args)
        {
            if (args?.Message?.Interaction?.Id != null && GameManager.gameData.ContainsKey(args.Message.Interaction.Id))
                GameManager.gameData.Remove(args.Message.Interaction.Id);
            return;
        }

        private static async Task Client_ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs args)
        {
            switch (args.Interaction.Data.CustomId.Split('_')[0])
            {
                case "gm":
                    await HandleGameEventInteraction(sender, args);
                    break;
                case "po":
                    await HandlePollInteraction(sender, args);
                    break;
            }
        }

        private static async Task HandleGameEventInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs args)
        {

            DiscordEmbed attachedEmbed = args.Message.Embeds[0];

            List<DiscordMember> members = new List<DiscordMember>();
            var usersRaw = await args?.Guild?.GetAllMembersAsync();
            if (usersRaw != null)
            {
                members = usersRaw.ToList();
            }

            GameData gameData = GameManager.GetOrCreateGameData(args.Message.Interaction.Id, attachedEmbed, members);

            var times = gameData.timePlayer.Keys.ToList();
            times.Sort();

            var player = args.Interaction.User as DiscordMember;

            switch (args.Interaction.Data.CustomId)
            {
                case "gm_accept":
                    gameData.MovePlayer(player, times[0]);
                    break;
                case "gm_30min":
                    gameData.MovePlayer(player, times[1]);
                    break;
                case "gm_1h":
                    gameData.MovePlayer(player, times[2]);
                    break;
                case "gm_2h":
                    gameData.MovePlayer(player, times[3]);
                    break;
                case "gm_decline":
                    gameData.DeclinePlayer(player);
                    break;
                default:
                    gameData.MaybePlayer(player);
                    break;
            }

            await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
            .AddEmbed(GameManager.CreateEmbed(gameData, members, attachedEmbed.Image?.Url.ToString(), attachedEmbed.Thumbnail?.Url.ToString()))
            .AddComponents(GameManager.CreateButtons2(times[0]))
            .AddComponents(GameManager.CreateButtons1(times[0]))
            );
        }

        private static async Task HandlePollInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs args)
        {
            DiscordEmbed attachedEmbed = args.Message.Embeds[0];

            List<DiscordMember> members = new List<DiscordMember>();
            var usersRaw = await args?.Guild?.GetAllMembersAsync();
            if (usersRaw != null)
            {
                members = usersRaw.ToList();
            }

            bool multipleAnswers = bool.Parse(attachedEmbed.Footer.Text.Split(new string[] { ": " }, StringSplitOptions.RemoveEmptyEntries)[1]);

            List<(string, HashSet<DiscordMember>)> options = new List<(string, HashSet<DiscordMember>)>();
            foreach (var field in attachedEmbed.Fields)
            {
                string name = field.Name;
                HashSet<DiscordMember> voted = new HashSet<DiscordMember>();
                string[] values = field.Value.Split('\n');
                if (values.Length == 2)
                {
                    foreach (var mention in values[1].Split(','))
                    {
                        var foundMember = members.Find(m => m.Mention == mention);
                        if (foundMember != null)
                        {
                            voted.Add(foundMember);
                        }
                    }
                }

                options.Add((name, voted));
            }

            var membersVoted = options.Select(o => o.Item2).ToHashSet();

            var inputs = args.Interaction.Data.Values;

            if (args.Interaction.Data.CustomId == "po_remove")
            {
                inputs = new string[0];
            }

            var user = args.Interaction.User as DiscordMember;


            if (multipleAnswers == false)
            {
                if (inputs.Length > 1)
                {
                    await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                        .AsEphemeral(true)
                        .WithContent("Only one Answer allowed!"));

                    return;
                }
            }


            foreach (var option in options)
            {
                if (inputs.Length == 0)
                {
                    option.Item2.Remove(user);
                    continue;
                }

                if (inputs.Contains(option.Item1))
                {
                    option.Item2.Add(user);
                }
                else
                {
                    option.Item2.Remove(user);
                }
            }

            var poll = PollManager.CreatePoll(attachedEmbed.Title, options, multipleAnswers, attachedEmbed.Image?.Url.ToString(), attachedEmbed.Thumbnail?.Url.ToString());
            var selection = PollManager.CreateDropdown(options, multipleAnswers);

            await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
                .AddEmbed(poll)
                .AddComponents(selection)
                .AddComponents(new DiscordButtonComponent(ButtonStyle.Secondary, "po_remove", "Remove vote"))
                );
        }

        private static Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }
    }
}
