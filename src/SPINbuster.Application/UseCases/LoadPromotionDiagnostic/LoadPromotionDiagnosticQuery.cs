using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadPromotionDiagnostic;

public sealed record LoadPromotionDiagnosticQuery(
  PromotionDiagnosticId PromotionDiagnosticId) : IQuery<LoadPromotionDiagnosticResult>;
