using SPINbuster.Application.Abstractions;
using SPINbuster.Application.Contracts;

namespace SPINbuster.Documents;

public sealed class DocumentParserRegistry : IDocumentParserRegistry
{
  private readonly Dictionary<string, IDocumentParser> _parsers = new(StringComparer.Ordinal);

  public DocumentParserRegistry(IEnumerable<IDocumentParser> parsers)
  {
    foreach (var parser in parsers)
    {
      var descriptor = parser.Describe();
      _parsers[descriptor.ParserKey] = parser;
    }
  }

  public IDocumentParser GetRequired(string parserKey)
  {
    if (_parsers.TryGetValue(parserKey, out var parser))
    {
      return parser;
    }

    throw new KeyNotFoundException($"Parser '{parserKey}' is not registered.");
  }

  public IReadOnlyList<ParserDescriptor> List()
  {
    return _parsers.Values.Select(p => p.Describe()).ToArray();
  }
}
