# KahBot_v4

KahBot_v4 is a custom Discord bot written in C# using the Discord.Net library. It is designed for a specific use case of initiating a server shutdown process on a Discord server. The bot provides commands to start the shutdown process and perform actions related to it.

## Features

- **/dreamends Command**: This command starts the countdown for the server shutdown. It takes two parameters: `time` (DateTime) and `reason` (string). When executed, the bot sends an embedded message to the channel and begins the countdown. The embedded message is updated every minute until it reaches 1 minute remaining. At that point, the bot starts a 5-second countdown and automatically calls the `/testfinalact` command.

- **/testfinalact Command**: This command initiates the final act of the server shutdown process. It kicks every user on the server randomly and sends them a farewell message.

## Usage

1. Add an `appsettings.Development.json` file and copy the contents from `appsettings.json`.
2. Fill in the necessary details in the `appsettings.Development.json` file:
   - `DefaultConnection`: Specify the connection string for your MSSQL database.
   - `BotKey`: Provide the Bot Token obtained after inviting the bot to your server.
   - `LogChannel`: Set the channel where the bot will record the details of kicked users.
   - `GuildId`: Provide the ID of your Discord server where the bot will operate.
   - `BotId`: Specify the user ID of the bot (can be obtained after inviting the bot to the server).
   - `AdminID`: Provide your Discord ID as the administrator of the bot.
   - `Culture`: Choose the desired culture for the bot's language. Currently, only Turkish and English are supported. To add more cultures, duplicate the resource file and translate its contents accordingly.
3. Start the bot.
4. If you wish to deploy the bot to staging or production environments, make the necessary changes in the `launchSettings.json` file.

## Limitations

- **/testfinalact Command**: The values used by this command are not stored in the database. If the code execution gets interrupted, you will need to recall the command manually.
- The bot is primarily coded for a single server. If you intend to use the bot on multiple servers simultaneously, you will need to pass the required parameters to the `ServerGeneralHelper.StartFinalAct` method from `CountDownTimerHelper.StartCounter`.


## Known Bugs
- During the kicking process, after around 50 users, your bot might get reported to Discord as a spam bot since it sends a private message to each user before kicking them.
- When kicking a user, if the user is banned or not accessible, the loop throws an error. This error breaks the loop, but it is covered with try/catch blocks. However, I haven't had a chance to test it.

## What's Missing?
- This bot is designed to work on a single server and can be joined to multiple servers, but it can't execute commands on different servers. If you need that functionality, the code's working logic can be modified to accommodate it.
- I can't think of anything major other than the first point. Feel free to submit a pull request if you notice any improvements.

Feel free to make any necessary modifications or improvements based on your specific requirements.
