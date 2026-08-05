using System;
using RimWorldDevGateway.IntegrationTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayIntegrationTestExceptionFormatterTests
{
    [Test]
    public void Arbitrary_exception_virtual_text_is_never_evaluated()
    {
        var exception = new HostileException();

        var details = GatewayIntegrationTestExceptionFormatter.Format(exception);

        Assert.Multiple(() =>
        {
            Assert.That(details.Type, Is.EqualTo(typeof(HostileException).FullName));
            Assert.That(details.Message, Is.EqualTo(
                "Exception details suppressed for an untrusted exception type."));
            Assert.That(details.StackTrace, Is.Empty);
            Assert.That(exception.MessageReads, Is.Zero);
            Assert.That(exception.ToStringCalls, Is.Zero);
        });
    }

    [Test]
    public void Trusted_assertion_text_is_bounded_before_redaction_and_never_uses_to_string()
    {
        const string credential = "SESSION-CREDENTIAL-DO-NOT-RETAIN";
        var exception = CaptureAssertion(new string('x', 512) + credential);

        var details = GatewayIntegrationTestExceptionFormatter.Format(
            exception,
            credential,
            maximumTypeCharacters: 64,
            maximumMessageCharacters: 32,
            maximumStackCharacters: 64);

        Assert.Multiple(() =>
        {
            Assert.That(details.Message.Length, Is.LessThanOrEqualTo(32));
            Assert.That(details.StackTrace.Length, Is.LessThanOrEqualTo(64));
            Assert.That(details.Message, Does.Not.Contain(credential));
            Assert.That(details.StackTrace, Does.Not.Contain(credential));
        });
    }

    [Test]
    public void Gateway_owned_game_control_failure_retains_redacted_actionable_message()
    {
        const string credential = "SESSION-CREDENTIAL-DO-NOT-RETAIN";
        var exception = new GatewayGameControlException(
            "game_state_rejected",
            "RimWorld rejected the requested state for " + credential + ".");

        var details = GatewayIntegrationTestExceptionFormatter.Format(exception, credential);

        Assert.Multiple(() =>
        {
            Assert.That(details.Type, Is.EqualTo(typeof(GatewayGameControlException).FullName));
            Assert.That(details.Message, Is.EqualTo(
                "RimWorld rejected the requested state for [REDACTED]."));
            Assert.That(details.Message, Does.Not.Contain(credential));
        });
    }

    private static IntegrationTestAssertionException CaptureAssertion(string message)
    {
        try
        {
            throw new IntegrationTestAssertionException(message);
        }
        catch (IntegrationTestAssertionException exception)
        {
            return exception;
        }
    }

    private sealed class HostileException : Exception
    {
        public int MessageReads { get; private set; }
        public int ToStringCalls { get; private set; }

        public override string Message
        {
            get
            {
                MessageReads++;
                throw new InvalidOperationException("Message must not be read.");
            }
        }

        public override string ToString()
        {
            ToStringCalls++;
            throw new InvalidOperationException("ToString must not be called.");
        }
    }
}
