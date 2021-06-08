using System;
using System.Threading.Tasks;
using Quartz;

namespace TemplatesPeriodicJob.Jobs
{
    public class UpdateTemplatesTriggerJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("job call");
            
            return Task.CompletedTask;
        }
    }
}