using System;

namespace RimWorldDevGateway.EndToEndTesting;

public enum EndToEndTestStatus
{
    Pending = 0,
    Arranging = 1,
    Running = 2,
    Cleaning = 3,
    Passed = 4,
    Failed = 5,
    InfrastructureFailed = 6,
    Skipped = 7,
    Aborted = 8
}

public enum EndToEndStepStatus
{
    Pending = 0,
    Running = 1,
    Passed = 2,
    Failed = 3,
    TimedOut = 4,
    Aborted = 5
}

public enum EndToEndFailureKind
{
    Assertion = 0,
    Exception = 1,
    Timeout = 2,
    Infrastructure = 3,
    Aborted = 4
}

public static class EndToEndResultStates
{
    public static bool IsTerminal(EndToEndTestStatus status)
    {
        switch (status)
        {
            case EndToEndTestStatus.Passed:
            case EndToEndTestStatus.Failed:
            case EndToEndTestStatus.InfrastructureFailed:
            case EndToEndTestStatus.Skipped:
            case EndToEndTestStatus.Aborted:
                return true;
            case EndToEndTestStatus.Pending:
            case EndToEndTestStatus.Arranging:
            case EndToEndTestStatus.Running:
            case EndToEndTestStatus.Cleaning:
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }
}
