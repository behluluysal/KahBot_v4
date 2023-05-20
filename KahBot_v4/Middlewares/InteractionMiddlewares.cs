using Discord.WebSocket;
using KahBot_v4.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KahBot_v4.Middlewares
{
    internal class InteractionMiddlewares
    {
        private readonly ServerGeneralHelper _serverGeneralHelper;
        private readonly ResourceHelper _resourceHelper;

        public InteractionMiddlewares(ServerGeneralHelper serverGeneralHelper, ResourceHelper resourceHelper)
        {
            _serverGeneralHelper = serverGeneralHelper;
            _resourceHelper = resourceHelper;
        }

        /// <summary>
        /// Checks the prerequirements for slash commands
        /// </summary>
        /// <param name="SocketInteraction interaction"></param>
        /// <returns>Task</returns>
        public async Task HandleInteraction(SocketInteraction interaction)
        {
            if (_serverGeneralHelper == null || _serverGeneralHelper.CheckRequirements())
            {
                await interaction.RespondAsync(_resourceHelper.GetString(ResourceFiles.GeneralMessages, GeneralMessages.CheckRequirements.ToString()));
                return;
            }
        }
    }
}
