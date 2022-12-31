using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class Counter
    {
        public Guid Guid { get; set; }
        public string? Reason { get; set; }
        public DateTime EndDate { get; set; }
        public ulong CreatorId { get; set; }
        public string? CreatorName { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public bool IsFinished { get; set; }
    }
}