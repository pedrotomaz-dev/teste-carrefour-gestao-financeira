using CashFlow.Application;
using CashFlow.Infrastructure;
using CashFlow.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.AddApplication();
builder.Services.AddWorkerInfrastructure(builder.Configuration);

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ConsolidationDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await SqlitePragmaConfigurator.ApplyAsync(dbContext);
}

await host.RunAsync();
