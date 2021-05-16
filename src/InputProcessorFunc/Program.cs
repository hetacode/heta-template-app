using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Configuration;
using Confluent.Kafka;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(s => {
        // TODO: env variables
        var kafkaBuilder = new ProducerBuilder<Null, string>(new ProducerConfig{BootstrapServers="localhost:9092"}).Build();
        s.AddSingleton(kafkaBuilder);
    })
    .Build();

host.Run();