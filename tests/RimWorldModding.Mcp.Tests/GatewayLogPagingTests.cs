using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class GatewayLogPagingTests
{
    [TestCase(-1, 10)]
    [TestCase(0, 0)]
    [TestCase(0, 501)]
    public void PageArguments_RejectInvalidBounds(long after, int limit) =>
        Assert.Throws<ArgumentException>(() => GatewayLogArguments.Create(after, limit));

    [Test]
    public void PageArguments_ForwardCursorAndLimit()
    {
        Assert.That(GatewayLogArguments.Create(104, 23),
            Is.EqualTo(new[] { "logs", "--after", "104", "--limit", "23" }));
    }
}
