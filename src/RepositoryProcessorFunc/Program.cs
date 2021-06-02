using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RepositoryProcessorFunc.Models;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(s =>
    {
        s.AddDbContext<RepositoryFuncDbContext>(o => o.UseNpgsql(Environment.GetEnvironmentVariable("POSTGRES_CONNECTIONSTRING")));
    })
    .Build();

host.Run();
