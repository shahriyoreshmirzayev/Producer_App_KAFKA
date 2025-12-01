using Confluent.Kafka;
using MVCandKAFKA3.Data;
using System.Text.Json;

namespace MVCandKAFKA3.Services;

public class ApprovalFeedback
{
    public int ProductId { get; set; }
    public string Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime ReviewedDate { get; set; }
    public string? ReviewedBy { get; set; }
    public string? Comments { get; set; }
}

public class FeedbackConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FeedbackConsumerService> _logger;

    public FeedbackConsumerService(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<FeedbackConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = "producer-feedback-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_configuration["Kafka:FeedbackTopic"]);

        _logger.LogInformation("Feedback Consumer ishga tushdi - Topic: {Topic}",
            _configuration["Kafka:FeedbackTopic"]);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(1));

                    if (result != null)
                    {
                        await ProcessFeedback(result.Message.Value);
                        consumer.Commit(result);
                        _logger.LogInformation("Xatolik", result.Offset);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Xatolik");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Xatolik");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessFeedback(string message)
    {
        try
        {
            var feedback = JsonSerializer.Deserialize<ApprovalFeedback>(message);

            if (feedback == null)
            {
                _logger.LogWarning("Xatolik");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var product = await context.Products.FindAsync(feedback.ProductId);

            if (product == null)
            {
                _logger.LogWarning("Xatolik", feedback.ProductId);
                return;
            }

            product.KafkaStatus = feedback.Status;
            product.RejectionReason = feedback.RejectionReason;
            product.ReviewedDate = feedback.ReviewedDate;
            product.ReviewedBy = feedback.ReviewedBy;
            product.ReviewComments = feedback.Comments;
            product.UpdatedDate = DateTime.UtcNow;

            await context.SaveChangesAsync();

            _logger.LogInformation(
                "Qabul qilindi",
                feedback.ProductId, feedback.Status, feedback.ReviewedBy ?? "Unknown");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Xatolik: ", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Xatolik");
        }
    }
}