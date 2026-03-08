using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmailWorker.Workers
{
    public class KafkaOrderConsumer : BackgroundService
    {
        private readonly ILogger<KafkaOrderConsumer> _logger;

        public KafkaOrderConsumer(ILogger<KafkaOrderConsumer> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "order-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();

            const string topic = "loan.created.draft";
            consumer.Subscribe(topic);

            _logger.LogInformation("Kafka consumer started...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    _logger.LogInformation(
                        $"Received message: {result.Message.Value}"
                    );

                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error");
                }
            }

            consumer.Close();
        }
    }
}
