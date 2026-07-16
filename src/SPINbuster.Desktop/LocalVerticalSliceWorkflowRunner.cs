using SPINbuster.Application;
using SPINbuster.Application.UseCases.AcceptAiProposal;
using SPINbuster.Application.UseCases.AddInterpretation;
using SPINbuster.Application.UseCases.AttachEvidence;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.CaptureFieldNote;
using SPINbuster.Application.UseCases.CreateProject;
using SPINbuster.Application.UseCases.CreateReportDraft;
using SPINbuster.Application.UseCases.GenerateReportDraftRequest;
using SPINbuster.Application.UseCases.LoadAiProposalWorkflowSnapshot;
using SPINbuster.Application.UseCases.LoadInspectionWorkflowSnapshot;
using SPINbuster.Application.UseCases.LoadReportDraftSnapshot;
using SPINbuster.Application.UseCases.RejectAiProposal;
using SPINbuster.Application.UseCases.RequestReportDraftProposal;
using SPINbuster.Application.UseCases.StartInspectionSession;
using SPINbuster.Domain;

namespace SPINbuster.Desktop;

public sealed class LocalVerticalSliceWorkflowRunner
{
  private readonly ICommandHandler<AcceptAiProposalCommand, AcceptAiProposalResult> _acceptAiProposal;
  private readonly ICommandHandler<AddInterpretationCommand, AddInterpretationResult> _addInterpretation;
  private readonly ICommandHandler<AttachEvidenceCommand, AttachEvidenceResult> _attachEvidence;
  private readonly ICommandHandler<CaptureFieldNoteCommand, CaptureFieldNoteResult> _captureFieldNote;
  private readonly ICommandHandler<CreateProjectCommand, CreateProjectResult> _createProject;
  private readonly ICommandHandler<CreateReportDraftCommand, CreateReportDraftResult> _createReportDraft;
  private readonly IQueryHandler<GenerateReportDraftRequestQuery, GenerateReportDraftRequestResult> _generateReportDraftRequest;
  private readonly IQueryHandler<LoadAiProposalWorkflowSnapshotQuery, LoadAiProposalWorkflowSnapshotResult> _loadAiProposalWorkflowSnapshot;
  private readonly IQueryHandler<LoadInspectionWorkflowSnapshotQuery, LoadInspectionWorkflowSnapshotResult> _loadWorkflowSnapshot;
  private readonly IQueryHandler<LoadReportDraftSnapshotQuery, LoadReportDraftSnapshotResult> _loadReportDraftSnapshot;
  private readonly ICommandHandler<RejectAiProposalCommand, RejectAiProposalResult> _rejectAiProposal;
  private readonly ICommandHandler<RequestReportDraftProposalCommand, RequestReportDraftProposalResult> _requestReportDraftProposal;
  private readonly DesktopWorkflowSettings _settings;
  private readonly ICommandHandler<StartInspectionSessionCommand, StartInspectionSessionResult> _startInspectionSession;

  public LocalVerticalSliceWorkflowRunner(
    ICommandHandler<AcceptAiProposalCommand, AcceptAiProposalResult> acceptAiProposal,
    ICommandHandler<CreateProjectCommand, CreateProjectResult> createProject,
    ICommandHandler<StartInspectionSessionCommand, StartInspectionSessionResult> startInspectionSession,
    ICommandHandler<CaptureFieldNoteCommand, CaptureFieldNoteResult> captureFieldNote,
    ICommandHandler<AttachEvidenceCommand, AttachEvidenceResult> attachEvidence,
    ICommandHandler<AddInterpretationCommand, AddInterpretationResult> addInterpretation,
    IQueryHandler<GenerateReportDraftRequestQuery, GenerateReportDraftRequestResult> generateReportDraftRequest,
    ICommandHandler<CreateReportDraftCommand, CreateReportDraftResult> createReportDraft,
    ICommandHandler<RequestReportDraftProposalCommand, RequestReportDraftProposalResult> requestReportDraftProposal,
    ICommandHandler<RejectAiProposalCommand, RejectAiProposalResult> rejectAiProposal,
    IQueryHandler<LoadAiProposalWorkflowSnapshotQuery, LoadAiProposalWorkflowSnapshotResult> loadAiProposalWorkflowSnapshot,
    IQueryHandler<LoadInspectionWorkflowSnapshotQuery, LoadInspectionWorkflowSnapshotResult> loadWorkflowSnapshot,
    IQueryHandler<LoadReportDraftSnapshotQuery, LoadReportDraftSnapshotResult> loadReportDraftSnapshot,
    DesktopWorkflowSettings settings)
  {
    _acceptAiProposal = acceptAiProposal;
    _createProject = createProject;
    _startInspectionSession = startInspectionSession;
    _captureFieldNote = captureFieldNote;
    _attachEvidence = attachEvidence;
    _addInterpretation = addInterpretation;
    _generateReportDraftRequest = generateReportDraftRequest;
    _createReportDraft = createReportDraft;
    _requestReportDraftProposal = requestReportDraftProposal;
    _rejectAiProposal = rejectAiProposal;
    _loadAiProposalWorkflowSnapshot = loadAiProposalWorkflowSnapshot;
    _loadWorkflowSnapshot = loadWorkflowSnapshot;
    _loadReportDraftSnapshot = loadReportDraftSnapshot;
    _settings = settings;
  }

  public async Task<LocalVerticalSliceWorkflowResult> RunAsync(CancellationToken cancellationToken = default)
  {
    var createdProject = await _createProject.HandleAsync(
      new CreateProjectCommand(_settings.ProjectName),
      cancellationToken);
    var startedInspectionSession = await _startInspectionSession.HandleAsync(
      new StartInspectionSessionCommand(createdProject.ProjectId, _settings.SessionName),
      cancellationToken);
    var capturedFieldNote = await _captureFieldNote.HandleAsync(
      new CaptureFieldNoteCommand(startedInspectionSession.InspectionSessionId, _settings.FieldNoteText),
      cancellationToken);
    var attachedEvidence = await _attachEvidence.HandleAsync(
      new AttachEvidenceCommand(
        startedInspectionSession.InspectionSessionId,
        _settings.EvidenceFileName,
        _settings.EvidenceMediaType,
        _settings.EvidenceStorageKey,
        _settings.EvidenceChecksum),
      cancellationToken);
    var addedInterpretation = await _addInterpretation.HandleAsync(
      new AddInterpretationCommand(
        startedInspectionSession.InspectionSessionId,
        attachedEvidence.EvidenceAttachmentId,
        _settings.InterpretationSummary),
      cancellationToken);
    var draftContext = await _generateReportDraftRequest.HandleAsync(
      new GenerateReportDraftRequestQuery(
        createdProject.ProjectId,
        startedInspectionSession.InspectionSessionId,
        _settings.DraftTitle),
      cancellationToken);
    var createdReportDraft = await _createReportDraft.HandleAsync(
      new CreateReportDraftCommand(
        new OperationId(_settings.ReportOperationId),
        createdProject.ProjectId,
        startedInspectionSession.InspectionSessionId,
        _settings.DraftTitle,
        [capturedFieldNote.FieldNoteId],
        [attachedEvidence.EvidenceAttachmentId],
        [
          new CreateReportDraftSectionInput(_settings.DraftSummaryHeading, _settings.DraftSummaryContent),
          new CreateReportDraftSectionInput(_settings.DraftObservationHeading, _settings.DraftObservationContent),
        ]),
      cancellationToken);
    var requestedAiProposal = await _requestReportDraftProposal.HandleAsync(
      new RequestReportDraftProposalCommand(
        new OperationId(_settings.ProposalOperationId),
        createdReportDraft.ReportId,
        _settings.ProposalPromptPackageId,
        _settings.ProposalPromptPackageVersion,
        _settings.ProposalTemperature),
      cancellationToken);
    var replayedAiProposalRequest = await _requestReportDraftProposal.HandleAsync(
      new RequestReportDraftProposalCommand(
        new OperationId(_settings.ProposalOperationId),
        createdReportDraft.ReportId,
        _settings.ProposalPromptPackageId,
        _settings.ProposalPromptPackageVersion,
        _settings.ProposalTemperature),
      cancellationToken);
    var persistedAiProposalSnapshot = await _loadAiProposalWorkflowSnapshot.HandleAsync(
      new LoadAiProposalWorkflowSnapshotQuery(
        requestedAiProposal.ModelRunId,
        requestedAiProposal.ProposalId),
      cancellationToken);
    var persistedReportSnapshotBeforeReview = await _loadReportDraftSnapshot.HandleAsync(
      new LoadReportDraftSnapshotQuery(createdReportDraft.ReportId),
      cancellationToken);

    AcceptAiProposalResult? acceptedAiProposal = null;
    RejectAiProposalResult? rejectedAiProposal = null;
    if (persistedAiProposalSnapshot.Proposal?.Status == ProposalStatus.ReadyForReview)
    {
      switch (_settings.ProposalReviewAction)
      {
        case DesktopAiReviewAction.HumanAccept:
          acceptedAiProposal = await _acceptAiProposal.HandleAsync(
            new AcceptAiProposalCommand(
              persistedAiProposalSnapshot.Proposal.ProposalId,
              _settings.ProposalReviewNotes),
            cancellationToken);
          break;

        case DesktopAiReviewAction.Reject:
          rejectedAiProposal = await _rejectAiProposal.HandleAsync(
            new RejectAiProposalCommand(
              persistedAiProposalSnapshot.Proposal.ProposalId,
              _settings.ProposalReviewNotes),
            cancellationToken);
          break;
      }
    }

    var reviewedAiProposalSnapshot = await _loadAiProposalWorkflowSnapshot.HandleAsync(
      new LoadAiProposalWorkflowSnapshotQuery(
        requestedAiProposal.ModelRunId,
        requestedAiProposal.ProposalId),
      cancellationToken);
    var persistedInspectionSnapshot = await _loadWorkflowSnapshot.HandleAsync(
      new LoadInspectionWorkflowSnapshotQuery(
        createdProject.ProjectId,
        startedInspectionSession.InspectionSessionId),
      cancellationToken);
    var persistedReportSnapshot = await _loadReportDraftSnapshot.HandleAsync(
      new LoadReportDraftSnapshotQuery(createdReportDraft.ReportId),
      cancellationToken);

    return new LocalVerticalSliceWorkflowResult(
      createdProject,
      startedInspectionSession,
      capturedFieldNote,
      attachedEvidence,
      addedInterpretation,
      draftContext,
      createdReportDraft,
      requestedAiProposal,
      replayedAiProposalRequest,
      persistedAiProposalSnapshot,
      persistedReportSnapshotBeforeReview,
      acceptedAiProposal,
      rejectedAiProposal,
      reviewedAiProposalSnapshot,
      persistedInspectionSnapshot,
      persistedReportSnapshot);
  }
}
