using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStore.EF.Configurations
{
    public class CounterConfiguration : IEntityTypeConfiguration<Counter>
    {
        public void Configure(EntityTypeBuilder<Counter> builder)
        {
            builder.Property(x => x.ChannelId).IsRequired();
            builder.Property(x => x.Reason).IsRequired();
            builder.Property(x => x.CreatorName).IsRequired();
            builder.Property(x => x.CreatorId).IsRequired();
            builder.Property(x => x.IsFinished).HasDefaultValue(0);
        }
    }
}
