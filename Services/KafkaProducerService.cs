using Confluent.Kafka;
using System.Text.Json;

namespace MVCandKAFKA3;

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

            _logger.LogInformation("Yuborildi", result.Topic, result.Partition.Value, result.Offset.Value);

            return true;
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError(ex, "Xatolik", ex.Error.Reason);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Xatolik");
            return false;
        }
    }

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

            _logger.LogInformation("Xatolik", result.Topic, result.Offset.Value);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Xatolik");
            return false;
        }
    }

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

            _logger.LogInformation("Yuborildi", result.Topic, result.Offset.Value);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Xatolik");
            return false;
        }
    }
    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}