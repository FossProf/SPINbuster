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
    services.AddScoped<ISpinbusterDatabaseMigrator, SqliteDatabaseMigrator>();
    services.AddScoped<SqliteAuditRecorder>();
    services.AddScoped<IAuditRecorder>(serviceProvider => serviceProvider.GetRequiredService<SqliteAuditRecorder>());
    services.AddSingleton<IDeferredReferenceHandler, KnowledgeDocumentDeferredReferenceHandler>();
    services.AddScoped<SqliteUnitOfWork>();
    services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<SqliteUnitOfWork>());
    services.AddScoped<IProjectRepository, SqliteProjectRepository>();
    services.AddScoped<IInspectionSessionRepository, SqliteInspectionSessionRepository>();
    services.AddScoped<IReportRepository, SqliteReportRepository>();
    services.AddScoped<ISaveTransactionRepository, SqliteSaveTransactionRepository>();
    services.AddScoped<IAuditEventQueryRepository, SqliteAuditEventQueryRepository>();
    services.AddScoped<IContextManifestRepository, SqliteContextManifestRepository>();
    services.AddScoped<IModelRunRepository, SqliteModelRunRepository>();
    services.AddScoped<IAiProposalRepository, SqliteAiProposalRepository>();
    services.AddScoped<IKnowledgeDocumentRepository, SqliteKnowledgeDocumentRepository>();
    services.AddScoped<IKnowledgeRevisionRepository, SqliteKnowledgeRevisionRepository>();
    services.AddScoped<IKnowledgeRelationshipRepository, SqliteKnowledgeRelationshipRepository>();
    services.AddScoped<IKnowledgeCitationRepository, SqliteKnowledgeCitationRepository>();
    services.AddScoped<IStorageObjectRepository, SqliteStorageObjectRepository>();
    services.AddScoped<IImportedDocumentSourceRepository, SqliteImportedDocumentSourceRepository>();
    services.AddScoped<IDocumentImportSessionRepository, SqliteDocumentImportSessionRepository>();
    services.AddScoped<IDocumentProcessingAttemptRepository, SqliteDocumentProcessingAttemptRepository>();
    services.AddScoped<IDocumentCandidateRepository, SqliteDocumentCandidateRepository>();
    services.AddScoped<IParserRunRepository, SqliteParserRunRepository>();
    services.AddScoped<IFragmentCandidateRepository, SqliteFragmentCandidateRepository>();
    return services;
  }
}
