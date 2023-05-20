using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KahBot_v4.Helpers
{
    public class ServerGeneralHelper
    {
        private readonly IConfigurationRoot _configuration;
        private readonly LoggingService _logger;
        private readonly DiscordSocketClient _client;
        private ServerGeneralHelperModel _serverGeneralHelperModel;
        private readonly ResourceHelper _resourceHelper;

        #region [ Inline Class ]
        private class ServerGeneralHelperModel
        {
            public ServerGeneralHelperModel()
            {
                
            }
            public ServerGeneralHelperModel(DiscordSocketClient client, 
                IMessageChannel messageChannel, 
                IGuild guild, 
                IGuildUser admin,
                IGuildUser kahbot)
            {
                Client = client; ;
                FinalActLogChannel = messageChannel;
                Guild = guild;
                Admin = admin;
                KahBot = kahbot;
            }
            public DiscordSocketClient? Client { get; set; }
            public IMessageChannel? FinalActLogChannel { get; set; }
            public IGuild? Guild { get; set; }
            public IGuildUser? Admin { get; set; }
            public IGuildUser? KahBot { get; set; }
        }
        #endregion

        public ServerGeneralHelper(IConfigurationRoot configuration, LoggingService logger, DiscordSocketClient client, ResourceHelper resourceHelper)
        {
            _configuration = configuration;
            _logger = logger;
            _client = client;
            _serverGeneralHelperModel = new ServerGeneralHelperModel();
            _resourceHelper = resourceHelper;
        }

        #region [ Public Methods ]
        /// <summary>
        /// Final Act is for sending a farewell message to each user and kick them after
        /// </summary>
        /// <param name="client"></param>
        public async Task StartFinalAct(DiscordSocketClient client, ulong timerChannelId)
        {
            if (_serverGeneralHelperModel.Client == null || _serverGeneralHelperModel.Guild == null)
            {
                await _logger.LogAsync(new(LogSeverity.Error, "Error", "On executing StartFinalAct: Client or Guild is null, check settings", null));
                return;
            }

            IGuildUser[] users = _serverGeneralHelperModel.Guild.GetUsersAsync().Result.ToArray();
            
            int kickedUsers = 0;
            int totalUsers = users.Where(user => user.IsBot == false).Count();
            totalUsers -= 1; // -1 because admin can't be kicked
            var timerRequestedChannel = await _serverGeneralHelperModel.Guild.GetChannelAsync(timerChannelId) as IMessageChannel;

            var finalActGeneralMessage = await timerRequestedChannel
                .SendMessageAsync(
                    text: _resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.FinalActActivated.ToString()), 
                    embed: PrepareFinalActGeneralMessage(kickedUsers, totalUsers)
                );
            List<IGuildUser> randomUserList = users.OrderBy(arg => Guid.NewGuid()).ToList();

            
            foreach (IGuildUser user in randomUserList)
            {
                try
                {
                    if (user.IsBot || user.Id == _serverGeneralHelperModel.Admin.Id)
                        continue;
                    await SendFarewellMessage(user);
                    await SendServerShutDownMessage(user);
                    AllowedMentions mentions = new AllowedMentions();
                    mentions.AllowedTypes = AllowedMentionTypes.Users;

                    Embed kickUserMessage = new EmbedBuilder()
                       .WithAuthor(_serverGeneralHelperModel.KahBot)
                       .WithTitle(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.Farewell.ToString()))
                       .WithDescription(string.Format(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.LogChannelLeaveMessage.ToString()), user.Id, DateTime.Now))
                       .WithColor(Color.Red)
                       .WithCurrentTimestamp()
                       .Build();
                    await user.KickAsync();
                    kickedUsers++;
                    await _serverGeneralHelperModel.FinalActLogChannel.SendMessageAsync(embed: kickUserMessage);
                    await finalActGeneralMessage.ModifyAsync(x =>
                    {
                        x.Embed = PrepareFinalActGeneralMessage(kickedUsers, totalUsers);
                    });
                    Thread.Sleep(5000);
                }
                catch (Exception e)
                {
                    await _logger.LogAsync(new(LogSeverity.Error, "FinalAct", e.Message, e));
                }
            }
        }

        /// <summary>
        /// Called when DiscordClient is ready
        /// </summary>
        /// <returns></returns>
        public async Task InitializeModel()
        {
            try
            {
                string? guildId = _configuration.GetSection("FinalAct:GuildId").Value;
                string? finalActLogChannelId = _configuration.GetSection("FinalAct:LogChannel").Value;
                string? adminId = _configuration.GetSection("AdminID").Value;
                string? kahbotId = _configuration.GetSection("FinalAct:BotId").Value;

                ServerGeneralHelperModel model = new ServerGeneralHelperModel();
                _serverGeneralHelperModel = model;

                #pragma warning disable CS8601, CS8604, CS8600
                IMessageChannel finalActLogChannel = _client.GetChannel(ulong.Parse(finalActLogChannelId)) as IMessageChannel;
                IGuild guild = _client.GetGuild(ulong.Parse(guildId));
                var admin = await guild.GetUserAsync(ulong.Parse(adminId));
                var kahbot = await guild.GetUserAsync(ulong.Parse(kahbotId));
                #pragma warning restore CS8601, CS8604, CS8600

                if (finalActLogChannel == null || guild == null || admin == null || kahbot == null)
                    throw new NullReferenceException("FinalAct:GuildId or FinalAct:LogChannel was null");

                model = new ServerGeneralHelperModel(_client, finalActLogChannel, guild, admin, kahbot);
                _serverGeneralHelperModel = model;
            }
            catch (Exception e)
            {
                await _logger.LogAsync(new(LogSeverity.Error, "ServerGeneralHelper -> Initialize", e.Message, e));
            }
        }

        public bool CheckRequirements()
        {
            return (_serverGeneralHelperModel == null
                || _serverGeneralHelperModel.Guild == null
                || _serverGeneralHelperModel.FinalActLogChannel == null
                || _serverGeneralHelperModel.KahBot == null
                || _serverGeneralHelperModel.Admin == null);
        }

        #endregion
        #region [ Private Methods ]

        /// <summary>
        /// Prepares embed farewell message to send each user in pm from bot
        /// </summary>
        /// <param name="user"></param>
        private async Task SendFarewellMessage(IGuildUser user)
        {
            Embed pmToUserMessageBody = new EmbedBuilder()
                    .WithAuthor(_serverGeneralHelperModel.KahBot)
                    .WithTitle(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.BotPmTitle.ToString()))
                    .WithDescription(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.BotPmBody.ToString()))

                    .AddField(
                        $"{_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.BotPmCustomFieldTitle.ToString())} :",
                        $"{_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.BotPmCustomFieldBody.ToString())}")
                    .WithFooter(footer => footer.Text = _resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.BotPmFooter.ToString()))
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp()
                    .Build();
            await user.SendMessageAsync(embed: pmToUserMessageBody);
        }

        /// <summary>
        /// Prepares embed farewell message to send each user in pm from admin
        /// </summary>
        /// <param name="user"></param>
        private async Task SendServerShutDownMessage(IGuildUser user)
        {
            Embed pmToUserMessageBody = new EmbedBuilder()
                    .WithAuthor(_serverGeneralHelperModel.Admin)
                    .WithTitle(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.AdminPmTitle.ToString()))
                    .WithDescription(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.AdminPmBody.ToString()))
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp()
                    .Build();
            await user.SendMessageAsync(embed: pmToUserMessageBody);
        }

        private Embed PrepareFinalActGeneralMessage(int kickedUserCount, int totalUsers)
        {
            Embed kickUserMessage = new EmbedBuilder()
                  .WithAuthor(_serverGeneralHelperModel.KahBot)
                  .WithTitle(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.KickedUserCounterTitle.ToString()))
                  .WithDescription($"{kickedUserCount}/{totalUsers} {_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.UsersKicked.ToString())}.")
                  .WithColor(Color.Blue)
                  .WithCurrentTimestamp()
                  .Build();
            return kickUserMessage;
        }

        #endregion
    }
    
}
