using System;
using System.Net.Http;
using System.Threading.Tasks;
using Quartz;

namespace HttpPeriodicJob.Jobs
{
    [DisallowConcurrentExecution]
    public class UpdateHttpTriggerJob : IJob
    {
        private string HttpTriggerUrl => Environment.GetEnvironmentVariable("HTTP_TRIGGER_URL");

        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine($"UpdateHttpTriggerJob call {HttpTriggerUrl} url");

            using HttpClient client = new();
            var result = await client.GetAsync(HttpTriggerUrl);

            Console.WriteLine($"end UpdateHttpTriggerJob with {result.StatusCode} status code");
        }
    }
}