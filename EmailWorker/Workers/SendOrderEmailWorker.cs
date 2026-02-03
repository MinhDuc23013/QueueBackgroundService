using EmailWorker.Applications.Interfaces;
using EmailWorker.Applications.Interfaces.DTO;
using EmailWorker.Infrastructures.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EmailWorker.Workers
{
    public sealed class SendOrderEmailWorker : BackgroundService
    {
        private readonly IEmailService _emailService;
        private readonly RabbitMqOptions _options;

        private IConnection? _connection;
        private IModel? _channel;

        public SendOrderEmailWorker(
            IEmailService emailService,
            IOptions<RabbitMqOptions> options)
        {
            _emailService = emailService;
            _options = options.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName ?? "localhost",
                Port = 5672,
                UserName = _options.UserName ?? "guest",
                Password = _options.Password ?? "guest",
                DispatchConsumersAsync = true
            };

            string queueName = "email-service";

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceivedAsync;

            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

            Console.WriteLine("Email Worker started...");
            return Task.CompletedTask;
        }

        private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
        {
            try
            {
                var body = Encoding.UTF8.GetString(args.Body.ToArray());
                var command = JsonSerializer.Deserialize<EmailRequestDTO>(body)!;

                await _emailService.SendAsync(command);

                _channel!.BasicAck(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");

                _channel!.BasicNack(
                    args.DeliveryTag,
                    multiple: false,
                    requeue: false);
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
