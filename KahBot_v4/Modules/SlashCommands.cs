using DataStore.EF.Data;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using KahBot_v4.Controllers;
using KahBot_v4.Helpers;
using Microsoft.IdentityModel.Logging;
using Microsoft.Extensions.Configuration;

namespace KahBot_v4.Modules
{
    public class SlashCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly CounterController _counterController;
        private readonly CountDownTimerHelper _countDownTimerHelper;
        private readonly IConfigurationRoot _configuration;
        private readonly ServerGeneralHelper _serverGeneralHelper;
        public SlashCommands(CounterController counterController, 
            CountDownTimerHelper countDownTimerHelper, 
            IConfigurationRoot configuration
            ,ServerGeneralHelper serverGeneralHelper)
        {
            _counterController = counterController;
            _countDownTimerHelper = countDownTimerHelper;
            _configuration = configuration;
            _serverGeneralHelper = serverGeneralHelper;
        }

        [SlashCommand("dreamends", "Going to the world ending?")]
        public async Task StartCounterAsync(DateTime endDate, string reason)
        {
            // If not admin, don't execute
            string? adminId = _configuration.GetSection("AdminID").Value;
            if (!string.IsNullOrEmpty(adminId)
                && Context.User.Id != ulong.Parse(adminId))
            {
                await RespondAsync("Call admin please");
            }

            if (endDate.CompareTo(DateTime.Now) <= 0)
            {
                await RespondAsync("Cant count forwards");
            }


            // Reply to interaction
            AllowedMentions mentions = new AllowedMentions();
            mentions.AllowedTypes = AllowedMentionTypes.Everyone;
            await RespondAsync(text: $"CountDown started for final act! <#{(_configuration.GetSection("Channels:Announcements").Value)}> @everyone", allowedMentions: mentions);

            // Send the first message to hold the message as a variable to be able to edit again.
            double timeLeftMillis = endDate.Subtract(DateTime.Now).TotalMilliseconds;
            

            // Send a dummy text to get channel id and message id
            IUserMessage counterMessage = await ReplyAsync("CountDown timer is preparing...");

            //Fill model
            Counter counter = new Counter
            {
                Reason = reason,
                CreatorId = Context.User.Id,
                CreatorName = Context.User.Username,
                EndDate = endDate,
                ChannelId = counterMessage.Channel.Id,
                MessageId = counterMessage.Id
            };
            if (!await _counterController.Add(counter))
            {
                await counterMessage.ModifyAsync(x => { x.Content = "Problem at saving data to database"; });
                return;
            };

            // Send the coundown message body
            await counterMessage.ModifyAsync(x => { x.Embed = _countDownTimerHelper.CreateCounterMessage(counter, timeLeftMillis); x.Content = ""; }) ;

            // Start the coundown thread
            var countDownThread = new Thread(() => _countDownTimerHelper.StartCounter(counter, Context.Client));
            countDownThread.Start();

            //Send private message to notify the requester user
            Embed pmToRequesterMessageBody = new EmbedBuilder()
                    .WithAuthor(Context.User)
                    .WithTitle("CountDown Timer Request Info")
                    .WithDescription("Your countdown timer request is taken by KahBot_v4")

                    .AddField("Requester: ", $"{Context.User}")
                    .AddField("Reason: ", $"{reason}")
                    .AddField("Target Date: ", $"{endDate}")

                    .WithFooter(footer => footer.Text = "Fun fact: ")
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp()
                    .Build();
            await Context.User.SendMessageAsync(embed: pmToRequesterMessageBody);
        }

        [SlashCommand("testfinalact", "test final act")]
        public async Task TestFinalAct()
        {
            string? adminId = _configuration.GetSection("AdminID").Value;
            if (!string.IsNullOrEmpty(adminId)
                && Context.User.Id != ulong.Parse(adminId))
            {
                await RespondAsync("Call admin please");
            }
            await _serverGeneralHelper.StartFinalAct(Context.Client, Context.Channel.Id);
        }
    }
}
