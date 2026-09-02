using CashFlow.Api.Middleware;
using CashFlow.Application;
using CashFlow.Infrastructure;
using CashFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext());

builder.Services.AddApplication();
builder.Services.AddApiInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CashFlow API",
        Version = "v1",
        Description = "Gestão de lançamentos financeiros e saldo diário consolidado."
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // EnsureCreated (em vez de Migrations) para manter a avaliação local simples e permitir os
    // dois providers (Sqlite/Postgres) sem manter dois conjuntos de migrations — ver README,
    // seção "Melhorias futuras", para o caminho de evolução com Migrations versionadas.
    await dbContext.Database.EnsureCreatedAsync();
    await SqlitePragmaConfigurator.ApplyAsync(dbContext);
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Necessário para o WebApplicationFactory<Program> nos testes de integração.
public partial class Program;
