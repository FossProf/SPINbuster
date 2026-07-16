using SPINbuster.Application;
using SPINbuster.Application.Contracts;
using SPINbuster.Application.UseCases.AcceptAiProposal;
using SPINbuster.Application.UseCases.AddInterpretation;
using SPINbuster.Application.UseCases.AddKnowledgeCitation;
using SPINbuster.Application.UseCases.AddKnowledgeDocumentRevision;
using SPINbuster.Application.UseCases.AttachEvidence;
using SPINbuster.Application.UseCases.CaptureFieldNote;
using SPINbuster.Application.UseCases.CreateKnowledgeRelationship;
using SPINbuster.Application.UseCases.CreateProject;
using SPINbuster.Application.UseCases.CreateReportDraft;
using SPINbuster.Application.UseCases.GenerateReportDraftRequest;
using SPINbuster.Application.UseCases.LoadAiProposalWorkflowSnapshot;
using SPINbuster.Application.UseCases.LoadInspectionWorkflowSnapshot;
using SPINbuster.Application.UseCases.LoadProjectKnowledgeSnapshot;
using SPINbuster.Application.UseCases.LoadReportDraftSnapshot;
using SPINbuster.Application.UseCases.RejectAiProposal;
using SPINbuster.Application.UseCases.RegisterKnowledgeDocument;
using SPINbuster.Application.UseCases.RequestReportDraftProposal;
using SPINbuster.Application.UseCases.StartInspectionSession;
using SPINbuster.Application.UseCases.SupersedeKnowledgeRevision;
using SPINbuster.Domain;

namespace SPINbuster.Desktop;

public sealed class LocalVerticalSliceWorkflowRunner
{
  private readonly ICommandHandler<AcceptAiProposalCommand, AcceptAiProposalResult> _acceptAiProposal;
  private readonly ICommandHandler<AddInterpretationCommand, AddInterpretationResult> _addInterpretation;
  private readonly ICommandHandler<AddKnowledgeCitationCommand, AddKnowledgeCitationResult> _addKnowledgeCitation;
  private readonly ICommandHandler<AddKnowledgeDocumentRevisionCommand, AddKnowledgeDocumentRevisionResult> _addKnowledgeDocumentRevision;
  private readonly ICommandHandler<AttachEvidenceCommand, AttachEvidenceResult> _attachEvidence;
  private readonly ICommandHandler<CaptureFieldNoteCommand, CaptureFieldNoteResult> _captureFieldNote;
  private readonly ICommandHandler<CreateKnowledgeRelationshipCommand, CreateKnowledgeRelationshipResult> _createKnowledgeRelationship;
  private readonly ICommandHandler<CreateProjectCommand, CreateProjectResult> _createProject;
  private readonly ICommandHandler<CreateReportDraftCommand, CreateReportDraftResult> _createReportDraft;
  private readonly IQueryHandler<GenerateReportDraftRequestQuery, GenerateReportDraftRequestResult> _generateReportDraftRequest;
  private readonly IQueryHandler<LoadAiProposalWorkflowSnapshotQuery, LoadAiProposalWorkflowSnapshotResult> _loadAiProposalWorkflowSnapshot;
  private readonly IQueryHandler<LoadInspectionWorkflowSnapshotQuery, LoadInspectionWorkflowSnapshotResult> _loadWorkflowSnapshot;
  private readonly IQueryHandler<LoadProjectKnowledgeSnapshotQuery, LoadProjectKnowledgeSnapshotResult> _loadProjectKnowledgeSnapshot;
  private readonly IQueryHandler<LoadReportDraftSnapshotQuery, LoadReportDraftSnapshotResult> _loadReportDraftSnapshot;
  private readonly ICommandHandler<RegisterKnowledgeDocumentCommand, RegisterKnowledgeDocumentResult> _registerKnowledgeDocument;
  private readonly ICommandHandler<RejectAiProposalCommand, RejectAiProposalResult> _rejectAiProposal;
  private readonly ICommandHandler<RequestReportDraftProposalCommand, RequestReportDraftProposalResult> _requestReportDraftProposal;
  private readonly DesktopWorkflowSettings _settings;
  private readonly ICommandHandler<StartInspectionSessionCommand, StartInspectionSessionResult> _startInspectionSession;
  private readonly ICommandHandler<SupersedeKnowledgeRevisionCommand, SupersedeKnowledgeRevisionResult> _supersedeKnowledgeRevision;

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
    ICommandHandler<RegisterKnowledgeDocumentCommand, RegisterKnowledgeDocumentResult> registerKnowledgeDocument,
    ICommandHandler<AddKnowledgeDocumentRevisionCommand, AddKnowledgeDocumentRevisionResult> addKnowledgeDocumentRevision,
    ICommandHandler<SupersedeKnowledgeRevisionCommand, SupersedeKnowledgeRevisionResult> supersedeKnowledgeRevision,
    ICommandHandler<CreateKnowledgeRelationshipCommand, CreateKnowledgeRelationshipResult> createKnowledgeRelationship,
    ICommandHandler<AddKnowledgeCitationCommand, AddKnowledgeCitationResult> addKnowledgeCitation,
    IQueryHandler<LoadProjectKnowledgeSnapshotQuery, LoadProjectKnowledgeSnapshotResult> loadProjectKnowledgeSnapshot,
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
    _registerKnowledgeDocument = registerKnowledgeDocument;
    _addKnowledgeDocumentRevision = addKnowledgeDocumentRevision;
    _supersedeKnowledgeRevision = supersedeKnowledgeRevision;
    _createKnowledgeRelationship = createKnowledgeRelationship;
    _addKnowledgeCitation = addKnowledgeCitation;
    _loadProjectKnowledgeSnapshot = loadProjectKnowledgeSnapshot;
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

    var reviewedAiProposalSnapshotBeforeKnowledge = await _loadAiProposalWorkflowSnapshot.HandleAsync(
      new LoadAiProposalWorkflowSnapshotQuery(
        requestedAiProposal.ModelRunId,
        requestedAiProposal.ProposalId),
      cancellationToken);
    var persistedInspectionSnapshot = await _loadWorkflowSnapshot.HandleAsync(
      new LoadInspectionWorkflowSnapshotQuery(
        createdProject.ProjectId,
        startedInspectionSession.InspectionSessionId),
      cancellationToken);
    var persistedReportSnapshotBeforeKnowledge = await _loadReportDraftSnapshot.HandleAsync(
      new LoadReportDraftSnapshotQuery(createdReportDraft.ReportId),
      cancellationToken);

    var registeredSpecificationDocument = await _registerKnowledgeDocument.HandleAsync(
      new RegisterKnowledgeDocumentCommand(
        createdProject.ProjectId,
        KnowledgeDocumentType.Specification,
        _settings.SpecificationTitle,
        _settings.SpecificationExternalReference,
        _settings.SpecificationDiscipline),
      cancellationToken);
    var addedSpecificationInitialRevision = await _addKnowledgeDocumentRevision.HandleAsync(
      new AddKnowledgeDocumentRevisionCommand(
        registeredSpecificationDocument.KnowledgeDocumentId,
        KnowledgeSourceId.New(),
        _settings.SpecificationInitialRevisionLabel,
        new DateOnly(2026, 7, 16),
        _settings.InitialTimestampUtc.AddHours(1),
        KnowledgeSourceAuthorityLevel.EngineerIssued,
        "spec-content-hash-r0",
        "spec-metadata-hash-r0",
        "specification-system",
        _settings.SpecificationInitialRevisionNotes,
        KnowledgeIngestionStatus.Processed),
      cancellationToken);
    var supersededSpecificationRevision = await _supersedeKnowledgeRevision.HandleAsync(
      new SupersedeKnowledgeRevisionCommand(
        registeredSpecificationDocument.KnowledgeDocumentId,
        addedSpecificationInitialRevision.KnowledgeDocumentRevisionId,
        KnowledgeSourceId.New(),
        _settings.SpecificationSupersedingRevisionLabel,
        new DateOnly(2026, 7, 16),
        _settings.InitialTimestampUtc.AddHours(2),
        KnowledgeSourceAuthorityLevel.EngineerIssued,
        "spec-content-hash-r1",
        "spec-metadata-hash-r1",
        "specification-system",
        _settings.SpecificationSupersedingRevisionNotes,
        KnowledgeIngestionStatus.Processed),
      cancellationToken);
    var registeredRfiDocument = await _registerKnowledgeDocument.HandleAsync(
      new RegisterKnowledgeDocumentCommand(
        createdProject.ProjectId,
        KnowledgeDocumentType.RFI,
        _settings.RfiTitle,
        _settings.RfiExternalReference,
        _settings.RfiDiscipline),
      cancellationToken);
    var addedRfiInitialRevision = await _addKnowledgeDocumentRevision.HandleAsync(
      new AddKnowledgeDocumentRevisionCommand(
        registeredRfiDocument.KnowledgeDocumentId,
        KnowledgeSourceId.New(),
        _settings.RfiInitialRevisionLabel,
        new DateOnly(2026, 7, 16),
        _settings.InitialTimestampUtc.AddHours(3),
        KnowledgeSourceAuthorityLevel.EngineerIssued,
        "rfi-content-hash-r0",
        "rfi-metadata-hash-r0",
        "rfi-system",
        _settings.RfiInitialRevisionNotes,
        KnowledgeIngestionStatus.Processed),
      cancellationToken);
    var createdKnowledgeRelationship = await _createKnowledgeRelationship.HandleAsync(
      new CreateKnowledgeRelationshipCommand(
        createdProject.ProjectId,
        KnowledgeSubjectReference.ForRevision(createdProject.ProjectId, addedRfiInitialRevision.KnowledgeDocumentRevisionId),
        KnowledgeSubjectReference.ForRevision(createdProject.ProjectId, supersededSpecificationRevision.SuccessorRevisionId),
        KnowledgeRelationshipType.Clarifies,
        _settings.RelationshipRationale),
      cancellationToken);
    var addedKnowledgeCitation = await _addKnowledgeCitation.HandleAsync(
      new AddKnowledgeCitationCommand(
        createdProject.ProjectId,
        supersededSpecificationRevision.SuccessorRevisionId,
        KnowledgeCitationLocationType.Section,
        _settings.CitationLocatorValue,
        _settings.CitationQuotedText),
      cancellationToken);

    var reloadedKnowledgeSnapshot = await _loadProjectKnowledgeSnapshot.HandleAsync(
      new LoadProjectKnowledgeSnapshotQuery(createdProject.ProjectId),
      cancellationToken);
    var replayedKnowledgeSnapshot = await _loadProjectKnowledgeSnapshot.HandleAsync(
      new LoadProjectKnowledgeSnapshotQuery(createdProject.ProjectId),
      cancellationToken);
    var reviewedAiProposalSnapshot = await _loadAiProposalWorkflowSnapshot.HandleAsync(
      new LoadAiProposalWorkflowSnapshotQuery(
        requestedAiProposal.ModelRunId,
        requestedAiProposal.ProposalId),
      cancellationToken);
    var persistedReportSnapshot = await _loadReportDraftSnapshot.HandleAsync(
      new LoadReportDraftSnapshotQuery(createdReportDraft.ReportId),
      cancellationToken);
    var failurePresentations = await RunExpectedFailureScenariosAsync(
      createdProject.ProjectId,
      registeredSpecificationDocument.KnowledgeDocumentId,
      addedSpecificationInitialRevision.KnowledgeDocumentRevisionId,
      supersededSpecificationRevision.SuccessorRevisionId,
      addedRfiInitialRevision.KnowledgeDocumentRevisionId,
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
      reviewedAiProposalSnapshotBeforeKnowledge,
      persistedInspectionSnapshot,
      persistedReportSnapshotBeforeKnowledge,
      registeredSpecificationDocument,
      addedSpecificationInitialRevision,
      supersededSpecificationRevision,
      registeredRfiDocument,
      addedRfiInitialRevision,
      createdKnowledgeRelationship,
      addedKnowledgeCitation,
      reloadedKnowledgeSnapshot,
      replayedKnowledgeSnapshot,
      reviewedAiProposalSnapshot,
      persistedReportSnapshot,
      failurePresentations);
  }

  private async Task<IReadOnlyList<DesktopWorkflowFailurePresentation>> RunExpectedFailureScenariosAsync(
    ProjectId projectId,
    KnowledgeDocumentId specificationDocumentId,
    KnowledgeDocumentRevisionId supersededRevisionId,
    KnowledgeDocumentRevisionId currentSpecificationRevisionId,
    KnowledgeDocumentRevisionId rfiRevisionId,
    CancellationToken cancellationToken)
  {
    var failures = new List<DesktopWorkflowFailurePresentation>();

    failures.Add(await CaptureExpectedFailureAsync(
      "Duplicate revision",
      () => _addKnowledgeDocumentRevision.HandleAsync(
        new AddKnowledgeDocumentRevisionCommand(
          specificationDocumentId,
          KnowledgeSourceId.New(),
          _settings.SpecificationInitialRevisionLabel,
          new DateOnly(2026, 7, 16),
          _settings.InitialTimestampUtc.AddHours(4),
          KnowledgeSourceAuthorityLevel.EngineerIssued,
          "spec-content-hash-duplicate",
          "spec-metadata-hash-duplicate",
          "specification-system",
          "Duplicate initial revision.",
          KnowledgeIngestionStatus.Processed),
        cancellationToken)));
    failures.Add(await CaptureExpectedFailureAsync(
      "Cross-project relationship",
      () => _createKnowledgeRelationship.HandleAsync(
        new CreateKnowledgeRelationshipCommand(
          projectId,
          KnowledgeSubjectReference.ForRevision(projectId, rfiRevisionId),
          KnowledgeSubjectReference.ForRevision(ProjectId.New(), currentSpecificationRevisionId),
          KnowledgeRelationshipType.Clarifies,
          "Cross-project relationship should fail."),
        cancellationToken)));
    failures.Add(await CaptureExpectedFailureAsync(
      "Missing revision citation",
      () => _addKnowledgeCitation.HandleAsync(
        new AddKnowledgeCitationCommand(
          projectId,
          KnowledgeDocumentRevisionId.New(),
          KnowledgeCitationLocationType.Section,
          "Section 9.9.Z",
          "Missing revision."),
        cancellationToken)));
    failures.Add(await CaptureExpectedFailureAsync(
      "Invalid supersession",
      () => _supersedeKnowledgeRevision.HandleAsync(
        new SupersedeKnowledgeRevisionCommand(
          specificationDocumentId,
          supersededRevisionId,
          KnowledgeSourceId.New(),
          "2",
          new DateOnly(2026, 7, 16),
          _settings.InitialTimestampUtc.AddHours(5),
          KnowledgeSourceAuthorityLevel.EngineerIssued,
          "spec-content-hash-invalid-supersession",
          "spec-metadata-hash-invalid-supersession",
          "specification-system",
          "Supersede an already superseded revision.",
          KnowledgeIngestionStatus.Processed),
        cancellationToken)));
    failures.Add(await CaptureExpectedFailureAsync(
      "Duplicate relationship",
      () => _createKnowledgeRelationship.HandleAsync(
        new CreateKnowledgeRelationshipCommand(
          projectId,
          KnowledgeSubjectReference.ForRevision(projectId, rfiRevisionId),
          KnowledgeSubjectReference.ForRevision(projectId, currentSpecificationRevisionId),
          KnowledgeRelationshipType.Clarifies,
          _settings.RelationshipRationale),
        cancellationToken)));
    failures.Add(await CaptureExpectedFailureAsync(
      "Invalid citation locator",
      () => _addKnowledgeCitation.HandleAsync(
        new AddKnowledgeCitationCommand(
          projectId,
          currentSpecificationRevisionId,
          KnowledgeCitationLocationType.Section,
          "   ",
          "Whitespace locator."),
        cancellationToken)));

    return failures;
  }

  private static async Task<DesktopWorkflowFailurePresentation> CaptureExpectedFailureAsync(
    string scenario,
    Func<Task> operation)
  {
    try
    {
      await operation();
    }
    catch (Exception exception) when (
      exception is DomainInvariantException
      || exception is LifecycleTransitionException
      || exception is InvalidOperationException)
    {
      return new DesktopWorkflowFailurePresentation(
        scenario,
        exception.GetType().Name,
        exception.Message);
    }

    throw new InvalidOperationException($"Expected failure scenario '{scenario}' completed successfully.");
  }
}
