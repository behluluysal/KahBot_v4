# KahBot_v4
This is a custom discord bot written in Discord.Net C#. Coded for a shutdown of a discord server. What this bot does is start the server shutdown process with /dreamends command and give a date. Bot will send an embded message to the channel and will start the countdown. Embded message will be updated every minute until it reaches 1 minute. After that bot will start to count 5 secods, when timer ends bot automatically call /testfinalact command. This command will start to kick every member randomly and send them a farewell message.

Commands:
- /dreamends (DateTime time, string reason) : Starts the countdown for server shutdown
- /testfinalact : Starts to kick every user after sending them a farewell message

Notes: 
/dreamends uses MSSQL database. When bot goes active you don't need to recall the command. It'll automatically continue the countdown timers which is not completed yet.
Same functionality is not applied to FinalAct. It is not coded yet.


How to use?
Add appsettings.Development.json and copy the contents from appsetting.json. After filling the contents, you can start the bot. Change launchSettings.json if you'll go for staging or production.


Lacks of:
- /testfinalact values are not stored in database, if code gets interrupted you'll need to recall the method.
- Mainly coded for a single server only. If you are willing to use the bot on different servers simultaneously you'll need to send the parameters to ServerGeneralHelper m StartFinalAct from CountDownTimerHelper m StartCounter.
