using SPINbuster.Application.Contracts;

namespace SPINbuster.Application.Abstractions;

/// <summary>
/// Resolves registered parser adapters by their stable parser key.
/// Enables multiple parsers to coexist within a single Application scope.
/// </summary>
public interface IDocumentParserRegistry
{
  IDocumentParser GetRequired(string parserKey);

  IReadOnlyList<ParserDescriptor> List();
}
