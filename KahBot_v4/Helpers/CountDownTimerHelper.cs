using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Models;
using System.Diagnostics.Metrics;
using KahBot_v4.Controllers;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace KahBot_v4.Helpers
{
    public class CountDownTimerHelper
    {
        private readonly CounterController _counterController;
        private readonly ServerGeneralHelper _serverGeneralHelper;
        private const int _oneMinuteMillis = 60000;
        private const int _fiveSecondsMilis = 5000;
        private readonly TimeSpan LocalUtcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
        public CountDownTimerHelper(CounterController counterController, ServerGeneralHelper serverGeneralHelper)
        {
            _counterController = counterController;
            _serverGeneralHelper = serverGeneralHelper;
        }

        #region [ Public Methods ]

        public async Task ContinueUnfinishedCountdownsAsync(DiscordSocketClient client)
        {
            List<Counter>? unfinishedCountDowns = await _counterController.Get();
            if(unfinishedCountDowns != null)
            {
                unfinishedCountDowns = unfinishedCountDowns.Where(x => x.IsFinished == false).ToList();
                foreach (var item in unfinishedCountDowns)
                {
                    var countDownThread = new Thread(() => StartCounter(item, client));
                    countDownThread.Start();
                }
            }
        }



        #endregion

        #region [ CountDown Methods ]

        public Embed CreateCounterMessage(Counter counterData, double timeLeftMillis)
        {
            bool isEnded = false;
            TimeSpan timeLeft;
            string footerText = string.Empty;
            if (timeLeftMillis <= 0)
            {
                isEnded = true;
                timeLeft = counterData.EndDate.Subtract(counterData.EndDate);
            }
            else
            {
                timeLeft = TimeSpan.FromMilliseconds(timeLeftMillis);
            }

            if(timeLeftMillis < 60000)
            {
                footerText = "Countdown refreshes every 5 second";
            }
            else
            {
                footerText = "Countdown refreshes every minute";
            }
            return new EmbedBuilder()
                    .WithAuthor(counterData.CreatorName)
                    .WithTitle("Counter")
                    .WithDescription("This counter is created by KahBot_v4")

                    .AddField("Requester: ", $"{(counterData.CreatorName != null ? counterData.CreatorName : "N/A")}")
                    .AddField("Reason: ", $"{counterData.Reason}")
                    .AddField("Target Date: ", $"{counterData.EndDate}")
                    .AddField("Time Left: ", $"{timeLeft} {(isEnded ? "CountDown Finished" : string.Empty)} ")

                    .WithFooter(footer => footer.Text = footerText)
                    .WithColor(Color.Blue)
                    .WithCurrentTimestamp()
                    .Build();
        }
        public async void StartCounter(Counter counter, DiscordSocketClient client)
        {
            #region [ Prepare to start counter ]
            var channel = client.GetChannel(counter.ChannelId) as IMessageChannel;

            if (counter.CreatorName == null || counter.Reason == null || channel == null)
                return;

            IMessage message = await channel.GetMessageAsync(counter.MessageId);
            double timeLeft = counter.EndDate.Subtract(GetNow()).TotalMilliseconds;
            

            if (!await CanStartTimer(counter, message))
            {
                return;
            }

            #endregion

            #region [ Initialize countdown variables ]
            CountMode countMode = CountMode.OneMinute;
            bool isLastMessageSent = false;
            bool isSecondLastMessageSent = false; // Because counters uses 5 second format, we need to know if we the next message will be the last or not
            bool sendLastMessage = false;
            bool sendSecondToLastMessage = false;

            int millisToCountDown = 60000;
            int millisToCheckForLastStage = 60000; //Used only to check if we are about to send the last 2 message or not
            double countDownMilliSeconds = 0;
            #endregion
            //Start CountDown (Main Loop)
            do
            {
                countDownMilliSeconds = counter.EndDate.Subtract(DateTime.UtcNow + LocalUtcOffset).TotalMilliseconds;

                #region [ Check Control Values and Time Format ]

                var result = FixControlValues(isSecondLastMessageSent, countDownMilliSeconds);
                if (result.countMode == CountMode.FiveSeconds)
                {
                    countMode = result.countMode;
                    millisToCountDown = result.millis;
                    millisToCheckForLastStage = result.millis;
                }

                // If 1 minute format and second != 0 then fix time left format
                if (counter.EndDate.Subtract(GetNow()).Seconds != 0 && !isSecondLastMessageSent)
                    countDownMilliSeconds = await FixTimeLeftFormat(counter, countMode);

                result = FixControlValues(isSecondLastMessageSent, countDownMilliSeconds);
                if (result.countMode == CountMode.FiveSeconds)
                {
                    countMode = result.countMode;
                    millisToCountDown = result.millis;
                    millisToCheckForLastStage = result.millis;
                }

                #endregion

                #region [ Determine if the message second to last or last message ]
                if (countDownMilliSeconds <= 0 && !isLastMessageSent)
                    sendLastMessage = true;
                if ((countDownMilliSeconds >= 1 && countDownMilliSeconds <= 5000) && !isSecondLastMessageSent)
                    sendSecondToLastMessage = true;
                #endregion

                if ((sendLastMessage && isSecondLastMessageSent && !isLastMessageSent) // send last message
                    || (sendSecondToLastMessage && !isSecondLastMessageSent) // send second to last message
                    || countDownMilliSeconds > millisToCheckForLastStage) // send normal 5 sec timer message
                {
                    #region [ Edit CountDown message body]
                    if (sendLastMessage)
                    {
                        await ((IUserMessage)message).ModifyAsync(x =>
                        {
                            x.Embed = CreateCounterMessage(counter, countDownMilliSeconds);
                        });
                        //Update db
                        counter.IsFinished = true;
                        if (await _counterController.Put(counter.Guid, counter))
                            break;
                    }
                    else
                    {
                        _ = ((IUserMessage)message).ModifyAsync(x =>
                        {
                            x.Embed = CreateCounterMessage(counter, countDownMilliSeconds);
                        });
                    }
                    #endregion
                }

                #region [ If message was second to last or the last message, mark them as sent]
                if (sendSecondToLastMessage)
                {
                    isSecondLastMessageSent = true;
                    millisToCountDown = 50;
                }
                
                if (sendLastMessage)
                    isLastMessageSent = true;
                #endregion

                Thread.Sleep(millisToCountDown);
                
            } while (!isLastMessageSent);

            await _serverGeneralHelper.StartFinalAct(client, counter.ChannelId);
        }

            #region [ CountDown Calculate Methods (Private) ]

            private DateTime GetNow()
            {
                return DateTime.UtcNow + LocalUtcOffset;
            }

            private (CountMode countMode, int millis) FixControlValues(bool isSecondLastMessageSent, double countDownMilliSeconds)
            {
                if (!isSecondLastMessageSent && countDownMilliSeconds <= (_oneMinuteMillis + _fiveSecondsMilis))
                {
                    return (CountMode.FiveSeconds, _fiveSecondsMilis);
                }
                else
                {
                    return (CountMode.OneMinute, _oneMinuteMillis);
                }
            }

            private async Task<bool> CanStartTimer(Counter counter, IMessage message)
            {
                if (counter.EndDate.Subtract(GetNow()).TotalMilliseconds >= 0)
                {
                    return true;
                }

                await ((IUserMessage)message).ModifyAsync(x =>
                {
                    x.Embed = CreateCounterMessage(counter, 0);
                });
                return false;
            }

            private async Task<double> FixTimeLeftFormat(Counter counter, CountMode countMode)
            {
                if (countMode == CountMode.OneMinute)
                {
                    return await FixTimeLeftOnOneMinuteFormat(counter);
                }
                else
                {
                    return await FixTimeLeftOnFiveSecondsFormat(counter);
                }
            }

            private Task<double> FixTimeLeftOnOneMinuteFormat(Counter counter)
            {
                //Implement 1 min control
                //Wait until timeLeftSeconds == 0 for accuracy
                int timeLeftSeconds = counter.EndDate.Subtract(GetNow()).Seconds;
                int timeLeftMillis = counter.EndDate.Subtract(GetNow()).Milliseconds;
                if (timeLeftSeconds == 0 && timeLeftMillis < 200)
                    return Task.FromResult(counter.EndDate.Subtract(GetNow()).TotalMilliseconds);

                do
                {
                    timeLeftSeconds = counter.EndDate.Subtract(GetNow()).Seconds;
                    Thread.Sleep(100);
                } while (timeLeftSeconds != 0);


                //Wait until timeLeftMillis > 200 for accuracy
                timeLeftMillis = counter.EndDate.Subtract(GetNow()).Milliseconds;
                do
                {
                    timeLeftMillis = counter.EndDate.Subtract(GetNow()).Milliseconds;
                    Thread.Sleep(5);
                } while (timeLeftMillis > 200 && timeLeftSeconds == 0);

                return Task.FromResult(counter.EndDate.Subtract(GetNow()).TotalMilliseconds);
            }

            private Task<double> FixTimeLeftOnFiveSecondsFormat(Counter counter)
            {
                //Implement 5 sec control
                //Wait until timeLeftSeconds mod 5 == 0 for accuracy
                int timeLeftSeconds = counter.EndDate.Subtract(GetNow()).Seconds;
                int timeLeftMillis = counter.EndDate.Subtract(GetNow()).Milliseconds;
                if (timeLeftSeconds % 5 == 0 && timeLeftMillis < 200)
                    return Task.FromResult(counter.EndDate.Subtract(GetNow()).TotalMilliseconds);

                do
                {
                    timeLeftSeconds = counter.EndDate.Subtract(GetNow()).Seconds;
                    Thread.Sleep(100);
                } while (timeLeftSeconds % 5 != 0);


                //Wait until timeLeftMillis > 200 for accuracy
                timeLeftMillis = counter.EndDate.Subtract(GetNow()).Milliseconds;
                do
                {
                    timeLeftMillis = counter.EndDate.Subtract(GetNow()).Milliseconds;
                    Thread.Sleep(5);
                } while (timeLeftMillis > 200 && timeLeftSeconds % 5 == 0);

                return Task.FromResult(counter.EndDate.Subtract(GetNow()).TotalMilliseconds);
            }

            #endregion

        #endregion

        #region [ Inline Enums ]

        enum CountMode
        {
            OneMinute = 0,
            FiveSeconds = 1
        }

        #endregion
    }
}
