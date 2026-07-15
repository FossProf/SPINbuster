using Microsoft.EntityFrameworkCore;
using SPINbuster.Infrastructure.Persistence.Records;

namespace SPINbuster.Infrastructure.Persistence;

internal static class SpinbusterModelConfiguration
{
  public static void Configure(ModelBuilder modelBuilder)
  {
    modelBuilder.HasAnnotation("ProductVersion", "9.0.0");
    modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 128);

    ConfigureProjects(modelBuilder);
    ConfigureInspectionSessions(modelBuilder);
    ConfigureReports(modelBuilder);
    ConfigureSaveTransactions(modelBuilder);
    ConfigureAuditEvents(modelBuilder);
  }

  private static void ConfigureProjects(ModelBuilder modelBuilder)
  {
    var builder = modelBuilder.Entity<ProjectRecord>();
    builder.ToTable("projects");
    builder.HasKey(record => record.Id);
    builder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.ProjectId).ValueGeneratedNever();
    builder.Property(record => record.Name).HasMaxLength(256).IsRequired();
    builder.Property(record => record.CreatedBy).HasMaxLength(256).IsRequired();
    builder.Property(record => record.CreatedAtUtc).IsRequired();
    builder.Property(record => record.Lifecycle).IsRequired();
  }

  private static void ConfigureInspectionSessions(ModelBuilder modelBuilder)
  {
    var builder = modelBuilder.Entity<InspectionSessionRecord>();
    builder.ToTable("inspection_sessions");
    builder.HasKey(record => record.Id);
    builder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.InspectionSessionId).ValueGeneratedNever();
    builder.Property(record => record.ProjectId).HasConversion(StronglyTypedIdValueConverters.ProjectId).IsRequired();
    builder.Property(record => record.Name).HasMaxLength(256).IsRequired();
    builder.Property(record => record.CreatedBy).HasMaxLength(256).IsRequired();
    builder.Property(record => record.CreatedAtUtc).IsRequired();
    builder.Property(record => record.Lifecycle).IsRequired();
    builder.HasIndex(record => record.ProjectId);
    builder.HasOne<ProjectRecord>()
      .WithMany()
      .HasForeignKey(record => record.ProjectId)
      .OnDelete(DeleteBehavior.Restrict);

    builder.HasMany(record => record.FieldNotes)
      .WithOne()
      .HasForeignKey(record => record.InspectionSessionId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(record => record.EvidenceAttachments)
      .WithOne()
      .HasForeignKey(record => record.InspectionSessionId)
      .OnDelete(DeleteBehavior.Cascade);

    var fieldNoteBuilder = modelBuilder.Entity<FieldNoteRecord>();
    fieldNoteBuilder.ToTable("field_notes");
    fieldNoteBuilder.HasKey(record => record.Id);
    fieldNoteBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.FieldNoteId).ValueGeneratedNever();
    fieldNoteBuilder.Property(record => record.InspectionSessionId).HasConversion(StronglyTypedIdValueConverters.InspectionSessionId).IsRequired();
    fieldNoteBuilder.Property(record => record.CapturedBy).HasMaxLength(256).IsRequired();
    fieldNoteBuilder.Property(record => record.CapturedAtUtc).IsRequired();
    fieldNoteBuilder.Property(record => record.RawText).HasColumnType("TEXT").IsRequired();
    fieldNoteBuilder.HasIndex(record => record.InspectionSessionId);

    var evidenceBuilder = modelBuilder.Entity<EvidenceAttachmentRecord>();
    evidenceBuilder.ToTable("evidence_attachments");
    evidenceBuilder.HasKey(record => record.Id);
    evidenceBuilder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.EvidenceAttachmentId).ValueGeneratedNever();
    evidenceBuilder.Property(record => record.InspectionSessionId).HasConversion(StronglyTypedIdValueConverters.InspectionSessionId).IsRequired();
    evidenceBuilder.Property(record => record.CapturedBy).HasMaxLength(256).IsRequired();
    evidenceBuilder.Property(record => record.CapturedAtUtc).IsRequired();
    evidenceBuilder.Property(record => record.FileName).HasMaxLength(512).IsRequired();
    evidenceBuilder.Property(record => record.MediaType).HasMaxLength(256).IsRequired();
    evidenceBuilder.Property(record => record.StorageKey).HasMaxLength(1024).IsRequired();
    evidenceBuilder.Property(record => record.Checksum).HasMaxLength(256).IsRequired();
    evidenceBuilder.Property(record => record.InterpretationSummary).HasColumnType("TEXT");
    evidenceBuilder.Property(record => record.InterpretedBy).HasMaxLength(256);
    evidenceBuilder.HasIndex(record => record.InspectionSessionId);
  }

  private static void ConfigureReports(ModelBuilder modelBuilder)
  {
    var builder = modelBuilder.Entity<ReportRecord>();
    builder.ToTable("reports");
    builder.HasKey(record => record.Id);
    builder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.ReportId).ValueGeneratedNever();
    builder.Property(record => record.ProjectId).HasConversion(StronglyTypedIdValueConverters.ProjectId).IsRequired();
    builder.Property(record => record.InspectionSessionId).HasConversion(StronglyTypedIdValueConverters.InspectionSessionId).IsRequired();
    builder.Property(record => record.Title).HasMaxLength(512).IsRequired();
    builder.Property(record => record.Body).HasColumnType("TEXT").IsRequired();
    builder.Property(record => record.CreatedBy).HasMaxLength(256).IsRequired();
    builder.Property(record => record.CreatedAtUtc).IsRequired();
    builder.Property(record => record.Lifecycle).IsRequired();
    builder.Property(record => record.ApprovedBy).HasMaxLength(256);
    builder.HasIndex(record => record.ProjectId);
    builder.HasIndex(record => record.InspectionSessionId);
    builder.HasOne<ProjectRecord>()
      .WithMany()
      .HasForeignKey(record => record.ProjectId)
      .OnDelete(DeleteBehavior.Restrict);
    builder.HasOne<InspectionSessionRecord>()
      .WithMany()
      .HasForeignKey(record => record.InspectionSessionId)
      .OnDelete(DeleteBehavior.Restrict);
  }

  private static void ConfigureSaveTransactions(ModelBuilder modelBuilder)
  {
    var builder = modelBuilder.Entity<SaveTransactionRecord>();
    builder.ToTable("save_transactions");
    builder.HasKey(record => record.Id);
    builder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.SaveTransactionId).ValueGeneratedNever();
    builder.Property(record => record.ReportId).HasConversion(StronglyTypedIdValueConverters.ReportId).IsRequired();
    builder.Property(record => record.InitiatedBy).HasMaxLength(256).IsRequired();
    builder.Property(record => record.CreatedAtUtc).IsRequired();
    builder.Property(record => record.State).IsRequired();
    builder.Property(record => record.FailureReason).HasColumnType("TEXT");
    builder.HasIndex(record => record.ReportId);
    builder.HasOne<ReportRecord>()
      .WithMany()
      .HasForeignKey(record => record.ReportId)
      .OnDelete(DeleteBehavior.Restrict);
  }

  private static void ConfigureAuditEvents(ModelBuilder modelBuilder)
  {
    var builder = modelBuilder.Entity<AuditEventRecord>();
    builder.ToTable("audit_events");
    builder.HasKey(record => record.Id);
    builder.Property(record => record.Id).HasConversion(StronglyTypedIdValueConverters.AuditEventId).ValueGeneratedNever();
    builder.Property(record => record.SubjectType).HasMaxLength(128).IsRequired();
    builder.Property(record => record.SubjectId).HasMaxLength(64).IsRequired();
    builder.Property(record => record.EventType).HasMaxLength(128).IsRequired();
    builder.Property(record => record.Actor).HasMaxLength(256).IsRequired();
    builder.Property(record => record.OccurredAtUtc).IsRequired();
    builder.Property(record => record.Description).HasColumnType("TEXT").IsRequired();
    builder.HasIndex(record => new { record.SubjectType, record.SubjectId, record.OccurredAtUtc });
  }
}
