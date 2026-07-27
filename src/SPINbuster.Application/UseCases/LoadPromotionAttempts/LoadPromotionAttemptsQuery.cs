using SPINbuster.Application.Contracts;
using SPINbuster.Domain;

namespace SPINbuster.Application.UseCases.LoadPromotionAttempts;

public sealed record LoadPromotionAttemptsQuery(
  FragmentCandidateId FragmentCandidateId,
  ProjectId ProjectId,
  int MaxResults = 100) : IQuery<LoadPromotionAttemptsResult>;
