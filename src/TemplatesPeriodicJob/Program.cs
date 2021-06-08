using System;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using TemplatesPeriodicJob.Jobs;

var isKuberenetesMode = bool.Parse(Environment.GetEnvironmentVariable("KUBERNETES_MODE") ?? "false");


if (isKuberenetesMode)
{
    // TODO: onetime executor - servcie should be run kubernetes cron job
    // https://kubernetes.io/docs/concepts/workloads/controllers/cron-jobs/
}
else
{
    Console.WriteLine("cron mode");
    await CronScheduler();
}


async Task CronScheduler()
{
    var isRunning = true;
    StdSchedulerFactory factory = new();
    var scheduler = await factory.GetScheduler();

    await scheduler.Start();

    var job = JobBuilder.Create<UpdateTemplatesTriggerJob>().Build();
    var trigger = TriggerBuilder.Create().WithCronSchedule("0 0/1 * * * ?").StartNow().Build();

    await scheduler.ScheduleJob(job, trigger);

    Console.CancelKeyPress += (s, e) =>
    {
        isRunning = false;
        e.Cancel = true;
    };

    while (isRunning)
    {
        await Task.Delay(100);
    }
    await scheduler.Shutdown();
    Console.WriteLine("service stopped");
}