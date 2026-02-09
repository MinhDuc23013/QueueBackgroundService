using EmailWorker.Applications.Interfaces;
using EmailWorker.Applications.Interfaces.DTO;
using EmailWorker.Entity;
using EmailWorker.Infrastructures.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net.Mail;
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

        private const int MaxRetry = 3;

        private const string MainQueue = "email-service";
        private const string RetryQueue = "email-service.retry";
        private const string DlqQueue = "email-service.dlq";

        private static readonly ThreadLocal<Random> _random =
            new(() => new Random());

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
                DispatchConsumersAsync = true,

                // ⭐ quan trọng
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            DeclareQueues();

            _channel.BasicQos(0, 1, false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceivedAsync;

            _channel.BasicConsume(MainQueue, autoAck: false, consumer);

            Console.WriteLine("Email Worker started...");
            return Task.CompletedTask;
        }

        private void DeclareQueues()
        {
            _channel!.QueueDeclare(
                queue: MainQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    ["x-dead-letter-exchange"] = "",
                    ["x-dead-letter-routing-key"] = RetryQueue
                });

            _channel.QueueDeclare(
                queue: RetryQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    ["x-dead-letter-exchange"] = "",
                    ["x-dead-letter-routing-key"] = MainQueue
                });

            _channel.QueueDeclare(
                queue: DlqQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);
        }

        private async Task OnMessageReceivedAsync(
            object sender,
            BasicDeliverEventArgs ea)
        {
            try
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());

                var dto = JsonSerializer.Deserialize<OutboxMessage>(message);
                if (dto == null)
                    throw new InvalidDataException("Invalid message payload");

                await _emailService.SendAsync(dto);

                _channel!.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                HandleRetry(ea, ex);
            }
        }

        private void HandleRetry(BasicDeliverEventArgs ea, Exception ex)
        {
            if (!IsTransient(ex))
            {
                PublishToDlq(ea, ex);
                _channel!.BasicAck(ea.DeliveryTag, false);
                return;
            }

            var headers = ea.BasicProperties.Headers != null
                ? new Dictionary<string, object>(ea.BasicProperties.Headers)
                : new Dictionary<string, object>();

            int retryCount = headers.TryGetValue("x-retry-count", out var value)
                ? Convert.ToInt32(value)
                : 0;

            retryCount++;

            if (retryCount > MaxRetry)
            {
                PublishToDlq(ea, ex);
                _channel!.BasicAck(ea.DeliveryTag, false);
                return;
            }

            var delayMs = CalculateRetryDelayWithJitter(retryCount);

            var props = _channel!.CreateBasicProperties();
            props.Persistent = true;
            props.Headers = headers;

            props.Headers["x-retry-count"] = retryCount;
            props.Headers["x-error-type"] = ex.GetType().Name;
            props.Headers["x-last-retry-at"] = DateTime.UtcNow.ToString("O");

            // ⭐ TTL per message
            props.Expiration = delayMs.ToString();

            _channel.BasicPublish(
                exchange: "",
                routingKey: RetryQueue,
                basicProperties: props,
                body: ea.Body);

            _channel.BasicAck(ea.DeliveryTag, false);
        }

        private void PublishToDlq(BasicDeliverEventArgs ea, Exception ex)
        {
            var props = _channel!.CreateBasicProperties();
            props.Persistent = true;
            props.Headers = ea.BasicProperties.Headers != null
                ? new Dictionary<string, object>(ea.BasicProperties.Headers)
                : new Dictionary<string, object>();

            props.Headers["x-error"] = ex.Message;
            props.Headers["x-failed-at"] = DateTime.UtcNow.ToString("O");

            _channel.BasicPublish(
                exchange: "",
                routingKey: DlqQueue,
                basicProperties: props,
                body: ea.Body);
        }

        private bool IsTransient(Exception ex) =>
            ex switch
            {
                TimeoutException => true,
                TaskCanceledException => true,
                HttpRequestException => true,

                SmtpException smtp when smtp.StatusCode is
                    SmtpStatusCode.MailboxBusy or
                    SmtpStatusCode.MailboxUnavailable or
                    SmtpStatusCode.TransactionFailed => true,

                AggregateException agg => agg.InnerExceptions.Any(IsTransient),
                _ when ex.InnerException != null => IsTransient(ex.InnerException),

                _ => false
            };

        private int CalculateRetryDelayWithJitter(int retryCount)
        {
            const int baseDelayMs = 5_000;
            const int maxDelayMs = 2 * 60 * 1000;

            var exp = Math.Min(
                baseDelayMs * Math.Pow(2, retryCount - 1),
                maxDelayMs);

            return _random.Value!.Next(0, (int)exp);
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
