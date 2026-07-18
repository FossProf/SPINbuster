namespace SPINbuster.Domain;

public enum KnowledgeDocumentType
{
  Drawing = 0,
  Specification = 1,
  RFI = 2,
  Bulletin = 3,
  Submittal = 4,
  ChangeOrder = 5,
  Report = 6,
  FieldNote = 7,
  Evidence = 8,
  GeneralReference = 9,
}

public enum KnowledgeSourceAuthorityLevel
{
  Informational = 0,
  ContractorSubmitted = 1,
  OwnerProvided = 2,
  EngineerIssued = 3,
  FieldObserved = 4,
}

public enum KnowledgeRevisionLifecycle
{
  Received = 0,
  CurrentAuthoritative = 1,
  Superseded = 2,
  Withdrawn = 3,
}

public enum KnowledgeRelationshipType
{
  References = 0,
  Supersedes = 1,
  Clarifies = 2,
  Implements = 3,
  AppliesTo = 4,
  Supports = 5,
  Contradicts = 6,
  DerivedFrom = 7,
  AttachedTo = 8,
  RespondsTo = 9,
}

public enum KnowledgeCitationLocationType
{
  PageNumber = 0,
  SheetNumber = 1,
  Section = 2,
  Paragraph = 3,
  Detail = 4,
  Table = 5,
  Figure = 6,
  LineRange = 7,
  FreeformLocator = 8,
}

public enum KnowledgeIngestionStatus
{
  Registered = 0,
  MetadataCaptured = 1,
  PendingProcessing = 2,
  Processed = 3,
  Failed = 4,
}

public enum KnowledgeVerificationStatus
{
  Unverified = 0,
  Verified = 1,
  Rejected = 2,
  Superseded = 3,
}

public enum KnowledgeDocumentLifecycle
{
  Active = 0,
  Archived = 1,
}

public enum KnowledgeSubjectKind
{
  Document = 0,
  Revision = 1,
}

public readonly record struct KnowledgeSubjectReference
{
  private KnowledgeSubjectReference(
    ProjectId projectId,
    KnowledgeSubjectKind subjectKind,
    KnowledgeDocumentId? documentId,
    KnowledgeDocumentRevisionId? revisionId)
  {
    ProjectId = projectId;
    SubjectKind = subjectKind;
    DocumentId = documentId;
    RevisionId = revisionId;
  }

  public ProjectId ProjectId { get; }

  public KnowledgeSubjectKind SubjectKind { get; }

  public KnowledgeDocumentId? DocumentId { get; }

  public KnowledgeDocumentRevisionId? RevisionId { get; }

  public static KnowledgeSubjectReference ForDocument(ProjectId projectId, KnowledgeDocumentId documentId)
    => new(projectId, KnowledgeSubjectKind.Document, documentId, null);

  public static KnowledgeSubjectReference ForRevision(ProjectId projectId, KnowledgeDocumentRevisionId revisionId)
    => new(projectId, KnowledgeSubjectKind.Revision, null, revisionId);

  public string ToStableKey()
  {
    return SubjectKind switch
    {
      KnowledgeSubjectKind.Document => $"Document:{DocumentId}",
      KnowledgeSubjectKind.Revision => $"Revision:{RevisionId}",
      _ => throw new DomainInvariantException($"Unsupported {nameof(KnowledgeSubjectKind)} value {SubjectKind}."),
    };
  }
}

public sealed class KnowledgeDocument : AuditableEntity
{
  private const string AuditSubjectType = "KnowledgeDocument";

  private readonly List<KnowledgeDocumentRevision> _revisions = [];

  public KnowledgeDocument(
    KnowledgeDocumentId id,
    ProjectId projectId,
    KnowledgeDocumentType documentType,
    string canonicalTitle,
    string? externalReferenceNumber,
    string? disciplineOrCategory,
    string createdBy,
    DateTimeOffset createdAtUtc)
  {
    Id = id;
    ProjectId = projectId;
    DocumentType = documentType;
    CanonicalTitle = DomainGuards.NotNullOrWhiteSpace(canonicalTitle, nameof(canonicalTitle));
    ExternalReferenceNumber = NormalizeOptional(externalReferenceNumber);
    DisciplineOrCategory = NormalizeOptional(disciplineOrCategory);
    CreatedBy = DomainGuards.NotNullOrWhiteSpace(createdBy, nameof(createdBy));
    CreatedAtUtc = DomainGuards.NotDefault(createdAtUtc, nameof(createdAtUtc));
    Lifecycle = KnowledgeDocumentLifecycle.Active;

    AppendAuditEvent(CreateAuditEvent(
      "KnowledgeDocumentRegistered",
      createdBy,
      createdAtUtc,
      $"Knowledge document registered as {DocumentType} with title '{CanonicalTitle}'."));
  }

  public KnowledgeDocumentId Id { get; }

  public ProjectId ProjectId { get; }

  public KnowledgeDocumentType DocumentType { get; }

  public string CanonicalTitle { get; }

  public string? ExternalReferenceNumber { get; }

  public string? DisciplineOrCategory { get; }

  public KnowledgeDocumentRevisionId? CurrentAuthoritativeRevisionId { get; private set; }

  public KnowledgeDocumentLifecycle Lifecycle { get; private set; }

  public string CreatedBy { get; }

  public DateTimeOffset CreatedAtUtc { get; }

  public IReadOnlyList<KnowledgeDocumentRevision> Revisions => _revisions.AsReadOnly();

  protected override string SubjectType => AuditSubjectType;

  protected override string SubjectId => Id.ToString();

  internal static KnowledgeDocument Rehydrate(
    KnowledgeDocumentId id,
    ProjectId projectId,
    KnowledgeDocumentType documentType,
    string canonicalTitle,
    string? externalReferenceNumber,
    string? disciplineOrCategory,
    KnowledgeDocumentRevisionId? currentAuthoritativeRevisionId,
    KnowledgeDocumentLifecycle lifecycle,
    string createdBy,
    DateTimeOffset createdAtUtc,
    IEnumerable<KnowledgeDocumentRevision> revisions,
    IEnumerable<AuditEvent> auditTrail)
  {
    var document = new KnowledgeDocument(
      id,
      projectId,
      documentType,
      canonicalTitle,
      externalReferenceNumber,
      disciplineOrCategory,
      createdBy,
      createdAtUtc)
    {
      CurrentAuthoritativeRevisionId = currentAuthoritativeRevisionId,
      Lifecycle = lifecycle,
    };

    document._revisions.Clear();
    document._revisions.AddRange(revisions ?? []);
    document.ValidateRehydratedState();
    document.RestoreAuditTrail(auditTrail);
    return document;
  }

  public KnowledgeDocumentRevision AddInitialRevision(
    KnowledgeDocumentRevision revision,
    string actor,
    DateTimeOffset occurredAtUtc)
  {
    EnsureDocumentCanReceiveActiveRevision(nameof(AddInitialRevision));
    EnsureRevisionBelongsToDocument(revision);

    if (CurrentAuthoritativeRevisionId is not null)
    {
      throw new DomainInvariantException("An initial authoritative revision cannot be added when a current revision already exists.");
    }

    if (revision.SupersedesRevisionId is not null)
    {
      throw new DomainInvariantException("Initial knowledge revisions cannot supersede an existing revision.");
    }

    EnsureRevisionIdentityIsUnique(revision);

    revision.PromoteToCurrentAuthoritative();
    _revisions.Add(revision);
    CurrentAuthoritativeRevisionId = revision.Id;

    AppendAuditEvent(CreateAuditEvent(
      "KnowledgeRevisionCreated",
      actor,
      occurredAtUtc,
      $"Knowledge revision {revision.RevisionLabel} created as the initial authoritative revision."));
    return revision;
  }

  public KnowledgeRevisionSupersessionOutcome SupersedeCurrentRevision(
    KnowledgeDocumentRevision successorRevision,
    string actor,
    DateTimeOffset occurredAtUtc)
  {
    EnsureDocumentCanReceiveActiveRevision(nameof(SupersedeCurrentRevision));
    EnsureRevisionBelongsToDocument(successorRevision);
    EnsureRevisionIdentityIsUnique(successorRevision);

    if (CurrentAuthoritativeRevisionId is null)
    {
      throw new DomainInvariantException("A knowledge revision cannot be superseded before an initial revision exists.");
    }

    if (successorRevision.SupersedesRevisionId != CurrentAuthoritativeRevisionId)
    {
      throw new DomainInvariantException("A successor revision must explicitly supersede the current authoritative revision.");
    }

    var supersededRevision = _revisions.SingleOrDefault(revision => revision.Id == CurrentAuthoritativeRevisionId.Value)
      ?? throw new DomainInvariantException("The current authoritative revision could not be located.");

    supersededRevision.MarkSuperseded(successorRevision.Id);
    successorRevision.PromoteToCurrentAuthoritative();
    _revisions.Add(successorRevision);
    CurrentAuthoritativeRevisionId = successorRevision.Id;

    AppendAuditEvent(CreateAuditEvent(
      "KnowledgeRevisionCreated",
      actor,
      occurredAtUtc,
      $"Knowledge revision {successorRevision.RevisionLabel} created for document {Id}."));
    AppendAuditEvent(CreateAuditEvent(
      "KnowledgeRevisionSuperseded",
      actor,
      occurredAtUtc,
      $"Knowledge revision {supersededRevision.RevisionLabel} superseded by {successorRevision.RevisionLabel}."));

    return new KnowledgeRevisionSupersessionOutcome(successorRevision, supersededRevision);
  }

  public KnowledgeDocumentRevision VerifyRevision(
    KnowledgeDocumentRevisionId revisionId,
    KnowledgeVerificationStatus verificationStatus,
    string actor,
    DateTimeOffset occurredAtUtc)
  {
    var revision = _revisions.SingleOrDefault(item => item.Id == revisionId)
      ?? throw new DomainInvariantException($"Knowledge revision {revisionId} was not found on document {Id}.");

    revision.UpdateVerificationStatus(verificationStatus);
    AppendAuditEvent(CreateAuditEvent(
      "KnowledgeRevisionVerificationChanged",
      actor,
      occurredAtUtc,
      $"Knowledge revision {revision.RevisionLabel} verification changed to {verificationStatus}."));
    return revision;
  }

  public void Archive(string actor, DateTimeOffset occurredAtUtc)
  {
    if (Lifecycle != KnowledgeDocumentLifecycle.Active)
    {
      throw new LifecycleTransitionException(nameof(KnowledgeDocument), Lifecycle.ToString(), nameof(Archive));
    }

    Lifecycle = KnowledgeDocumentLifecycle.Archived;
    AppendAuditEvent(CreateAuditEvent(
      "KnowledgeDocumentArchived",
      actor,
      occurredAtUtc,
      "Knowledge document archived."));
  }

  public void Restore(string actor, DateTimeOffset occurredAtUtc)
  {
    if (Lifecycle != KnowledgeDocumentLifecycle.Archived)
    {
      throw new LifecycleTransitionException(nameof(KnowledgeDocument), Lifecycle.ToString(), nameof(Restore));
    }

    Lifecycle = KnowledgeDocumentLifecycle.Active;
    AppendAuditEvent(CreateAuditEvent(
      "KnowledgeDocumentRestored",
      actor,
      occurredAtUtc,
      "Knowledge document restored to active status."));
  }

  private void EnsureDocumentCanReceiveActiveRevision(string transitionName)
  {
    if (Lifecycle == KnowledgeDocumentLifecycle.Archived)
    {
      throw new LifecycleTransitionException(nameof(KnowledgeDocument), Lifecycle.ToString(), transitionName);
    }
  }

  private void EnsureRevisionBelongsToDocument(KnowledgeDocumentRevision revision)
  {
    if (revision is null)
    {
      throw new DomainInvariantException($"{nameof(revision)} must be provided.");
    }

    if (revision.DocumentId != Id)
    {
      throw new DomainInvariantException($"Knowledge revision {revision.Id} does not belong to document {Id}.");
    }
  }

  private void EnsureRevisionIdentityIsUnique(KnowledgeDocumentRevision revision)
  {
    if (_revisions.Any(existing => existing.Id == revision.Id))
    {
      throw new DomainInvariantException($"Knowledge revision {revision.Id} is already registered on document {Id}.");
    }

    if (_revisions.Any(existing => string.Equals(existing.RevisionLabel, revision.RevisionLabel, StringComparison.OrdinalIgnoreCase)))
    {
      throw new DomainInvariantException($"Duplicate knowledge revision label '{revision.RevisionLabel}' is not allowed on document {Id}.");
    }
  }

  private void ValidateRehydratedState()
  {
    var currentRevisions = _revisions
      .Where(revision => revision.Lifecycle == KnowledgeRevisionLifecycle.CurrentAuthoritative)
      .ToArray();
    if (currentRevisions.Length > 1)
    {
      throw new DomainInvariantException("A knowledge document cannot have multiple current authoritative revisions.");
    }

    if (CurrentAuthoritativeRevisionId is not null)
    {
      if (_revisions.All(revision => revision.Id != CurrentAuthoritativeRevisionId.Value))
      {
        throw new DomainInvariantException("The current authoritative revision must belong to the same knowledge document.");
      }

      if (currentRevisions.Length == 1 && currentRevisions[0].Id != CurrentAuthoritativeRevisionId.Value)
      {
        throw new DomainInvariantException("The stored current authoritative revision ID must match the current authoritative revision entity.");
      }
    }
  }

  private static string? NormalizeOptional(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }
}

public sealed record KnowledgeRevisionSupersessionOutcome(
  KnowledgeDocumentRevision SuccessorRevision,
  KnowledgeDocumentRevision SupersededRevision);

public sealed class KnowledgeDocumentRevision
{
  public KnowledgeDocumentRevision(
    KnowledgeDocumentRevisionId id,
    KnowledgeDocumentId documentId,
    KnowledgeSourceId knowledgeSourceId,
    string revisionLabel,
    DateOnly? effectiveDate,
    DateTimeOffset receivedAtUtc,
    KnowledgeSourceAuthorityLevel sourceAuthority,
    string contentHash,
    string metadataHash,
    KnowledgeDocumentRevisionId? supersedesRevisionId,
    string? sourceSystemReference,
    string? descriptiveNotes,
    DateTimeOffset createdAtUtc,
    KnowledgeIngestionStatus ingestionStatus = KnowledgeIngestionStatus.Registered,
    KnowledgeVerificationStatus verificationStatus = KnowledgeVerificationStatus.Unverified)
  {
    if (supersedesRevisionId == id)
    {
      throw new DomainInvariantException("A knowledge revision cannot supersede itself.");
    }

    Id = id;
    DocumentId = documentId;
    KnowledgeSourceId = knowledgeSourceId;
    RevisionLabel = DomainGuards.NotNullOrWhiteSpace(revisionLabel, nameof(revisionLabel));
    EffectiveDate = effectiveDate;
    ReceivedAtUtc = DomainGuards.NotDefault(receivedAtUtc, nameof(receivedAtUtc));
    SourceAuthority = sourceAuthority;
    VerificationStatus = verificationStatus;
    ContentHash = DomainGuards.NotNullOrWhiteSpace(contentHash, nameof(contentHash));
    MetadataHash = DomainGuards.NotNullOrWhiteSpace(metadataHash, nameof(metadataHash));
    SupersedesRevisionId = supersedesRevisionId;
    SourceSystemReference = NormalizeOptional(sourceSystemReference);
    DescriptiveNotes = NormalizeOptional(descriptiveNotes);
    CreatedAtUtc = DomainGuards.NotDefault(createdAtUtc, nameof(createdAtUtc));
    IngestionStatus = ingestionStatus;
    Lifecycle = KnowledgeRevisionLifecycle.Received;
  }

  public KnowledgeDocumentRevisionId Id { get; }

  public KnowledgeDocumentId DocumentId { get; }

  public KnowledgeSourceId KnowledgeSourceId { get; }

  public string RevisionLabel { get; }

  public DateOnly? EffectiveDate { get; }

  public DateTimeOffset ReceivedAtUtc { get; }

  public KnowledgeSourceAuthorityLevel SourceAuthority { get; }

  public KnowledgeVerificationStatus VerificationStatus { get; private set; }

  public string ContentHash { get; }

  public string MetadataHash { get; }

  public KnowledgeDocumentRevisionId? SupersedesRevisionId { get; }

  public KnowledgeDocumentRevisionId? SupersededByRevisionId { get; private set; }

  public string? SourceSystemReference { get; }

  public string? DescriptiveNotes { get; }

  public DateTimeOffset CreatedAtUtc { get; }

  public KnowledgeIngestionStatus IngestionStatus { get; private set; }

  public KnowledgeRevisionLifecycle Lifecycle { get; private set; }

  internal static KnowledgeDocumentRevision Rehydrate(
    KnowledgeDocumentRevisionId id,
    KnowledgeDocumentId documentId,
    KnowledgeSourceId knowledgeSourceId,
    string revisionLabel,
    DateOnly? effectiveDate,
    DateTimeOffset receivedAtUtc,
    KnowledgeSourceAuthorityLevel sourceAuthority,
    KnowledgeVerificationStatus verificationStatus,
    string contentHash,
    string metadataHash,
    KnowledgeDocumentRevisionId? supersedesRevisionId,
    KnowledgeDocumentRevisionId? supersededByRevisionId,
    string? sourceSystemReference,
    string? descriptiveNotes,
    DateTimeOffset createdAtUtc,
    KnowledgeIngestionStatus ingestionStatus,
    KnowledgeRevisionLifecycle lifecycle)
  {
    var revision = new KnowledgeDocumentRevision(
      id,
      documentId,
      knowledgeSourceId,
      revisionLabel,
      effectiveDate,
      receivedAtUtc,
      sourceAuthority,
      contentHash,
      metadataHash,
      supersedesRevisionId,
      sourceSystemReference,
      descriptiveNotes,
      createdAtUtc,
      ingestionStatus,
      verificationStatus)
    {
      SupersededByRevisionId = supersededByRevisionId,
      Lifecycle = lifecycle,
    };

    return revision;
  }

  public void UpdateVerificationStatus(KnowledgeVerificationStatus verificationStatus)
  {
    if (Lifecycle == KnowledgeRevisionLifecycle.Superseded && verificationStatus != KnowledgeVerificationStatus.Superseded)
    {
      throw new DomainInvariantException("Superseded knowledge revisions must retain superseded verification status.");
    }

    if (verificationStatus == KnowledgeVerificationStatus.Superseded && Lifecycle != KnowledgeRevisionLifecycle.Superseded)
    {
      throw new DomainInvariantException("Only superseded knowledge revisions may use superseded verification status.");
    }

    VerificationStatus = verificationStatus;
  }

  public void UpdateIngestionStatus(KnowledgeIngestionStatus ingestionStatus)
  {
    IngestionStatus = ingestionStatus;
  }

  internal void PromoteToCurrentAuthoritative()
  {
    if (Lifecycle != KnowledgeRevisionLifecycle.Received)
    {
      throw new DomainInvariantException($"Knowledge revision {Id} cannot become current authoritative from lifecycle {Lifecycle}.");
    }

    Lifecycle = KnowledgeRevisionLifecycle.CurrentAuthoritative;
  }

  internal void MarkSuperseded(KnowledgeDocumentRevisionId supersededByRevisionId)
  {
    if (Lifecycle != KnowledgeRevisionLifecycle.CurrentAuthoritative)
    {
      throw new DomainInvariantException("Only the current authoritative knowledge revision can be superseded.");
    }

    if (SupersededByRevisionId is not null)
    {
      throw new DomainInvariantException("Knowledge supersession history cannot be rewritten destructively.");
    }

    if (supersededByRevisionId == Id)
    {
      throw new DomainInvariantException("A knowledge revision cannot be superseded by itself.");
    }

    SupersededByRevisionId = supersededByRevisionId;
    Lifecycle = KnowledgeRevisionLifecycle.Superseded;
    VerificationStatus = KnowledgeVerificationStatus.Superseded;
  }

  private static string? NormalizeOptional(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }
}

public sealed class KnowledgeRelationship : AuditableEntity
{
  private const string AuditSubjectType = "KnowledgeRelationship";

  public KnowledgeRelationship(
    KnowledgeRelationshipId id,
    ProjectId projectId,
    KnowledgeSubjectReference source,
    KnowledgeSubjectReference target,
    KnowledgeRelationshipType relationshipType,
    string evidenceOrRationale,
    string createdBy,
    DateTimeOffset createdAtUtc,
    KnowledgeVerificationStatus verificationStatus = KnowledgeVerificationStatus.Unverified)
  {
    if (source.ProjectId != projectId || target.ProjectId != projectId)
    {
      throw new DomainInvariantException("Knowledge relationships cannot cross project boundaries.");
    }

    if (source == target && !AllowsSelfReference(relationshipType))
    {
      throw new DomainInvariantException($"Self relationships are not allowed for {relationshipType}.");
    }

    Id = id;
    ProjectId = projectId;
    Source = source;
    Target = target;
    RelationshipType = relationshipType;
    EvidenceOrRationale = DomainGuards.NotNullOrWhiteSpace(evidenceOrRationale, nameof(evidenceOrRationale));
    CreatedBy = DomainGuards.NotNullOrWhiteSpace(createdBy, nameof(createdBy));
    CreatedAtUtc = DomainGuards.NotDefault(createdAtUtc, nameof(createdAtUtc));
    VerificationStatus = verificationStatus;

    AppendAuditEvent(CreateAuditEvent(
      "KnowledgeRelationshipCreated",
      createdBy,
      createdAtUtc,
      $"Knowledge relationship {RelationshipType} created between {Source.ToStableKey()} and {Target.ToStableKey()}."));

    if (relationshipType == KnowledgeRelationshipType.Contradicts)
    {
      AppendAuditEvent(CreateAuditEvent(
        "KnowledgeContradictionDetected",
        createdBy,
        createdAtUtc,
        $"Contradictory knowledge relationship detected between {Source.ToStableKey()} and {Target.ToStableKey()}."));
    }
  }

  public KnowledgeRelationshipId Id { get; }

  public ProjectId ProjectId { get; }

  public KnowledgeSubjectReference Source { get; }

  public KnowledgeSubjectReference Target { get; }

  public KnowledgeRelationshipType RelationshipType { get; }

  public string EvidenceOrRationale { get; }

  public string CreatedBy { get; }

  public DateTimeOffset CreatedAtUtc { get; }

  public KnowledgeVerificationStatus VerificationStatus { get; private set; }

  protected override string SubjectType => AuditSubjectType;

  protected override string SubjectId => Id.ToString();

  internal static KnowledgeRelationship Rehydrate(
    KnowledgeRelationshipId id,
    ProjectId projectId,
    KnowledgeSubjectReference source,
    KnowledgeSubjectReference target,
    KnowledgeRelationshipType relationshipType,
    string evidenceOrRationale,
    string createdBy,
    DateTimeOffset createdAtUtc,
    KnowledgeVerificationStatus verificationStatus,
    IEnumerable<AuditEvent> auditTrail)
  {
    var relationship = new KnowledgeRelationship(
      id,
      projectId,
      source,
      target,
      relationshipType,
      evidenceOrRationale,
      createdBy,
      createdAtUtc,
      verificationStatus);

    relationship.RestoreAuditTrail(auditTrail);
    return relationship;
  }

  public void UpdateVerificationStatus(KnowledgeVerificationStatus verificationStatus)
  {
    VerificationStatus = verificationStatus;
  }

  public static void EnsureNoDuplicate(
    IEnumerable<KnowledgeRelationship> existingRelationships,
    KnowledgeRelationship candidate)
  {
    if ((existingRelationships ?? [])
      .Any(existing =>
        existing.ProjectId == candidate.ProjectId
        && existing.Source == candidate.Source
        && existing.Target == candidate.Target
        && existing.RelationshipType == candidate.RelationshipType))
    {
      throw new DomainInvariantException(
        $"Duplicate knowledge relationship {candidate.RelationshipType} between {candidate.Source.ToStableKey()} and {candidate.Target.ToStableKey()} is not allowed.");
    }
  }

  private static bool AllowsSelfReference(KnowledgeRelationshipType relationshipType)
  {
    return false;
  }
}

public sealed class KnowledgeCitation
{
  public KnowledgeCitation(
    KnowledgeCitationId id,
    KnowledgeDocumentRevisionId citedRevisionId,
    KnowledgeCitationLocationType locatorType,
    string locatorValue,
    string revisionContentHash,
    DateTimeOffset createdAtUtc,
    string? quotedOrSummarizedText)
  {
    Id = id;
    CitedRevisionId = citedRevisionId;
    LocatorType = locatorType;
    LocatorValue = DomainGuards.NotNullOrWhiteSpace(locatorValue, nameof(locatorValue));
    RevisionContentHash = DomainGuards.NotNullOrWhiteSpace(revisionContentHash, nameof(revisionContentHash));
    CreatedAtUtc = DomainGuards.NotDefault(createdAtUtc, nameof(createdAtUtc));
    QuotedOrSummarizedText = string.IsNullOrWhiteSpace(quotedOrSummarizedText) ? null : quotedOrSummarizedText.Trim();
  }

  public KnowledgeCitationId Id { get; }

  public KnowledgeDocumentRevisionId CitedRevisionId { get; }

  public KnowledgeCitationLocationType LocatorType { get; }

  public string LocatorValue { get; }

  public string RevisionContentHash { get; }

  public DateTimeOffset CreatedAtUtc { get; }

  public string? QuotedOrSummarizedText { get; }
}
