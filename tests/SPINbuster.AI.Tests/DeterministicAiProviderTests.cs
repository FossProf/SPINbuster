using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SPINbuster.AI;
using SPINbuster.Application.Abstractions;
using SPINbuster.Domain;

namespace SPINbuster.AI.Tests;

public sealed class DeterministicAiProviderTests
{
  [Fact]
  public async Task SuccessScenarioReturnsStructuredPayloadAndNormalizedMetadata()
  {
    var provider = new DeterministicAiProvider(new DeterministicAiProviderOptions
    {
      Scenario = DeterministicAiScenario.Success,
    }, NullLogger<DeterministicAiProvider>.Instance);

    var result = await provider.GenerateAsync(CreateRequest());

    Assert.True(result.Succeeded);
    Assert.Equal(AiGenerationFailureClassification.None, result.FailureClassification);
    Assert.Equal(42, result.LatencyMilliseconds);
    Assert.Equal(128, result.InputTokenCount);
    Assert.Equal(96, result.OutputTokenCount);
    Assert.Contains("field-note-1", result.StructuredOutputJson, StringComparison.Ordinal);
    Assert.Contains("evidence-1", result.StructuredOutputJson, StringComparison.Ordinal);
  }

  [Fact]
  public async Task TimeoutScenarioReturnsTimeoutClassification()
  {
    var provider = new DeterministicAiProvider(new DeterministicAiProviderOptions
    {
      Scenario = DeterministicAiScenario.Timeout,
    }, NullLogger<DeterministicAiProvider>.Instance);

    var result = await provider.GenerateAsync(CreateRequest());

    Assert.False(result.Succeeded);
    Assert.Equal(AiGenerationFailureClassification.Timeout, result.FailureClassification);
    Assert.Null(result.StructuredOutputJson);
  }

  [Fact]
  public async Task ProviderUnavailableScenarioReturnsUnavailableClassification()
  {
    var provider = new DeterministicAiProvider(new DeterministicAiProviderOptions
    {
      Scenario = DeterministicAiScenario.ProviderUnavailable,
    }, NullLogger<DeterministicAiProvider>.Instance);

    var result = await provider.GenerateAsync(CreateRequest());

    Assert.False(result.Succeeded);
    Assert.Equal(AiGenerationFailureClassification.ProviderUnavailable, result.FailureClassification);
  }

  [Fact]
  public async Task MalformedJsonScenarioReturnsRepeatableMalformedPayload()
  {
    var provider = new DeterministicAiProvider(new DeterministicAiProviderOptions
    {
      Scenario = DeterministicAiScenario.MalformedJson,
    }, NullLogger<DeterministicAiProvider>.Instance);

    var result = await provider.GenerateAsync(CreateRequest());

    Assert.True(result.Succeeded);
    Assert.Equal("{ \"sections\": [", result.StructuredOutputJson);
  }

  [Fact]
  public void DescriptorExposesExpectedCapabilities()
  {
    var provider = new DeterministicAiProvider(new DeterministicAiProviderOptions(), NullLogger<DeterministicAiProvider>.Instance);

    var descriptor = provider.Describe();

    Assert.Equal("tier0-deterministic", descriptor.ProviderId);
    Assert.Contains(AiProviderCapability.StructuredOutput, descriptor.Capabilities);
    Assert.Contains(AiProviderCapability.DeterministicFixtures, descriptor.Capabilities);
    Assert.Contains(AiProviderCapability.TimeoutClassification, descriptor.Capabilities);
  }

  [Fact]
  public async Task PromptRegistryResolvesApprovedFixturePackage()
  {
    var registry = new DeterministicPromptPackageRegistry();

    var package = await registry.GetByIdAsync("report-draft-proposal-default", "0.1.0");

    Assert.NotNull(package);
    Assert.Equal(PromptPackageStatus.Approved, package!.Status);
    Assert.Equal("report-draft-proposal", package.RequiredOutputSchemaId);
  }

  private static AiGenerationRequest CreateRequest()
  {
    return new AiGenerationRequest(
      "operation-1",
      "report-draft-proposal-default",
      "0.1.0",
      "report-draft-proposal",
      "1.0.0",
      "Prompt template",
      """
SPINbuster governed report-draft proposal context
Authoritative field notes:
- FieldNote field-note-1: Observed corrosion at lower seam.
Authoritative evidence:
- Evidence evidence-1: photo.jpg [image/jpeg]
""",
      "manifest-hash",
      "input-hash",
      0.2m,
      TimeSpan.FromSeconds(30));
  }
}
