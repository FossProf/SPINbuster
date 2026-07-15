using Microsoft.Extensions.DependencyInjection;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.CaptureFieldNote;
using SPINbuster.Application.UseCases.CreateProject;
using SPINbuster.Application.UseCases.LoadInspectionWorkflowSnapshot;
using SPINbuster.Application.UseCases.StartInspectionSession;

namespace SPINbuster.Application;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddSpinbusterApplication(this IServiceCollection services)
  {
    services.AddScoped<ICommandHandler<CreateProjectCommand, CreateProjectResult>, CreateProjectUseCase>();
    services.AddScoped<ICommandHandler<StartInspectionSessionCommand, StartInspectionSessionResult>, StartInspectionSessionUseCase>();
    services.AddScoped<ICommandHandler<CaptureFieldNoteCommand, CaptureFieldNoteResult>, CaptureFieldNoteUseCase>();
    services.AddScoped<IQueryHandler<LoadInspectionWorkflowSnapshotQuery, LoadInspectionWorkflowSnapshotResult>, LoadInspectionWorkflowSnapshotUseCase>();
    return services;
  }
}
