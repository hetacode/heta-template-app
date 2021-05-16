using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InputProcessorFunc
{
    public class InputProcessorFunc
    {
        [Function("InputProcessorFunc")]
        public void Run(
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
                    logger.LogInformation($"added {minioEvent.Key}");
                    break;
                default:
                    logger.LogInformation($"no executor for {minioEvent.EventName}");
                    break;
            }
        }
    }

    public record KafkaMessage(int Offset, int Partition, string Topic, DateTime Timestamp, string Value);
    public record S3ObjectEvent(string EventName, string Key);
}