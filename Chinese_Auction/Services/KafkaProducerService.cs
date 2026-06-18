using Confluent.Kafka;
using System.Text.Json;

namespace Chinese_Auction.Services
{
    /// <summary>
    /// ממשק לשירות Kafka Producer
    /// </summary>
    public interface IKafkaProducerService
    {
        Task SendLotteryEventAsync(object lotteryData);
    }

    /// <summary>
    /// שירות ל-Kafka Producer
    /// 
    /// מטרה: שליחת הודעות ל-Kafka עבור אירועים של הגרלות
    /// 
    /// כל הודעה מכילה:
    /// - פרטי המשתמש (ID, אימייל, שם)
    /// - פרטי המתנה (ID, שם, תיאור)
    /// - טיימסטאמפ של האירוע
    /// - סוג האירוע (הגרלה)
    /// </summary>
    public class KafkaProducerService : IKafkaProducerService
    {
        private readonly IProducer<string, string> _producer;
        private readonly string _topic;
        private readonly ILogger<KafkaProducerService> _logger;

        public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
        {
            _logger = logger;

            // קראת Bootstrap Servers מ-Configuration
            var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
            
            // קראת Topic מ-Configuration
            _topic = configuration["Kafka:Topic"] ?? "purchase-events";

            // יצירת ProducerConfig עם הגדרות הקיימות
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                ClientId = "chinese-auction-producer",
                // Acks = RequiredAcks.All = חכה שכל הreplicates אישרו
                Acks = Acks.All,
                // RetryBackoffMs = 100 = המתן 100 מילישניות לפני ניסיון חדש
                RetryBackoffMs = 100,
                // MessageTimeoutMs = 30000 = timeout של 30 שניות
                MessageTimeoutMs = 30000
            };

            // יצירת Producer עם ProducerBuilder
            // ProducerBuilder<string, string> משמעות:
            // - Key = string (מזהה הודעה)
            // - Value = string (תוכן ההודעה JSON)
            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, e) =>
                {
                    _logger.LogError($"Kafka Error: {e.Reason}");
                })
                .SetLogHandler((_, message) =>
                {
                    _logger.LogInformation($"Kafka Log: {message.Message}");
                })
                .Build();

            _logger.LogInformation($"Kafka Producer initialized with Bootstrap Servers: {bootstrapServers}, Topic: {_topic}");
        }

        /// <summary>
        /// שליחת אירוע הגרלה ל-Kafka
        /// </summary>
        public async Task SendLotteryEventAsync(object lotteryData)
        {
            try
            {
                var messageValue = JsonSerializer.Serialize(lotteryData);
                var messageKey = $"lottery-{DateTime.UtcNow.Ticks}";

                var message = new Message<string, string>
                {
                    Key = messageKey,
                    Value = messageValue,
                    Timestamp = new Timestamp(DateTime.UtcNow)
                };

                var deliveryReport = await _producer.ProduceAsync(_topic, message);

                _logger.LogInformation(
                    $"Lottery event sent to Kafka Topic: {deliveryReport.Topic}, " +
                    $"Partition: {deliveryReport.Partition}, " +
                    $"Offset: {deliveryReport.Offset}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending lottery event to Kafka");
                throw;
            }
        }

        /// <summary>
        /// סגירת ה-Producer כשהיישום נסגר
        /// חשוב לשחרר משאבים
        /// </summary>
        public void Dispose()
        {
            _producer?.Dispose();
        }
    }
}
