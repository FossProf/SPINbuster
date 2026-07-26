using SPINbuster.Application.Abstractions;
using SPINbuster.Domain;

namespace SPINbuster.Application;

public sealed class AuthorityPolicy : IAuthorityPolicy
{
  private readonly ICurrentUser _currentUser;

  public AuthorityPolicy(ICurrentUser currentUser)
  {
    _currentUser = currentUser;
  }

  public string PolicyVersion => "1.0.0";

  public AuthorityPolicyResult Classify(
    FragmentCandidate candidate,
    ProjectId projectId)
  {
    var authority = KnowledgeSourceAuthorityLevel.Informational;

    var basis = $"Caller {_currentUser.UserId} for project {projectId}; candidate {candidate.Id} review state {candidate.ReviewState}; policy {PolicyVersion} classifies authority as {authority}.";

    return new AuthorityPolicyResult(authority, basis);
  }
}
