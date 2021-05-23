using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ProfilePageGeneratorFunc
{
    public static class ProfilePageGeneratorFunc
    {
        [Function("ProfilePageGeneratorFunc")]
        public static void Run(
            [KafkaTrigger("KAFKA_BROKERS", "input-to-process-profile-page", ConsumerGroup="input-processor-group")]
            KafkaMessage ev,
            FunctionContext context)
        {
            var logger = context.GetLogger("ProfilePageGeneratorFunc");
            logger.LogInformation($"Start ProfilePageGeneratorFunc");
        }
    }

    public record KafkaMessage(int Offset, int Partition, string Topic, DateTime Timestamp, string Value);
}
