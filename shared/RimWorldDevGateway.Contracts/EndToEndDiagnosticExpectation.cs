using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace RimWorldDevGateway.Contracts;

[DataContract]
public sealed class EndToEndDiagnosticExpectation
{
    [DataMember(Name = "id", IsRequired = true, Order = 1)]
    public string Id { get; set; } = string.Empty;
    [DataMember(Name = "severity", IsRequired = true, Order = 2)]
    public string Severity { get; set; } = string.Empty;
    [DataMember(Name = "message", IsRequired = true, Order = 3)]
    public string Message { get; set; } = string.Empty;
    [DataMember(Name = "count", IsRequired = true, Order = 4)]
    public int Count { get; set; }
    [DataMember(Name = "innerCauses", IsRequired = true, Order = 5)]
    public string[] InnerCauses { get; set; } = Array.Empty<string>();

    public static EndToEndDiagnosticExpectation[] ValidateAndCopy(IEnumerable<EndToEndDiagnosticExpectation> declarations)
    {
        if (declarations is null) throw Invalid("declarations are absent");
        var rules = declarations.Take(17).ToArray();
        if (rules.Length > 16) throw Invalid("at most 16 declarations are allowed per test");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var messages = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<EndToEndDiagnosticExpectation>();
        foreach (var rule in rules)
        {
            if (rule is null || !LiteralId(rule.Id) || !ids.Add(rule.Id)) throw Invalid("IDs must be distinct bounded literals");
            if (rule.Severity != "ERROR" && rule.Severity != "WARN") throw Invalid("severity must be ERROR or WARN");
            if (rule.Count < 1 || rule.Count > 16) throw Invalid("count must be 1–16");
            if (string.IsNullOrWhiteSpace(rule.Message) || rule.Message.Length > 4096 ||
                rule.Message != rule.Message.Trim() || rule.Message.Any(char.IsControl)) throw Invalid("first line must be exact bounded text");
            ValidateSavedDataTemplate(rule.Message);
            if (!messages.Add(rule.Severity + "\n" + rule.Message)) throw Invalid("parent diagnostics are ambiguous");
            if (rule.InnerCauses is null || rule.InnerCauses.Length > 4 ||
                rule.InnerCauses.Distinct(StringComparer.Ordinal).Count() != rule.InnerCauses.Length)
                throw Invalid("at most four distinct inner causes are allowed");
            foreach (var cause in rule.InnerCauses)
                if (cause is null || cause.Length > 4096 || !cause.StartsWith(" ---> ", StringComparison.Ordinal) ||
                    cause.IndexOf(": ", StringComparison.Ordinal) <= 6 || cause.Any(char.IsControl) ||
                    cause.IndexOfAny(new[] { '{', '}' }) >= 0)
                    throw Invalid("inner causes must be exact native inner-exception lines");
            result.Add(new EndToEndDiagnosticExpectation
            {
                Id = rule.Id, Severity = rule.Severity, Message = rule.Message, Count = rule.Count,
                InnerCauses = (string[])rule.InnerCauses.Clone()
            });
        }
        return result.OrderBy(rule => rule.Id, StringComparer.Ordinal).ToArray();
    }

    private static bool LiteralId(string value) => !string.IsNullOrEmpty(value) && value.Length <= 128 &&
        value.All(character => character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z' ||
            character >= '0' && character <= '9' || character is '.' or '-' or '_' or ':');

    public static bool Equivalent(IEnumerable<EndToEndDiagnosticExpectation> left, IEnumerable<EndToEndDiagnosticExpectation> right)
    {
        var first = ValidateAndCopy(left);
        var second = ValidateAndCopy(right);
        return first.Length == second.Length && first.Zip(second, (a, b) =>
            a.Id == b.Id && a.Severity == b.Severity && a.Message == b.Message && a.Count == b.Count &&
            a.InnerCauses.SequenceEqual(b.InnerCauses, StringComparer.Ordinal)).All(equal => equal);
    }

    private static void ValidateSavedDataTemplate(string message)
    {
        const string token = "{savedata}";
        int start = message.IndexOf(token, StringComparison.Ordinal);
        if (start < 0)
        {
            if (message.IndexOfAny(new[] { '{', '}' }) >= 0) throw Invalid("unknown message token");
            return;
        }
        if (message.Replace(token, string.Empty).IndexOfAny(new[] { '{', '}' }) >= 0 ||
            message.IndexOf(token, start + token.Length, StringComparison.Ordinal) >= 0 || start == 0 || message[start - 1] != '"')
            throw Invalid("only one quoted {savedata} binding is allowed");
        int end = message.IndexOf('"', start + token.Length);
        if (end < 0) throw Invalid("savedata path must be quoted");
        string suffix = message.Substring(start + token.Length, end - start - token.Length);
        if (!suffix.StartsWith("\\Saves\\", StringComparison.Ordinal) && !suffix.StartsWith("/Saves/", StringComparison.Ordinal))
            throw Invalid("savedata binding must name the exact Saves directory");
        string name = suffix.Substring(7);
        if (name.Length == 0 || name.Length > 128 || name.Contains("..") ||
            !name.All(character => character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z' ||
                character >= '0' && character <= '9' || character is '.' or '-' or '_') ||
            !(name.EndsWith(".rws", StringComparison.Ordinal) || name.EndsWith(".rws.new", StringComparison.Ordinal)))
            throw Invalid("savedata binding requires one literal save basename without traversal or wildcards");
    }

    private static ArgumentException Invalid(string reason) => new ArgumentException("Invalid expected diagnostic: " + reason + ".");
}
