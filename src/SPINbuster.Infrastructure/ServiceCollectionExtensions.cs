using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Repositories;
using SPINbuster.Infrastructure.Persistence;
using SPINbuster.Infrastructure.Repositories;
using SPINbuster.Infrastructure.Services;

namespace SPINbuster.Infrastructure;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddSpinbusterSqliteInfrastructure(
    this IServiceCollection services,
    string connectionString)
  {
    services.AddDbContext<SpinbusterDbContext>(options => options.UseSqlite(connectionString));
    services.AddScoped<SqliteAuditRecorder>();
    services.AddScoped<IAuditRecorder>(serviceProvider => serviceProvider.GetRequiredService<SqliteAuditRecorder>());
    services.AddScoped<IUnitOfWork, SqliteUnitOfWork>();
    services.AddScoped<IProjectRepository, SqliteProjectRepository>();
    services.AddScoped<IInspectionSessionRepository, SqliteInspectionSessionRepository>();
    services.AddScoped<IReportRepository, SqliteReportRepository>();
    services.AddScoped<ISaveTransactionRepository, SqliteSaveTransactionRepository>();
    services.AddScoped<IAuditEventQueryRepository, SqliteAuditEventQueryRepository>();
    services.AddScoped<IContextManifestRepository, SqliteContextManifestRepository>();
    services.AddScoped<IModelRunRepository, SqliteModelRunRepository>();
    services.AddScoped<IAiProposalRepository, SqliteAiProposalRepository>();
    return services;
  }
}
