using Discord;
using Discord.Interactions;
using Core.Models;
using KahBot_v4.Controllers;
using KahBot_v4.Helpers;
using Microsoft.Extensions.Configuration;

namespace KahBot_v4.Modules
{
    public class SlashCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly CounterController _counterController;
        private readonly CountDownTimerHelper _countDownTimerHelper;
        private readonly IConfigurationRoot _configuration;
        private readonly ServerGeneralHelper _serverGeneralHelper;
        private readonly ResourceHelper _resourceHelper;
        private readonly ulong _adminId;
        public SlashCommands(CounterController counterController, 
            CountDownTimerHelper countDownTimerHelper, 
            IConfigurationRoot configuration,
            ServerGeneralHelper serverGeneralHelper, 
            ResourceHelper resourceHelper)
        {
            _counterController = counterController;
            _countDownTimerHelper = countDownTimerHelper;
            _configuration = configuration;
            _serverGeneralHelper = serverGeneralHelper;
            _resourceHelper = resourceHelper;
            _adminId = ulong.Parse(configuration.GetSection("AdminID").Value ?? "0");
        }

        [SlashCommand("dreamends", "Going to the world ending?")]
        public async Task StartCounterAsync(DateTime endDate, string reason)
        {
            if (Context.User.Id != _adminId)
            {
                await RespondAsync(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.AdminOnly.ToString()));
                return;
            }

            if (endDate.CompareTo(DateTime.Now) <= 0)
            {
                await RespondAsync(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.PastDateError.ToString()));
                return;
            }


            // Reply to interaction
            AllowedMentions mentions = new AllowedMentions();
            mentions.AllowedTypes = AllowedMentionTypes.Everyone;
            await RespondAsync(text: $"{_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.CountDownForFinalActStarted.ToString())} " +
                $"<#{(_configuration.GetSection("Channels:Announcements").Value)}> @everyone", allowedMentions: mentions);

            // Send the first message to hold the message as a variable to be able to edit again.
            double timeLeftMillis = endDate.Subtract(DateTime.Now).TotalMilliseconds;
            

            // Send a dummy text to get channel id and message id
            IUserMessage counterMessage = await ReplyAsync(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.CountDownTimerPreparing.ToString()));

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
                await counterMessage.ModifyAsync(x => { x.Content = _resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.DbCrudProblem.ToString()); });
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
                    .WithTitle(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.TimerInfo.ToString()))
                    .WithDescription(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.RequestTaken.ToString()))

                    .AddField(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.Requester.ToString()), Context.User)
                    .AddField(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.Reason.ToString()), reason)
                    .AddField(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.TargetDate.ToString()), endDate)

                    .WithFooter(footer => footer.Text = _resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.FunFact.ToString()))
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp()
                    .Build();
            await Context.User.SendMessageAsync(embed: pmToRequesterMessageBody);
        }

        [SlashCommand("testfinalact", "test final act")]
        public async Task TestFinalAct()
        {
            if (Context.User.Id != _adminId)
            {
                await RespondAsync(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.AdminOnly.ToString()));
                return;
            }
            await _serverGeneralHelper.StartFinalAct(Context.Client, Context.Channel.Id);
        }
    }
}
