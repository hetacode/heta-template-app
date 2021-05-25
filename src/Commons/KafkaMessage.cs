using System;

namespace Commons
{
        public record KafkaMessage(int Offset, int Partition, string Topic, DateTime Timestamp, string Value);
}