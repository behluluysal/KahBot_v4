using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace KahBot_v4.Modules
{
    public class MainCommands : ModuleBase<SocketCommandContext>
    {
        [Command("hello")]
        public async Task Hello()
        {
            await Context.Message.ReplyAsync($"Hello {Context.User.Username}. Nice to meet you!");
        }

        [Command("ping", RunMode = RunMode.Async)]
        public async Task PingAsync()
        {
            var tt = await ReplyAsync("pong");
        }
    }
}
