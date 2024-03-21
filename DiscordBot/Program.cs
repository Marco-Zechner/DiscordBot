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
using System.Data.OleDb;

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

            List<string> data = attachedEmbed.Footer.Text.Split(new string[] { "°o°" }, StringSplitOptions.None).ToList();
            int offsetHours = int.Parse(data[0]);

            List<DiscordMember> members = new List<DiscordMember>();
            var usersRaw = await args?.Guild?.GetAllMembersAsync();
            if (usersRaw != null)
            {
                members = usersRaw.ToList();
            }

            string time = ((DiscordButtonComponent)args.Message.Components.ToList()[0].Components.ToList()[1]).Label;
            string date = attachedEmbed.Title;

            DateTimeOffset dateTime = DateTimeOffset.Parse($"{date} {time}");

            GameData gameData = GameManager.GetOrCreateGameData(args.Message.Interaction.Id, attachedEmbed, dateTime, members);

            var player = args.Interaction.User as DiscordMember;

            switch (args.Interaction.Data.CustomId)
            {
                case "gm_minus30":
                    gameData.OffsetPlayer(player, false);
                    break;
                case "gm_plus30":
                    gameData.OffsetPlayer(player, true);
                    break;
                case "gm_accept":
                    gameData.MovePlayer(player, dateTime);
                    break;
                case "gm_decline":
                    gameData.DeclinePlayer(player);
                    break;
                case "gm_maybe":
                    gameData.MaybePlayer(player);
                    break;
                default:
                    if (args.Interaction.Data.Values.Length != 1)
                        break;
                    gameData.MovePlayer(player, DateTimeOffset.Parse(args.Interaction.Data.Values[0]));
                    break;
            }

            await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
            .AddEmbed(GameManager.CreateEmbed(gameData, members, data, attachedEmbed.Image?.Url.ToString(), attachedEmbed.Thumbnail?.Url.ToString()))
            .AddComponents(GameManager.CreateButtons1(dateTime))
            .AddComponents(GameManager.CreateDropdown(dateTime, offsetHours))
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
