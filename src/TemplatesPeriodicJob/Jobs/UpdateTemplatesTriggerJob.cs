using System;
using System.Net.Http;
using System.Threading.Tasks;
using Quartz;

namespace TemplatesPeriodicJob.Jobs
{
    [DisallowConcurrentExecution]
    public class UpdateTemplatesTriggerJob : IJob
    {
        private string TemplateProcessorFuncEndpoint => Environment.GetEnvironmentVariable("TEMPLATE_PROCESSOR_FUNCTION_URL");

        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine($"TemplateProcessorFunc call on {TemplateProcessorFuncEndpoint} url");

            using HttpClient client = new();
            var result = await client.GetAsync(TemplateProcessorFuncEndpoint);

            Console.WriteLine($"end TemplateProcessorFunc with {result.StatusCode} status code");
        }
    }
}