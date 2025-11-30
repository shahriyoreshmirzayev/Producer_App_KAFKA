using Confluent.Kafka;
using MVCandKAFKA3.Models;
using System.Text.Json;

namespace MVCandKAFKA3.Services
{
    public class KafkaProducerService
    {
        private readonly IProducer<Null, string> _producer;
        private readonly string _topic;
        private readonly ILogger<KafkaProducerService> _logger;

        public KafkaProducerService(string bootstrapServers, string topic, ILogger<KafkaProducerService> logger)
        {
            _topic = topic;
            _logger = logger;

            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                MaxInFlight = 5,
                MessageTimeoutMs = 10000,
                RequestTimeoutMs = 5000
            };

            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        // Asosiy method - Product yuborish
        public async Task<bool> SendMessageAsync(Product product)
        {
            try
            {
                var json = JsonSerializer.Serialize(product, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var result = await _producer.ProduceAsync(
                    _topic,
                    new Message<Null, string>
                    {
                        Value = json,
                        Timestamp = new Timestamp(DateTime.UtcNow)
                    }
                );

                _logger.LogInformation(
                    "Kafka'ga muvaffaqiyatli yuborildi - Topic: {Topic}, Partition: {Partition}, Offset: {Offset}",
                    result.Topic,
                    result.Partition.Value,
                    result.Offset.Value
                );

                return true;
            }
            catch (ProduceException<Null, string> ex)
            {
                _logger.LogError(ex, "Kafka produce xatolik - Reason: {Reason}", ex.Error.Reason);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kafka yuborishda kutilmagan xatolik");
                return false;
            }
        }

        // Generic method - Istalgan type yuborish
        public async Task<bool> SendAsync<T>(T message)
        {
            try
            {
                var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var result = await _producer.ProduceAsync(
                    _topic,
                    new Message<Null, string> { Value = json }
                );

                _logger.LogInformation(
                    "Message yuborildi - Topic: {Topic}, Offset: {Offset}",
                    result.Topic,
                    result.Offset.Value
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Xatolik yuz berdi");
                return false;
            }
        }

        // Custom topic bilan yuborish
        public async Task<bool> SendAsync<T>(string topic, T message)
        {
            try
            {
                var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var result = await _producer.ProduceAsync(
                    topic,
                    new Message<Null, string> { Value = json }
                );

                _logger.LogInformation(
                    "Custom topic'ga yuborildi - Topic: {Topic}, Offset: {Offset}",
                    result.Topic,
                    result.Offset.Value
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Custom topic'ga yuborishda xatolik");
                return false;
            }
        }

        // Producer'ni to'g'ri dispose qilish
        public void Dispose()
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
    }
}