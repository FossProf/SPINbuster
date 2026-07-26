namespace SPINbuster.Domain;

public sealed record AuthorityPolicyResult(
  KnowledgeSourceAuthorityLevel EffectiveAuthorityLevel,
  string AuthorityBasis);

public interface IAuthorityPolicy
{
  string PolicyVersion { get; }

  AuthorityPolicyResult Classify(
    FragmentCandidate candidate,
    ProjectId projectId);
}
