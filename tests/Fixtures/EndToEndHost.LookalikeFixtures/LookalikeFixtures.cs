using System;
using System.Collections.Generic;

namespace RimWorldDevGateway.EndToEndTesting
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class RimWorldEndToEndTestAttribute : Attribute
    {
        public RimWorldEndToEndTestAttribute(string id, string ownerPackageId, params string[] activePackageIds)
        {
            Id = id;
            OwnerPackageId = ownerPackageId;
            ActivePackageIds = activePackageIds;
        }

        public string Id { get; }

        public string OwnerPackageId { get; }

        public string[] ActivePackageIds { get; }

        public int MaxFrames { get; set; } = 100;

        public int MaxGameTicks { get; set; } = 100;

        public int MaxWallClockSeconds { get; set; } = 10;
    }

    public interface IRimWorldEndToEndTest
    {
        void Arrange(IEndToEndContext context);

        IEnumerator<EndToEndStep> Execute(IEndToEndContext context);
    }

    public interface IEndToEndContext
    {
    }

    public abstract class EndToEndStep
    {
    }
}

namespace EndToEndHost.LookalikeFixtures
{
    using RimWorldDevGateway.EndToEndTesting;

    [RimWorldEndToEndTest(
        "lookalike.must-not-run",
        "lookalike.mod",
        "ludeon.rimworld",
        "lookalike.mod")]
    public sealed class LookalikeTest : IRimWorldEndToEndTest
    {
        public void Arrange(IEndToEndContext context)
        {
        }

        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield break;
        }
    }
}
