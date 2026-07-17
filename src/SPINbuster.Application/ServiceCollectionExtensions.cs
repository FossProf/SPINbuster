using Microsoft.Extensions.DependencyInjection;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.AddInterpretation;
using SPINbuster.Application.UseCases.AcceptAiProposal;
using SPINbuster.Application.UseCases.AddKnowledgeCitation;
using SPINbuster.Application.UseCases.AttachEvidence;
using SPINbuster.Application.UseCases.BeginDocumentImportSession;
using SPINbuster.Application.UseCases.CompleteDocumentImportSession;
using SPINbuster.Application.UseCases.BuildReportProposalContext;
using SPINbuster.Application.UseCases.CreateReportDraft;
using SPINbuster.Application.UseCases.CaptureFieldNote;
using SPINbuster.Application.UseCases.CreateProject;
using SPINbuster.Application.UseCases.GenerateReportDraftRequest;
using SPINbuster.Application.UseCases.ImportDocumentSource;
using SPINbuster.Application.UseCases.LoadAiProposal;
using SPINbuster.Application.UseCases.LoadAiProposalWorkflowSnapshot;
using SPINbuster.Application.UseCases.LoadDocumentCandidates;
using SPINbuster.Application.UseCases.LoadDocumentImportSession;
using SPINbuster.Application.UseCases.LoadDocumentProcessingHistory;
using SPINbuster.Application.UseCases.LoadProjectDocumentWorkflowSnapshot;
using SPINbuster.Application.UseCases.LoadImportedDocumentSource;
using SPINbuster.Application.UseCases.LoadInspectionWorkflowSnapshot;
using SPINbuster.Application.UseCases.LoadKnowledgeDocument;
using SPINbuster.Application.UseCases.LoadKnowledgeNeighborhood;
using SPINbuster.Application.UseCases.LoadKnowledgeRevisionHistory;
using SPINbuster.Application.UseCases.LoadProjectKnowledgeSnapshot;
using SPINbuster.Application.UseCases.LoadReportDraftSnapshot;
using SPINbuster.Application.UseCases.PrepareTransactionalSave;
using SPINbuster.Application.UseCases.RecordDocumentCandidateReview;
using SPINbuster.Application.UseCases.RejectAiProposal;
using SPINbuster.Application.UseCases.RejectDocumentCandidate;
using SPINbuster.Application.UseCases.RegisterKnowledgeDocument;
using SPINbuster.Application.UseCases.RequestDocumentProcessing;
using SPINbuster.Application.UseCases.RequestReportDraftProposal;
using SPINbuster.Application.UseCases.StartInspectionSession;
using SPINbuster.Application.UseCases.AddKnowledgeDocumentRevision;
using SPINbuster.Application.UseCases.CreateKnowledgeRelationship;
using SPINbuster.Application.UseCases.SupersedeKnowledgeRevision;
using SPINbuster.Application.UseCases.VerifyKnowledgeRevision;

namespace SPINbuster.Application;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddSpinbusterApplication(this IServiceCollection services)
  {
    services.AddScoped<Abstractions.IAiProposalPayloadValidator, Internal.JsonAiProposalPayloadValidator>();
    services.AddScoped<ICommandHandler<BeginDocumentImportSessionCommand, BeginDocumentImportSessionResult>, BeginDocumentImportSessionUseCase>();
    services.AddScoped<ICommandHandler<CompleteDocumentImportSessionCommand, CompleteDocumentImportSessionResult>, CompleteDocumentImportSessionUseCase>();
    services.AddScoped<ICommandHandler<CreateProjectCommand, CreateProjectResult>, CreateProjectUseCase>();
    services.AddScoped<ICommandHandler<ImportDocumentSourceCommand, ImportDocumentSourceResult>, ImportDocumentSourceUseCase>();
    services.AddScoped<ICommandHandler<StartInspectionSessionCommand, StartInspectionSessionResult>, StartInspectionSessionUseCase>();
    services.AddScoped<ICommandHandler<CaptureFieldNoteCommand, CaptureFieldNoteResult>, CaptureFieldNoteUseCase>();
    services.AddScoped<ICommandHandler<AttachEvidenceCommand, AttachEvidenceResult>, AttachEvidenceUseCase>();
    services.AddScoped<ICommandHandler<AddInterpretationCommand, AddInterpretationResult>, AddInterpretationUseCase>();
    services.AddScoped<ICommandHandler<AcceptAiProposalCommand, AcceptAiProposalResult>, AcceptAiProposalUseCase>();
    services.AddScoped<ICommandHandler<AddKnowledgeCitationCommand, AddKnowledgeCitationResult>, AddKnowledgeCitationUseCase>();
    services.AddScoped<ICommandHandler<AddKnowledgeDocumentRevisionCommand, AddKnowledgeDocumentRevisionResult>, AddKnowledgeDocumentRevisionUseCase>();
    services.AddScoped<ICommandHandler<CreateReportDraftCommand, CreateReportDraftResult>, CreateReportDraftUseCase>();
    services.AddScoped<ICommandHandler<CreateKnowledgeRelationshipCommand, CreateKnowledgeRelationshipResult>, CreateKnowledgeRelationshipUseCase>();
    services.AddScoped<ICommandHandler<PrepareTransactionalSaveCommand, PrepareTransactionalSaveResult>, PrepareTransactionalSaveUseCase>();
    services.AddScoped<ICommandHandler<RecordDocumentCandidateReviewCommand, RecordDocumentCandidateReviewResult>, RecordDocumentCandidateReviewUseCase>();
    services.AddScoped<ICommandHandler<RegisterKnowledgeDocumentCommand, RegisterKnowledgeDocumentResult>, RegisterKnowledgeDocumentUseCase>();
    services.AddScoped<ICommandHandler<RequestDocumentProcessingCommand, RequestDocumentProcessingResult>, RequestDocumentProcessingUseCase>();
    services.AddScoped<ICommandHandler<RequestReportDraftProposalCommand, RequestReportDraftProposalResult>, RequestReportDraftProposalUseCase>();
    services.AddScoped<ICommandHandler<RejectAiProposalCommand, RejectAiProposalResult>, RejectAiProposalUseCase>();
    services.AddScoped<ICommandHandler<RejectDocumentCandidateCommand, RejectDocumentCandidateResult>, RejectDocumentCandidateUseCase>();
    services.AddScoped<ICommandHandler<SupersedeKnowledgeRevisionCommand, SupersedeKnowledgeRevisionResult>, SupersedeKnowledgeRevisionUseCase>();
    services.AddScoped<ICommandHandler<VerifyKnowledgeRevisionCommand, VerifyKnowledgeRevisionResult>, VerifyKnowledgeRevisionUseCase>();
    services.AddScoped<IQueryHandler<BuildReportProposalContextQuery, BuildReportProposalContextResult>, BuildReportProposalContextUseCase>();
    services.AddScoped<IQueryHandler<GenerateReportDraftRequestQuery, GenerateReportDraftRequestResult>, GenerateReportDraftRequestUseCase>();
    services.AddScoped<IQueryHandler<LoadAiProposalQuery, LoadAiProposalResult>, LoadAiProposalUseCase>();
    services.AddScoped<IQueryHandler<LoadAiProposalWorkflowSnapshotQuery, LoadAiProposalWorkflowSnapshotResult>, LoadAiProposalWorkflowSnapshotUseCase>();
    services.AddScoped<IQueryHandler<LoadDocumentCandidatesQuery, LoadDocumentCandidatesResult>, LoadDocumentCandidatesUseCase>();
    services.AddScoped<IQueryHandler<LoadDocumentImportSessionQuery, LoadDocumentImportSessionResult>, LoadDocumentImportSessionUseCase>();
    services.AddScoped<IQueryHandler<LoadDocumentProcessingHistoryQuery, LoadDocumentProcessingHistoryResult>, LoadDocumentProcessingHistoryUseCase>();
    services.AddScoped<IQueryHandler<LoadProjectDocumentWorkflowSnapshotQuery, LoadProjectDocumentWorkflowSnapshotResult>, LoadProjectDocumentWorkflowSnapshotUseCase>();
    services.AddScoped<IQueryHandler<LoadImportedDocumentSourceQuery, LoadImportedDocumentSourceResult>, LoadImportedDocumentSourceUseCase>();
    services.AddScoped<IQueryHandler<LoadInspectionWorkflowSnapshotQuery, LoadInspectionWorkflowSnapshotResult>, LoadInspectionWorkflowSnapshotUseCase>();
    services.AddScoped<IQueryHandler<LoadKnowledgeDocumentQuery, LoadKnowledgeDocumentResult>, LoadKnowledgeDocumentUseCase>();
    services.AddScoped<IQueryHandler<LoadKnowledgeNeighborhoodQuery, LoadKnowledgeNeighborhoodResult>, LoadKnowledgeNeighborhoodUseCase>();
    services.AddScoped<IQueryHandler<LoadKnowledgeRevisionHistoryQuery, LoadKnowledgeRevisionHistoryResult>, LoadKnowledgeRevisionHistoryUseCase>();
    services.AddScoped<IQueryHandler<LoadProjectKnowledgeSnapshotQuery, LoadProjectKnowledgeSnapshotResult>, LoadProjectKnowledgeSnapshotUseCase>();
    services.AddScoped<IQueryHandler<LoadReportDraftSnapshotQuery, LoadReportDraftSnapshotResult>, LoadReportDraftSnapshotUseCase>();
    return services;
  }
}
