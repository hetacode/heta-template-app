using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Confluent.Kafka;
using System;

string KafkaBrokers = Environment.GetEnvironmentVariable("KAFKA_BROKERS");

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(s =>
    {
        var kafkaBuilder = new ProducerBuilder<Null, string>(new ProducerConfig { BootstrapServers = KafkaBrokers }).Build();
        s.AddSingleton(kafkaBuilder);
    })
    .Build();

host.Run();