using System.Xml.Linq;

namespace SPINbuster.Architecture.Tests;

public sealed class DependencyGraphTests
{
  private static readonly IReadOnlyDictionary<string, string[]> AllowedProjectReferences =
    new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
      ["SPINbuster.Shared"] = [],
      ["SPINbuster.Domain"] = ["SPINbuster.Shared"],
      ["SPINbuster.Rules"] = ["SPINbuster.Domain", "SPINbuster.Shared"],
      ["SPINbuster.Application"] = ["SPINbuster.Domain"],
      ["SPINbuster.Infrastructure"] = ["SPINbuster.Application", "SPINbuster.Shared"],
      ["SPINbuster.AI"] = ["SPINbuster.Application", "SPINbuster.Shared"],
      ["SPINbuster.Documents"] = ["SPINbuster.Application", "SPINbuster.Shared"],
      ["SPINbuster.Reporting"] = ["SPINbuster.Application", "SPINbuster.Shared"],
      ["SPINbuster.Server"] = ["SPINbuster.Application", "SPINbuster.Infrastructure", "SPINbuster.AI", "SPINbuster.Rules", "SPINbuster.Documents", "SPINbuster.Reporting", "SPINbuster.Shared"],
      ["SPINbuster.Desktop"] = ["SPINbuster.AI", "SPINbuster.Application", "SPINbuster.Documents", "SPINbuster.Infrastructure"],
    };

  [Fact]
  public void ProductionProjectsFollowApprovedReferenceGraph()
  {
    var repoRoot = FindRepositoryRoot();
    var projectPaths = Directory
      .EnumerateFiles(Path.Combine(repoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
      .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
      .ToArray();

    Assert.Equal(
      AllowedProjectReferences.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase),
      projectPaths.Select(path => Path.GetFileNameWithoutExtension(path)));

    foreach (var projectPath in projectPaths)
    {
      var projectName = Path.GetFileNameWithoutExtension(projectPath);
      var actualReferences = LoadProjectReferences(projectPath)
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
        .ToArray();

      var expectedReferences = AllowedProjectReferences[projectName]
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
        .ToArray();

      Assert.Equal(expectedReferences, actualReferences);
    }
  }

  [Fact]
  public void DesktopProjectRemainsBootstrapHostNotMaui()
  {
    var repoRoot = FindRepositoryRoot();
    var desktopProject = Path.Combine(repoRoot, "src", "SPINbuster.Desktop", "SPINbuster.Desktop.csproj");
    var xml = XDocument.Load(desktopProject);
    var projectElement = xml.Root ?? throw new InvalidOperationException("Desktop project XML root was not found.");
    var sdk = projectElement.Attribute("Sdk")?.Value;
    var useMaui = xml.Descendants("UseMaui").SingleOrDefault()?.Value;

    Assert.Equal("Microsoft.NET.Sdk", sdk);
    Assert.True(string.IsNullOrWhiteSpace(useMaui), "Desktop bootstrap host should not enable MAUI until the real client project is introduced.");
  }

  [Fact]
  public void TemplatePlaceholderFilesDoNotRemainInTrackedProjects()
  {
    var repoRoot = FindRepositoryRoot();
    var bannedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "Class1.cs",
      "UnitTest1.cs",
    };

    var matchingFiles = Directory
      .EnumerateFiles(Path.Combine(repoRoot, "src"), "*.cs", SearchOption.AllDirectories)
      .Concat(Directory.EnumerateFiles(Path.Combine(repoRoot, "tests"), "*.cs", SearchOption.AllDirectories))
      .Where(path => bannedFileNames.Contains(Path.GetFileName(path)))
      .ToArray();

    Assert.Empty(matchingFiles);
  }

  [Fact]
  public void DomainProjectUsesOnlyApprovedPackageReferences()
  {
    var repoRoot = FindRepositoryRoot();
    var packageReferences = LoadPackageReferences(Path.Combine(repoRoot, "src", "SPINbuster.Domain", "SPINbuster.Domain.csproj"));

    Assert.Empty(packageReferences);
  }

  [Fact]
  public void ApplicationProjectDoesNotReferenceOuterImplementationProjects()
  {
    var repoRoot = FindRepositoryRoot();
    var applicationProject = Path.Combine(repoRoot, "src", "SPINbuster.Application", "SPINbuster.Application.csproj");
    var disallowedReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "SPINbuster.Infrastructure",
      "SPINbuster.AI",
      "SPINbuster.Documents",
      "SPINbuster.Reporting",
      "SPINbuster.Server",
      "SPINbuster.Desktop",
    };

    var actualReferences = LoadProjectReferences(applicationProject);

    Assert.DoesNotContain(actualReferences, disallowedReferences.Contains);
  }

  [Fact]
  public void ApplicationProjectKeepsOnlyMinimalInwardReferenceSet()
  {
    var repoRoot = FindRepositoryRoot();
    var applicationProject = Path.Combine(repoRoot, "src", "SPINbuster.Application", "SPINbuster.Application.csproj");
    var allowedReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "SPINbuster.Domain",
    };

    var actualReferences = LoadProjectReferences(applicationProject);

    Assert.All(actualReferences, reference => Assert.Contains(reference, allowedReferences));
  }

  [Fact]
  public void ProductionProjectsDoNotReferenceCompositionRoots()
  {
    var repoRoot = FindRepositoryRoot();
    var disallowedReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "SPINbuster.Server",
      "SPINbuster.Desktop",
    };

    var violations = Directory
      .EnumerateFiles(Path.Combine(repoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
      .Select(projectPath => new
      {
        Project = Path.GetFileNameWithoutExtension(projectPath),
        References = LoadProjectReferences(projectPath),
      })
      .Where(result => result.References.Any(disallowedReferences.Contains))
      .Select(result => $"{result.Project} -> {string.Join(", ", result.References.Where(disallowedReferences.Contains).OrderBy(name => name, StringComparer.OrdinalIgnoreCase))}")
      .ToArray();

    Assert.Empty(violations);
  }

  [Fact]
  public void AdapterProjectsDoNotReferenceOtherAdapterProjects()
  {
    var repoRoot = FindRepositoryRoot();
    var adapterProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "SPINbuster.Infrastructure",
      "SPINbuster.AI",
      "SPINbuster.Documents",
      "SPINbuster.Reporting",
    };

    var violations = Directory
      .EnumerateFiles(Path.Combine(repoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
      .Select(projectPath => new
      {
        Project = Path.GetFileNameWithoutExtension(projectPath),
        References = LoadProjectReferences(projectPath),
      })
      .Where(result => adapterProjects.Contains(result.Project))
      .SelectMany(result => result.References
        .Where(reference => adapterProjects.Contains(reference))
        .Select(reference => $"{result.Project} -> {reference}"))
      .ToArray();

    Assert.Empty(violations);
  }

  [Fact]
  public void AiProjectDoesNotReferenceCompositionRoots()
  {
    var repoRoot = FindRepositoryRoot();
    var aiProject = Path.Combine(repoRoot, "src", "SPINbuster.AI", "SPINbuster.AI.csproj");
    var actualReferences = LoadProjectReferences(aiProject);

    Assert.DoesNotContain("SPINbuster.Server", actualReferences, StringComparer.OrdinalIgnoreCase);
    Assert.DoesNotContain("SPINbuster.Desktop", actualReferences, StringComparer.OrdinalIgnoreCase);
  }

  [Fact]
  public void AiProjectDoesNotReferenceInfrastructureProject()
  {
    var repoRoot = FindRepositoryRoot();
    var aiProject = Path.Combine(repoRoot, "src", "SPINbuster.AI", "SPINbuster.AI.csproj");
    var actualReferences = LoadProjectReferences(aiProject);

    Assert.DoesNotContain("SPINbuster.Infrastructure", actualReferences, StringComparer.OrdinalIgnoreCase);
  }

  [Fact]
  public void ApplicationProjectDoesNotReferenceProviderSpecificPackages()
  {
    var repoRoot = FindRepositoryRoot();
    var applicationProject = Path.Combine(repoRoot, "src", "SPINbuster.Application", "SPINbuster.Application.csproj");
    var packageReferences = LoadPackageReferences(applicationProject);

    Assert.DoesNotContain(packageReferences, reference =>
      reference.Contains("Ollama", StringComparison.OrdinalIgnoreCase)
      || reference.Contains("OpenAI", StringComparison.OrdinalIgnoreCase)
      || reference.Contains("Azure.AI", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void AiProjectCannotCallInfrastructureRepositoriesDirectly()
  {
    var repoRoot = FindRepositoryRoot();
    var aiSourceFiles = Directory
      .EnumerateFiles(Path.Combine(repoRoot, "src", "SPINbuster.AI"), "*.cs", SearchOption.AllDirectories)
      .ToArray();

    foreach (var aiSourceFile in aiSourceFiles)
    {
      var contents = File.ReadAllText(aiSourceFile);
      Assert.DoesNotContain("SqliteReportRepository", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("SqliteProjectRepository", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("SqliteInspectionSessionRepository", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("SpinbusterDbContext", contents, StringComparison.Ordinal);
    }
  }

  [Fact]
  public void ApplicationKnowledgeContractsDoNotReferenceEfCoreOrFileSystemTypes()
  {
    var repoRoot = FindRepositoryRoot();
    var knowledgeFiles = Directory
      .EnumerateFiles(Path.Combine(repoRoot, "src", "SPINbuster.Application"), "*.cs", SearchOption.AllDirectories)
      .Where(path =>
        path.Contains("Knowledge", StringComparison.OrdinalIgnoreCase)
        || path.Contains($"{Path.DirectorySeparatorChar}Repositories{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
      .ToArray();

    foreach (var knowledgeFile in knowledgeFiles)
    {
      var contents = File.ReadAllText(knowledgeFile);
      Assert.DoesNotContain("IQueryable", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("DbContext", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("DbSet", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("FileInfo", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("DirectoryInfo", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("FileStream", contents, StringComparison.Ordinal);
    }
  }

  [Fact]
  public void AiAndDocumentsProjectsDoNotUseKnowledgeMutationRepositoriesDirectly()
  {
    var repoRoot = FindRepositoryRoot();
    var adapterRoots = new[]
    {
      Path.Combine(repoRoot, "src", "SPINbuster.AI"),
      Path.Combine(repoRoot, "src", "SPINbuster.Documents"),
    };

    foreach (var adapterRoot in adapterRoots)
    {
      foreach (var sourceFile in Directory.EnumerateFiles(adapterRoot, "*.cs", SearchOption.AllDirectories))
      {
        var contents = File.ReadAllText(sourceFile);
        Assert.DoesNotContain("IKnowledgeDocumentRepository", contents, StringComparison.Ordinal);
        Assert.DoesNotContain("IKnowledgeRevisionRepository", contents, StringComparison.Ordinal);
        Assert.DoesNotContain("IKnowledgeRelationshipRepository", contents, StringComparison.Ordinal);
        Assert.DoesNotContain("IKnowledgeCitationRepository", contents, StringComparison.Ordinal);
      }
    }
  }

  [Fact]
  public void DocumentsProjectDoesNotAccessInfrastructureDbContextDirectly()
  {
    var repoRoot = FindRepositoryRoot();
    var documentsFiles = Directory
      .EnumerateFiles(Path.Combine(repoRoot, "src", "SPINbuster.Documents"), "*.cs", SearchOption.AllDirectories)
      .ToArray();

    foreach (var documentsFile in documentsFiles)
    {
      var contents = File.ReadAllText(documentsFile);
      Assert.DoesNotContain("DbContext", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("SpinbusterDbContext", contents, StringComparison.Ordinal);
    }
  }

  [Fact]
  public void DesktopProjectDoesNotUseEfCoreInfrastructureOrStorageImplementationsDirectly()
  {
    var repoRoot = FindRepositoryRoot();
    var desktopFiles = Directory
      .EnumerateFiles(Path.Combine(repoRoot, "src", "SPINbuster.Desktop"), "*.cs", SearchOption.AllDirectories)
      .ToArray();

    foreach (var desktopFile in desktopFiles)
    {
      var contents = File.ReadAllText(desktopFile);
      Assert.DoesNotContain("DbContext", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("SpinbusterDbContext", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("Infrastructure.Persistence.Records", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("IImmutableContentStore", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("InMemoryImmutableContentStore", contents, StringComparison.Ordinal);
    }
  }

  [Fact]
  public void DesktopProjectDoesNotCallDomainMutationApisDirectly()
  {
    var repoRoot = FindRepositoryRoot();
    var desktopFiles = Directory
      .EnumerateFiles(Path.Combine(repoRoot, "src", "SPINbuster.Desktop"), "*.cs", SearchOption.AllDirectories)
      .ToArray();
    var forbiddenTokens = new[]
    {
      ".Activate(",
      ".StartSession(",
      ".CaptureFieldNote(",
      ".AttachEvidence(",
      ".Interpret(",
      ".CreateDraft(",
      ".Accept(",
      ".Reject(",
      ".BeginImporting(",
      ".Complete(",
    };

    foreach (var desktopFile in desktopFiles)
    {
      var contents = File.ReadAllText(desktopFile);
      Assert.DoesNotContain(forbiddenTokens, token => contents.Contains(token, StringComparison.Ordinal));
    }
  }

  [Fact]
  public void DesktopProjectDoesNotPerformRawStorageFileIo()
  {
    var repoRoot = FindRepositoryRoot();
    var desktopFiles = Directory
      .EnumerateFiles(Path.Combine(repoRoot, "src", "SPINbuster.Desktop"), "*.cs", SearchOption.AllDirectories)
      .ToArray();
    var forbiddenTokens = new[]
    {
      "File.Open",
      "File.Read",
      "File.Write",
      "File.Delete",
      "Directory.CreateDirectory",
      "Directory.Delete",
      "FileStream",
    };

    foreach (var desktopFile in desktopFiles)
    {
      var contents = File.ReadAllText(desktopFile);
      Assert.DoesNotContain(forbiddenTokens, token => contents.Contains(token, StringComparison.Ordinal));
    }
  }

  [Fact]
  public void DocumentApplicationContractsDoNotLeakProviderSpecificTypes()
  {
    var repoRoot = FindRepositoryRoot();
    var documentsApplicationFiles = Directory
      .EnumerateFiles(Path.Combine(repoRoot, "src", "SPINbuster.Application"), "*.cs", SearchOption.AllDirectories)
      .Where(path => Path.GetFileName(path).Contains("Document", StringComparison.OrdinalIgnoreCase))
      .ToArray();

    foreach (var documentsApplicationFile in documentsApplicationFiles)
    {
      var contents = File.ReadAllText(documentsApplicationFile);
      Assert.DoesNotContain("FileInfo", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("DirectoryInfo", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("Azure.", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("Amazon.", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("Google.", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("Sqlite", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("DbContext", contents, StringComparison.Ordinal);
    }
  }

  [Fact]
  public void DocumentSnapshotContractsDoNotLeakStreamsPathsOrMutableInfrastructureTypes()
  {
    var repoRoot = FindRepositoryRoot();
    var snapshotFiles = Directory
      .EnumerateFiles(Path.Combine(repoRoot, "src", "SPINbuster.Application", "UseCases", "LoadProjectDocumentWorkflowSnapshot"), "*.cs", SearchOption.AllDirectories)
      .ToArray();

    foreach (var snapshotFile in snapshotFiles)
    {
      var contents = File.ReadAllText(snapshotFile);
      Assert.DoesNotContain("Stream", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("FileInfo", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("DirectoryInfo", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("FileStream", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("Path.", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("DbContext", contents, StringComparison.Ordinal);
      Assert.DoesNotContain("Sqlite", contents, StringComparison.Ordinal);
    }
  }

  [Fact]
  public void NonInfrastructureProjectsDoNotReferenceInfrastructurePersistenceRecords()
  {
    var repoRoot = FindRepositoryRoot();
    var productionRoots = Directory
      .EnumerateDirectories(Path.Combine(repoRoot, "src"))
      .Where(path => !path.EndsWith("SPINbuster.Infrastructure", StringComparison.OrdinalIgnoreCase))
      .ToArray();

    foreach (var productionRoot in productionRoots)
    {
      foreach (var sourceFile in Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories))
      {
        var contents = File.ReadAllText(sourceFile);
        Assert.DoesNotContain("SPINbuster.Infrastructure.Persistence.Records", contents, StringComparison.Ordinal);
        Assert.DoesNotContain("KnowledgeDocumentRecord", contents, StringComparison.Ordinal);
        Assert.DoesNotContain("KnowledgeRelationshipRecord", contents, StringComparison.Ordinal);
      }
    }
  }

  [Fact]
  public void SqliteUnitOfWorkDoesNotReferenceAggregateSpecificRecordTypes()
  {
    var repoRoot = FindRepositoryRoot();
    var unitOfWorkFile = Path.Combine(repoRoot, "src", "SPINbuster.Infrastructure", "Services", "SqliteUnitOfWork.cs");
    var contents = File.ReadAllText(unitOfWorkFile);

    Assert.DoesNotContain("KnowledgeDocumentRecord", contents, StringComparison.Ordinal);
    Assert.DoesNotContain("KnowledgeDocumentRevisionRecord", contents, StringComparison.Ordinal);
    Assert.DoesNotContain("KnowledgeRelationshipRecord", contents, StringComparison.Ordinal);
    Assert.DoesNotContain("KnowledgeCitationRecord", contents, StringComparison.Ordinal);
    Assert.DoesNotContain("AiProposalRecord", contents, StringComparison.Ordinal);
    Assert.DoesNotContain("ModelRunRecord", contents, StringComparison.Ordinal);
    Assert.DoesNotContain("ReportRecord", contents, StringComparison.Ordinal);
    Assert.DoesNotContain("ProjectRecord", contents, StringComparison.Ordinal);
  }

  [Fact]
  public void SqliteUnitOfWorkUsesIDeferredReferenceHandlerForGenericSaveOrdering()
  {
    var repoRoot = FindRepositoryRoot();
    var unitOfWorkFile = Path.Combine(repoRoot, "src", "SPINbuster.Infrastructure", "Services", "SqliteUnitOfWork.cs");
    var contents = File.ReadAllText(unitOfWorkFile);

    Assert.Contains("IDeferredReferenceHandler", contents, StringComparison.Ordinal);
    Assert.DoesNotContain("DeferredCurrentRevisionLink", contents, StringComparison.Ordinal);
  }

  private static string[] LoadPackageReferences(string projectPath)
  {
    return XDocument
      .Load(projectPath)
      .Descendants("PackageReference")
      .Select(node => node.Attribute("Include")?.Value)
      .Where(name => !string.IsNullOrWhiteSpace(name))
      .ToArray()!;
  }

  private static string[] LoadProjectReferences(string projectPath)
  {
    return XDocument
      .Load(projectPath)
      .Descendants("ProjectReference")
      .Select(node => Path.GetFileNameWithoutExtension(node.Attribute("Include")?.Value))
      .Where(name => !string.IsNullOrWhiteSpace(name))
      .ToArray()!;
  }

  private static string FindRepositoryRoot()
  {
    var directory = new DirectoryInfo(AppContext.BaseDirectory);

    while (directory is not null)
    {
      if (File.Exists(Path.Combine(directory.FullName, "SPINbuster.sln")))
      {
        return directory.FullName;
      }

      directory = directory.Parent;
    }

    throw new InvalidOperationException("Repository root could not be located.");
  }
}
