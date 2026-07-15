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
      ["SPINbuster.Application"] = ["SPINbuster.Domain", "SPINbuster.Rules", "SPINbuster.Shared"],
      ["SPINbuster.Infrastructure"] = ["SPINbuster.Application", "SPINbuster.Shared"],
      ["SPINbuster.AI"] = ["SPINbuster.Application", "SPINbuster.Shared"],
      ["SPINbuster.Documents"] = ["SPINbuster.Application", "SPINbuster.Shared"],
      ["SPINbuster.Reporting"] = ["SPINbuster.Application", "SPINbuster.Shared"],
      ["SPINbuster.Server"] = ["SPINbuster.Application", "SPINbuster.Infrastructure", "SPINbuster.AI", "SPINbuster.Rules", "SPINbuster.Documents", "SPINbuster.Reporting", "SPINbuster.Shared"],
      ["SPINbuster.Desktop"] = ["SPINbuster.Application", "SPINbuster.Infrastructure", "SPINbuster.AI", "SPINbuster.Rules", "SPINbuster.Documents", "SPINbuster.Reporting", "SPINbuster.Shared"],
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
