using Confluent.Kafka;
using Serilog;
using System.Text.Json;

// הגדרת Serilog ללוגינג
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "Logs/consumer-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

var logger = Log.ForContext<Program>();

try
{
    logger.Information("Chinese Auction Event Consumer starting...");

    // הגדרות ה-Kafka Consumer
    var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "kafka:29092";
    var kafkaTopic = Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? "purchase-events";
    var kafkaGroupId = Environment.GetEnvironmentVariable("KAFKA_GROUP_ID") ?? "chinese-auction-consumer";

    var config = new ConsumerConfig
    {
        BootstrapServers = kafkaBootstrapServers,  // כתובת Kafka
        GroupId = kafkaGroupId,  // קבוצה של consumers
        AutoOffsetReset = AutoOffsetReset.Earliest,  // קרא מתחילת הtopic אם לא יש offset
        EnableAutoCommit = true,  // commit offset באופן אוטומטי
        StatisticsIntervalMs = 5000  // דיווח סטטיסטיקה כל 5 שניות
    };

    // יצירת Consumer עם ConsumerBuilder
    using (var consumer = new ConsumerBuilder<string, string>(config)
        .SetErrorHandler((_, e) =>
        {
            logger.Error($"Kafka Error: {e.Reason}");
        })
        .SetLogHandler((_, message) =>
        {
            logger.Debug($"Kafka Log: {message.Message}");
        })
        .SetStatisticsHandler((_, json) =>
        {
            logger.Debug($"Kafka Statistics: {json}");
        })
        .Build())
    {
        // הירשום ל-Topic מהסביבה / ברירת מחדל
        consumer.Subscribe(new[] { kafkaTopic });
        logger.Information("Consumer subscribed to topic: {Topic}", kafkaTopic);

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;  // prevent the process from terminating
            cts.Cancel();
        };

        try
        {
            while (true)
            {
                try
                {
                    // קביעת timeout כדי לא להשהות לנצח
                    var cr = consumer.Consume(cts.Token);

                    if (cr.IsPartitionEOF)
                    {
                        logger.Information(
                            $"Reached end of partition: " +
                            $"topic {cr.Topic}, partition {cr.Partition}, offset {cr.Offset}.");
                        continue;
                    }

                    logger.Information(
                        $"Message received - Offset: {cr.Offset}, Partition: {cr.Partition}");
                    logger.Information($"Key: {cr.Message.Key}");

                    // ניתוח ה-JSON
                    try
                    {
                        var jsonDocument = JsonDocument.Parse(cr.Message.Value);
                        var root = jsonDocument.RootElement;

                        // helper to read string properties with fallback names
                        static string GetStringProp(JsonElement el, params string[] names)
                        {
                            foreach (var n in names)
                            {
                                if (el.TryGetProperty(n, out var prop) && prop.ValueKind != JsonValueKind.Null)
                                {
                                    try { return prop.GetString(); } catch { }
                                }
                            }
                            return null;
                        }

                        var eventType = GetStringProp(root, "EventType", "eventType", "type") ?? "UNKNOWN";

                        logger.Information($"Event Type: {eventType}");
                        logger.Information($"Message Value: {cr.Message.Value}");

                        // עיבוד לפי סוג האירוע
                        switch (eventType.ToUpperInvariant())
                        {
                            case "PURCHASE_CREATED":
                            case "PURCHASE":
                                HandlePurchaseEvent(cr.Message.Value, logger);
                                break;

                            case "LOTTERY_WINNER":
                            case "LOTTERY":
                                HandleLotteryEvent(cr.Message.Value, logger);
                                break;

                            default:
                                logger.Warning($"Unknown event type: {eventType}");
                                break;
                        }
                    }
                    catch (JsonException je)
                    {
                        logger.Error(je, "Failed to parse JSON message");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException e)
                {
                    logger.Error(e, $"Consume error: {e.Error.Reason}");
                }
            }
        }
        finally
        {
            consumer.Close();
            logger.Information("Consumer closed");
        }
    }

    logger.Information("Consumer shutdown gracefully");
}
catch (Exception ex)
{
    logger.Fatal(ex, "Consumer crashed");
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>
/// עיבוד אירוע רכישה
/// כאן אפשר להוסיף לוגיקה נוספת:
/// - שמירה לקובץ
/// - שליחה לDB
/// - עדכון לאפליקציה חיצונית
/// וכו'
/// </summary>
static void HandlePurchaseEvent(string messageValue, Serilog.ILogger logger)
{
    logger.Information("=== PURCHASE EVENT ===");
    logger.Information($"Full Event: {messageValue}");

    try
    {
        var doc = JsonDocument.Parse(messageValue);
        var root = doc.RootElement;

        // safe helpers
        static string GetString(JsonElement el, params string[] names)
        {
            foreach (var n in names)
            {
                if (el.TryGetProperty(n, out var p) && p.ValueKind != JsonValueKind.Null)
                {
                    try { return p.GetString(); } catch { }
                }
            }
            return null;
        }

        static int? GetInt(JsonElement el, params string[] names)
        {
            foreach (var n in names)
            {
                if (el.TryGetProperty(n, out var p) && p.ValueKind != JsonValueKind.Null)
                {
                    if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var i)) return i;
                    if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out var j)) return j;
                }
            }
            return null;
        }

        try
        {
            var purchaseId = GetInt(root, "PurchaseId", "purchaseId");

            var userEl = root.TryGetProperty("User", out var u) ? u : (root.TryGetProperty("user", out var uu) ? uu : default(JsonElement));
            var userId = userEl.ValueKind != JsonValueKind.Undefined ? GetInt(userEl, "Id", "id") : null;
            var firstName = userEl.ValueKind != JsonValueKind.Undefined ? GetString(userEl, "FirstName", "firstName") : null;
            var lastName = userEl.ValueKind != JsonValueKind.Undefined ? GetString(userEl, "LastName", "lastName") : null;
            var userEmail = userEl.ValueKind != JsonValueKind.Undefined ? GetString(userEl, "Email", "email") : null;

            var giftEl = root.TryGetProperty("Gift", out var g) ? g : (root.TryGetProperty("gift", out var gg) ? gg : default(JsonElement));
            var giftName = giftEl.ValueKind != JsonValueKind.Undefined ? GetString(giftEl, "Name", "name") : null;

            var purchaseDate = GetString(root, "PurchaseDate", "purchaseDate", "createdAt", "CreatedAt");

            logger.Information($"Purchase ID: {purchaseId?.ToString() ?? "<missing>"}");
            logger.Information($"User: {(firstName ?? "?")} {(lastName ?? "?")} ({userEmail ?? "?"})");
            logger.Information($"Gift: {giftName ?? "?"}");
            logger.Information($"Purchase Date: {purchaseDate ?? "?"}");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error processing purchase event");
        }
    }
    catch (Exception ex)
    {
        logger.Error(ex, "Error processing purchase event");
    }
}

/// <summary>
/// עיבוד אירוע הגרלה (זוכה)
/// כאן אפשר להוסיף לוגיקה נוספת:
/// - שליחת אימייל לזוכה
/// - אפדייט סטטיסטיקה
/// - הודעה לאדמין
/// וכו'
/// </summary>
static void HandleLotteryEvent(string messageValue, Serilog.ILogger logger)
{
    logger.Information("=== LOTTERY WINNER EVENT ===");
    logger.Information($"Full Event: {messageValue}");

    try
    {
        var doc = JsonDocument.Parse(messageValue);
        var root = doc.RootElement;

        var winnerId = root.GetProperty("Winner").GetProperty("Id").GetInt32();
        var winnerName = $"{root.GetProperty("Winner").GetProperty("FirstName").GetString()} " +
                        $"{root.GetProperty("Winner").GetProperty("LastName").GetString()}";
        var winnerEmail = root.GetProperty("Winner").GetProperty("Email").GetString();
        var giftName = root.GetProperty("Gift").GetProperty("Name").GetString();
        var totalParticipants = root.GetProperty("TotalParticipants").GetInt32();

        logger.Information($"Winner ID: {winnerId}");
        logger.Information($"Winner: {winnerName} ({winnerEmail})");
        logger.Information($"Gift: {giftName}");
        logger.Information($"Total Participants: {totalParticipants}");
        logger.Information($"Congratulations to {winnerName}! 🎉");
    }
    catch (Exception ex)
    {
        logger.Error(ex, "Error processing lottery event");
    }
}
