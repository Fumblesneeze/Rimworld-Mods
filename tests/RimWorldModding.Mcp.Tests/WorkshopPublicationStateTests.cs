using NUnit.Framework;
using RimWorldDevGateway.Contracts;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class WorkshopPublicationStateTests
{
    [TestCase("submit", false, "k_EResultNotLoggedOn", "submit-indeterminate")]
    [TestCase("submit", true, "k_EResultFail", "submit-indeterminate")]
    [TestCase("create", false, "k_EResultAccessDenied", "create-failed-definite")]
    [TestCase("dependency-add", false, "k_EResultAccessDenied", "dependency-failed-definite")]
    [TestCase("preview-submit", true, "k_EResultFail", "preview-submit-indeterminate")]
    public void FailureKeepsUncertainSubmissionInExactItemRecovery(string stage, bool ioFailure, string result, string expected) =>
        Assert.That(WorkshopPublicationState.FailureStage(stage, ioFailure, result), Is.EqualTo(expected));
}
