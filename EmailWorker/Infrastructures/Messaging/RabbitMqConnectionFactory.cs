using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailWorker.Infrastructures.Messaging
{
    public sealed class RabbitMqConnectionFactory
    {
        private readonly RabbitMqOptions _options;

        public RabbitMqConnectionFactory(IOptions<RabbitMqOptions> options)
        {
            _options = options.Value;
        }

        public IConnection CreateConnection()
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                UserName = _options.UserName,
                Password = _options.Password,
                DispatchConsumersAsync = true
            };

            return factory.CreateConnection();
        }
    }
}
