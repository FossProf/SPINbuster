using Microsoft.Extensions.DependencyInjection;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.AddInterpretation;
using SPINbuster.Application.UseCases.AcceptAiProposal;
using SPINbuster.Application.UseCases.AttachEvidence;
using SPINbuster.Application.UseCases.BuildReportProposalContext;
using SPINbuster.Application.UseCases.CreateReportDraft;
using SPINbuster.Application.UseCases.CaptureFieldNote;
using SPINbuster.Application.UseCases.CreateProject;
using SPINbuster.Application.UseCases.GenerateReportDraftRequest;
using SPINbuster.Application.UseCases.LoadAiProposal;
using SPINbuster.Application.UseCases.LoadAiProposalWorkflowSnapshot;
using SPINbuster.Application.UseCases.LoadInspectionWorkflowSnapshot;
using SPINbuster.Application.UseCases.LoadReportDraftSnapshot;
using SPINbuster.Application.UseCases.PrepareTransactionalSave;
using SPINbuster.Application.UseCases.RejectAiProposal;
using SPINbuster.Application.UseCases.RequestReportDraftProposal;
using SPINbuster.Application.UseCases.StartInspectionSession;

namespace SPINbuster.Application;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddSpinbusterApplication(this IServiceCollection services)
  {
    services.AddScoped<Abstractions.IAiProposalPayloadValidator, Internal.JsonAiProposalPayloadValidator>();
    services.AddScoped<ICommandHandler<CreateProjectCommand, CreateProjectResult>, CreateProjectUseCase>();
    services.AddScoped<ICommandHandler<StartInspectionSessionCommand, StartInspectionSessionResult>, StartInspectionSessionUseCase>();
    services.AddScoped<ICommandHandler<CaptureFieldNoteCommand, CaptureFieldNoteResult>, CaptureFieldNoteUseCase>();
    services.AddScoped<ICommandHandler<AttachEvidenceCommand, AttachEvidenceResult>, AttachEvidenceUseCase>();
    services.AddScoped<ICommandHandler<AddInterpretationCommand, AddInterpretationResult>, AddInterpretationUseCase>();
    services.AddScoped<ICommandHandler<AcceptAiProposalCommand, AcceptAiProposalResult>, AcceptAiProposalUseCase>();
    services.AddScoped<ICommandHandler<CreateReportDraftCommand, CreateReportDraftResult>, CreateReportDraftUseCase>();
    services.AddScoped<ICommandHandler<PrepareTransactionalSaveCommand, PrepareTransactionalSaveResult>, PrepareTransactionalSaveUseCase>();
    services.AddScoped<ICommandHandler<RequestReportDraftProposalCommand, RequestReportDraftProposalResult>, RequestReportDraftProposalUseCase>();
    services.AddScoped<ICommandHandler<RejectAiProposalCommand, RejectAiProposalResult>, RejectAiProposalUseCase>();
    services.AddScoped<IQueryHandler<BuildReportProposalContextQuery, BuildReportProposalContextResult>, BuildReportProposalContextUseCase>();
    services.AddScoped<IQueryHandler<GenerateReportDraftRequestQuery, GenerateReportDraftRequestResult>, GenerateReportDraftRequestUseCase>();
    services.AddScoped<IQueryHandler<LoadAiProposalQuery, LoadAiProposalResult>, LoadAiProposalUseCase>();
    services.AddScoped<IQueryHandler<LoadAiProposalWorkflowSnapshotQuery, LoadAiProposalWorkflowSnapshotResult>, LoadAiProposalWorkflowSnapshotUseCase>();
    services.AddScoped<IQueryHandler<LoadInspectionWorkflowSnapshotQuery, LoadInspectionWorkflowSnapshotResult>, LoadInspectionWorkflowSnapshotUseCase>();
    services.AddScoped<IQueryHandler<LoadReportDraftSnapshotQuery, LoadReportDraftSnapshotResult>, LoadReportDraftSnapshotUseCase>();
    return services;
  }
}
