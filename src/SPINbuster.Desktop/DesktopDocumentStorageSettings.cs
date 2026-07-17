namespace SPINbuster.Desktop;

public sealed record DesktopDocumentStorageSettings(
  string RootPath,
  bool CreateRootIfMissing,
  bool FlushWritesThroughToDisk,
  bool VerifyFinalObjectAfterWrite,
  bool VerifyInventoryObjectIntegrity,
  int MaxInventoryResults);
