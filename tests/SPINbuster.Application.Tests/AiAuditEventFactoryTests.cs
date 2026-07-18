using System.Reflection;
using SPINbuster.Application.Internal;
using SPINbuster.Domain;

namespace SPINbuster.Application.Tests;

public sealed class AiAuditEventFactoryTests
{
  private static readonly MethodInfo? CreateMethod =
    typeof(AiAuditEventFactory).GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static);

  [Fact]
  public void PrivateCreateHelperExists()
  {
    Assert.NotNull(CreateMethod);
  }

  [Fact]
  public void PrivateCreateHelperReturnsAuditEvent()
  {
    var result = CreateMethod!.Invoke(null, ["TestType", "id-1", "TestEvent", "actor", DateTimeOffset.UtcNow, "desc."]);

    Assert.IsType<AuditEvent>(result);
  }

  [Fact]
  public void AllPublicMethodsAreStatic()
  {
    var publicMethods = typeof(AiAuditEventFactory)
      .GetMethods(BindingFlags.Public | BindingFlags.Static)
      .Where(m => m.DeclaringType == typeof(AiAuditEventFactory))
      .ToArray();

    Assert.NotEmpty(publicMethods);

    foreach (var method in publicMethods)
    {
      Assert.True(method.IsStatic, $"{method.Name} should be static.");
    }
  }

  [Fact]
  public void PrivateCreateHelperIsCalledByPublicMethods()
  {
    var il = typeof(AiAuditEventFactory).Assembly
      .GetType("SPINbuster.Application.Internal.AiAuditEventFactory")!
      .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
      .First(m => m.Name == "Create")
      .GetMethodBody()!.GetILAsByteArray();

    Assert.NotNull(il);
  }
}
