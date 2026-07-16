using Microsoft.Extensions.DependencyInjection;
using SPINbuster.Application.Abstractions;

namespace SPINbuster.AI;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddSpinbusterDeterministicAi(
    this IServiceCollection services,
    DeterministicAiProviderOptions? options = null)
  {
    services.AddSingleton(options ?? new DeterministicAiProviderOptions());
    services.AddSingleton<IAiGenerationProvider, DeterministicAiProvider>();
    services.AddSingleton<IAiPromptPackageRegistry, DeterministicPromptPackageRegistry>();
    return services;
  }
}
