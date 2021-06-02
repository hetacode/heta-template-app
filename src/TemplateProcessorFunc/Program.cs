using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TemplateProcessorFunc.Models;
using Microsoft.EntityFrameworkCore;
using System;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(s =>
    {
        s.AddDbContext<TemplateFuncDbContext>(o => o.UseNpgsql(Environment.GetEnvironmentVariable("POSTGRES_CONNECTIONSTRING")));
    })
    .Build();

host.Run();
