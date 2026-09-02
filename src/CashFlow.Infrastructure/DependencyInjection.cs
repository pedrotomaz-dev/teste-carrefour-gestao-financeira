using CashFlow.Application.Interfaces;
using CashFlow.Infrastructure.Common;
using CashFlow.Infrastructure.Messaging;
using CashFlow.Infrastructure.Messaging.InMemory;
using CashFlow.Infrastructure.Messaging.RabbitMq;
using CashFlow.Infrastructure.Outbox;
using CashFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CashFlow.Infrastructure;

public static class DependencyInjection
{
    private const string SqliteProvider = "Sqlite";
    private const string PostgresProvider = "Postgres";
    private const string RabbitMqProvider = "RabbitMq";

    /// <summary>Registra a infraestrutura do CashFlow.Api: persistência, outbox e o publisher de mensageria.</summary>
    public static IServiceCollection AddApiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase<AppDbContext>(configuration);
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddCommon(configuration);
        services.AddMessagingPublisher(configuration);

        services.Configure<OutboxDispatcherOptions>(configuration.GetSection(OutboxDispatcherOptions.SectionName));
        services.AddHostedService<OutboxDispatcherHostedService>();

        // Modo "single-process": sem RabbitMq/Worker separado, a própria Api também consome os
        // eventos da fila em memória para consolidar o saldo — ver docs/architecture.md.
        if (IsInMemoryMessaging(configuration))
        {
            services.AddScoped<IConsolidationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
            services.AddHostedService<InMemoryConsolidationConsumer>();
        }

        return services;
    }

    /// <summary>Registra a infraestrutura do CashFlow.ConsolidationWorker: persistência própria + consumer RabbitMQ.</summary>
    public static IServiceCollection AddWorkerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase<ConsolidationDbContext>(configuration);
        services.AddScoped<IConsolidationDbContext>(sp => sp.GetRequiredService<ConsolidationDbContext>());

        services.AddCommon(configuration);

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddHostedService<RabbitMqConsolidationConsumer>();

        return services;
    }

    private static IServiceCollection AddCommon(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        return services;
    }

    private static IServiceCollection AddDatabase<TContext>(this IServiceCollection services, IConfiguration configuration)
        where TContext : DbContext
    {
        var provider = configuration["Database:Provider"] ?? SqliteProvider;

        services.AddDbContext<TContext>(options =>
        {
            if (string.Equals(provider, PostgresProvider, StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = configuration.GetConnectionString(PostgresProvider)
                    ?? throw new InvalidOperationException("ConnectionStrings:Postgres não configurada.");
                options.UseNpgsql(connectionString);
            }
            else
            {
                var connectionString = configuration.GetConnectionString(SqliteProvider) ?? "Data Source=cashflow.db";
                options.UseSqlite(connectionString);
            }
        });

        return services;
    }

    private static IServiceCollection AddMessagingPublisher(this IServiceCollection services, IConfiguration configuration)
    {
        if (IsInMemoryMessaging(configuration))
        {
            services.AddSingleton<InMemoryMessageBus>();
            services.AddSingleton<IIntegrationEventPublisher, InMemoryIntegrationEventPublisher>();
        }
        else
        {
            services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
            services.AddSingleton<RabbitMqConnectionProvider>();
            services.AddSingleton<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();
        }

        return services;
    }

    private static bool IsInMemoryMessaging(IConfiguration configuration)
    {
        var provider = configuration[$"{MessagingOptions.SectionName}:Provider"] ?? "InMemory";
        return !string.Equals(provider, RabbitMqProvider, StringComparison.OrdinalIgnoreCase);
    }
}
