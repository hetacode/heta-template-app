using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Commons.Events;
using Confluent.Kafka;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Minio;

namespace InputProcessorFunc
{
    public class InputProcessorFunc
    {
        private readonly string MinioEndpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT");
        private readonly string MinioAccessKey = Environment.GetEnvironmentVariable("MINIO_ACCESSKEY");
        private readonly string MinioSecretKey = Environment.GetEnvironmentVariable("MINIO_SECRETKEY");
        private readonly IProducer<Null, string> _kafkaProducer;

        public InputProcessorFunc(IProducer<Null, string> kafkaProducer)
        {
            _kafkaProducer = kafkaProducer;

        }

        [Function("InputProcessorFunc")]
        public async Task Run(
            [KafkaTrigger("localhost:9092", "minio-events-inputs", ConsumerGroup="input-processor-group")]
            KafkaMessage ev,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("InputProcessorFunc");
            logger.LogInformation("InputProcessorFunc start");

            var minioEvent = JsonSerializer.Deserialize<S3ObjectEvent>(ev.Value);
            switch (minioEvent.EventName)
            {
                case "s3:ObjectRemoved:Delete":
                    logger.LogInformation($"deleted {minioEvent.Key}");
                    break;
                case "s3:ObjectCreated:Put":
                    {
                        await InputObjectCreated(logger, minioEvent);

                        break;
                    }
                default:
                    logger.LogInformation($"no executor for {minioEvent.EventName}");
                    break;
            }
        }

        private async Task InputObjectCreated(ILogger logger, S3ObjectEvent minioEvent)
        {
            logger.LogInformation($"added {minioEvent.Key} {MinioEndpoint}");
            var storage = new MinioClient(MinioEndpoint, MinioAccessKey, MinioSecretKey);
            var bucketName = minioEvent.Key.Split("/").FirstOrDefault();
            var objectName = minioEvent.Key[(bucketName.Length + 1)..];

            var ys = new YamlDotNet.Serialization.Deserializer();
            await storage.GetObjectAsync(bucketName, objectName, s =>
            {
                using var tr = new StreamReader(s);
                var des = ys.Deserialize<Dictionary<string, object>>(tr);
                if (des.ContainsKey("template"))
                {
                    var outEvent = new InputToProcessEvent(bucketName, objectName);
                    _kafkaProducer.Produce($"input-to-process-{des["template"]}", new Message<Null, string> { Value = JsonSerializer.Serialize(outEvent) });
                    return;
                }
            });
        }
    }

    public record KafkaMessage(int Offset, int Partition, string Topic, DateTime Timestamp, string Value);
    public record S3ObjectEvent(string EventName, string Key);
}