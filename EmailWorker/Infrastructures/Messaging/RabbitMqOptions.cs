using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailWorker.Infrastructures.Messaging
{
    public sealed class RabbitMqOptions
    {
        public string HostName { get; init; } = default!;
        public string UserName { get; init; } = default!;
        public string Password { get; init; } = default!;
        public string QueueName { get; init; } = default!;
    }
}
