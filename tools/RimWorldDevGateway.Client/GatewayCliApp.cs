using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.Client;

public sealed class GatewayCliApp
{
    private const int MaximumDiagnosticLength = 4096;

    private readonly IGatewaySessionProvider sessions;
    private readonly IGatewayHttpTransport transport;
    private readonly IGatewaySourceCompiler compiler;
    private readonly IGatewayClientFileSystem files;

    public GatewayCliApp(
        IGatewaySessionProvider sessions,
        IGatewayHttpTransport transport,
        IGatewaySourceCompiler compiler,
        IGatewayClientFileSystem files)
    {
        this.sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        this.compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
        this.files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        if (output is null)
        {
            throw new ArgumentNullException(nameof(output));
        }

        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        try
        {
            return RunCore(args, output);
        }
        catch (GatewayCliUsageException exception)
        {
            error.WriteLine(Bound(exception.Message));
            error.WriteLine("Run 'RimWorldDevGateway.Client --help' for usage.");
            return 2;
        }
        catch (Exception exception)
        {
            error.WriteLine(Bound(exception.Message));
            return 1;
        }
    }

    private int RunCore(string[] args, TextWriter output)
    {
        if (args.Length == 0 || args.Any(argument => argument is "-h" or "--help"))
        {
            WriteHelp(output);
            return 0;
        }

        var globals = GlobalOptions.Parse(args);
        if (globals.Remaining.Count == 0)
        {
            WriteHelp(output);
            return 0;
        }

        var command = globals.Remaining[0];
        var tail = globals.Remaining.Skip(1).ToArray();
        switch (command)
        {
            case "discover":
                RequireNoArguments(command, tail);
                WriteDiscovery(LoadSession(globals), globals.Output, output);
                return 0;
            case "status":
                RequireNoArguments(command, tail);
                WriteJsonResponse(Send(LoadSession(globals), "GET", "/status", null), globals.Output, output);
                return 0;
            case "ui-state":
                RequireNoArguments(command, tail);
                WriteJsonResponse(Send(LoadSession(globals), "GET", "/ui-state", null), globals.Output, output);
                return 0;
            case "logs":
                RunLogs(globals, tail, output);
                return 0;
            case "screenshot":
                RunScreenshot(globals, tail, output);
                return 0;
            case "click":
                RunClick(globals, tail, output);
                return 0;
            case "drag":
                RunDrag(globals, tail, output);
                return 0;
            case "keys":
                RunKeys(globals, tail, output);
                return 0;
            case "action":
                RunAction(globals, tail, output);
                return 0;
            case "execute-source":
                RunExecuteSource(globals, tail, output);
                return 0;
            case "automations":
                if (tail.Length > 0 && tail[0] == "run")
                {
                    RunAutomation(globals, tail.Skip(1).ToArray(), output);
                }
                else
                {
                    RequireNoArguments(command, tail);
                    WriteJsonResponse(Send(LoadSession(globals), "GET", "/automations", null), globals.Output, output);
                }

                return 0;
            case "run":
                RunAutomation(globals, tail, output);
                return 0;
            case "quickstart":
                RunQuickstart(globals, tail, output);
                return 0;
            case "shutdown":
                RequireNoArguments(command, tail);
                WriteJsonResponse(Send(LoadSession(globals), "POST", "/server/shutdown", "{}"), globals.Output, output);
                return 0;
            default:
                throw new GatewayCliUsageException($"Unknown command '{command}'.");
        }
    }

    private void RunLogs(GlobalOptions globals, string[] tail, TextWriter output)
    {
        var options = CommandOptions.Parse("logs", tail, Array.Empty<string>(), new[] { "after", "limit" }, 0, 0);
        var after = options.Long("after", 0, 0, long.MaxValue);
        var limit = options.Int("limit", 100, 1, 1000);
        WriteJsonResponse(
            Send(LoadSession(globals), "GET", $"/logs?after={after.ToString(CultureInfo.InvariantCulture)}&limit={limit.ToString(CultureInfo.InvariantCulture)}", null),
            globals.Output,
            output);
    }

    private void RunScreenshot(GlobalOptions globals, string[] tail, TextWriter output)
    {
        var options = CommandOptions.Parse(
            "screenshot",
            tail,
            Array.Empty<string>(),
            new[] { "file", "things", "padding" },
            0,
            0);
        var path = options.Required("file");
        var thingsText = options.Value("things");
        var paddingText = options.Value("padding");
        if (thingsText is null && paddingText is not null)
        {
            throw new GatewayCliUsageException("--padding requires --things HANDLE[,HANDLE...].");
        }

        var body = "{}";
        if (thingsText is not null)
        {
            var handles = thingsText
                .Split(',')
                .Select(handle => handle.Trim())
                .ToArray();
            if (handles.Length == 0 || handles.Any(string.IsNullOrWhiteSpace))
            {
                throw new GatewayCliUsageException(
                    "--things must contain one or more comma-separated exact Thing handles.");
            }

            body = GatewayContractJson.Write(new GatewayScreenshotRequest
            {
                ThingHandles = handles.ToList(),
                PaddingPixels = paddingText is null
                    ? null
                    : options.Int("padding", 32, 0, int.MaxValue)
            });
        }

        var response = Send(LoadSession(globals), "POST", "/screenshots", body);
        files.WriteAllBytes(path, response.Body);
        WriteValue(
            new Dictionary<string, object?>
            {
                ["bytes"] = response.Body.Length,
                ["contentType"] = response.ContentType,
                ["file"] = path
            },
            globals.Output,
            output);
    }

    private void RunClick(GlobalOptions globals, string[] tail, TextWriter output)
    {
        var options = CommandOptions.Parse("click", tail, new[] { "no-activate" }, new[] { "x", "y", "button" }, 0, 0);
        var body = GatewayContractJson.Write(new GatewayClickRequest
        {
            X = options.RequiredInt("x"),
            Y = options.RequiredInt("y"),
            Button = options.Value("button") ?? "left",
            Activate = !options.Flag("no-activate")
        });
        WriteJsonResponse(Send(LoadSession(globals), "POST", "/input/click", body), globals.Output, output);
    }

    private void RunDrag(GlobalOptions globals, string[] tail, TextWriter output)
    {
        var options = CommandOptions.Parse(
            "drag",
            tail,
            new[] { "no-activate" },
            new[] { "start-x", "start-y", "end-x", "end-y", "button", "duration-ms", "steps" },
            0,
            0);
        var body = GatewayContractJson.Write(new GatewayDragRequest
        {
            StartX = options.RequiredInt("start-x"),
            StartY = options.RequiredInt("start-y"),
            EndX = options.RequiredInt("end-x"),
            EndY = options.RequiredInt("end-y"),
            Button = options.Value("button") ?? "left",
            DurationMs = options.Int("duration-ms", 250, 1, 10_000),
            Steps = options.Int("steps", 10, 1, 1000),
            Activate = !options.Flag("no-activate")
        });
        WriteJsonResponse(Send(LoadSession(globals), "POST", "/input/drag", body), globals.Output, output);
    }

    private void RunKeys(GlobalOptions globals, string[] tail, TextWriter output)
    {
        var options = CommandOptions.Parse("keys", tail, new[] { "no-activate" }, new[] { "key", "text", "modifiers" }, 0, 0);
        var key = options.Value("key");
        var text = options.Value("text");
        if (string.IsNullOrEmpty(key) == string.IsNullOrEmpty(text))
        {
            throw new GatewayCliUsageException("keys requires exactly one of --key or --text.");
        }

        var modifiers = (options.Value("modifiers") ?? string.Empty)
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .ToList();
        var body = GatewayContractJson.Write(new GatewayKeysRequest
        {
            Key = key,
            Text = text,
            Modifiers = modifiers,
            Activate = !options.Flag("no-activate")
        });
        WriteJsonResponse(Send(LoadSession(globals), "POST", "/input/keys", body), globals.Output, output);
    }

    private void RunAction(GlobalOptions globals, string[] tail, TextWriter output)
    {
        var options = CommandOptions.Parse("action", tail, Array.Empty<string>(), new[] { "arguments" }, 1, 1);
        var body = GatewayContractJson.Write(new GatewaySemanticActionRequest
        {
            Arguments = GatewayClientJson.ParseObject(options.Value("arguments") ?? "{}", "--arguments")
        });
        WriteJsonResponse(
            Send(LoadSession(globals), "POST", "/actions/" + Uri.EscapeDataString(options.Positionals[0]), body),
            globals.Output,
            output);
    }

    private void RunExecuteSource(GlobalOptions globals, string[] tail, TextWriter output)
    {
        var options = CommandOptions.Parse(
            "execute-source",
            tail,
            Array.Empty<string>(),
            new[] { "managed", "contract", "entry-type", "entry-method", "request-json" },
            1,
            1);
        var compileRequest = new GatewaySourceCompilationRequest(
            options.Positionals[0],
            options.Required("managed"),
            options.Required("contract"));
        var assembly = compiler.Compile(compileRequest);
        if (assembly.Length == 0)
        {
            throw new GatewayClientException("The source compiler returned an empty assembly.");
        }

        var request = new GatewayAssemblyExecutionRequest
        {
            AssemblyBase64 = Convert.ToBase64String(assembly),
            EntryType = options.Required("entry-type"),
            EntryMethod = options.Value("entry-method") ?? "Execute",
            RequestJson = options.Value("request-json") ?? "{}"
        };
        WriteJsonResponse(
            Send(LoadSession(globals), "POST", "/executions/assembly", GatewayContractJson.Write(request)),
            globals.Output,
            output);
    }

    private void RunAutomation(GlobalOptions globals, string[] tail, TextWriter output)
    {
        var options = CommandOptions.Parse("run", tail, Array.Empty<string>(), new[] { "arguments", "idempotency-key" }, 1, 1);
        RunAutomationCore(
            globals,
            options.Positionals[0],
            GatewayClientJson.ParseObject(options.Value("arguments") ?? "{}", "--arguments"),
            options.Value("idempotency-key"),
            output);
    }

    private void RunQuickstart(GlobalOptions globals, string[] tail, TextWriter output)
    {
        var options = CommandOptions.Parse("quickstart", tail, Array.Empty<string>(), new[] { "idempotency-key" }, 1, 1);
        var descriptorPath = options.Positionals[0];
        Dictionary<string, object?> descriptor;
        try
        {
            descriptor = GatewayClientJson.ParseObject(files.ReadAllText(descriptorPath), $"quickstart descriptor '{descriptorPath}'");
        }
        catch (GatewayCliUsageException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new GatewayCliUsageException($"Could not read quickstart descriptor '{descriptorPath}': {Bound(exception.Message)}");
        }

        RunAutomationCore(globals, "quickstart.spawn", descriptor, options.Value("idempotency-key"), output);
    }

    private void RunAutomationCore(
        GlobalOptions globals,
        string name,
        Dictionary<string, object?> arguments,
        string? idempotencyKey,
        TextWriter output)
    {
        var body = GatewayContractJson.Write(new GatewayAutomationRunRequest
        {
            Arguments = arguments,
            IdempotencyKey = idempotencyKey
        });
        WriteJsonResponse(
            Send(LoadSession(globals), "POST", "/automations/" + Uri.EscapeDataString(name) + "/runs", body),
            globals.Output,
            output);
    }

    private GatewaySessionManifest LoadSession(GlobalOptions globals) =>
        sessions.Load(globals.ManifestPath, globals.ExpectedProcessId);

    private GatewayClientHttpResponse Send(
        GatewaySessionManifest session,
        string method,
        string relativePath,
        string? jsonBody)
    {
        var baseUrl = session.BaseUrl.TrimEnd('/');
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Authorization"] = "Bearer " + session.Token,
            ["X-Request-Id"] = "cli-" + Guid.NewGuid().ToString("N")
        };
        var request = new GatewayClientHttpRequest(
            method,
            new Uri(baseUrl + relativePath, UriKind.Absolute),
            headers,
            jsonBody is null ? null : "application/json; charset=utf-8",
            jsonBody is null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(jsonBody));
        var response = transport.Send(request);
        if (response.StatusCode is < 200 or >= 300)
        {
            var message = Encoding.UTF8.GetString(response.Body);
            throw new GatewayClientException(
                $"Gateway request failed with HTTP {response.StatusCode.ToString(CultureInfo.InvariantCulture)}: {Bound(message)}");
        }

        return response;
    }

    private static void WriteDiscovery(GatewaySessionManifest manifest, OutputMode mode, TextWriter output)
    {
        WriteValue(
            new Dictionary<string, object?>
            {
                ["apiVersion"] = manifest.ApiVersion,
                ["baseUrl"] = manifest.BaseUrl,
                ["gameVersion"] = manifest.GameVersion,
                ["modVersion"] = manifest.ModVersion,
                ["processId"] = manifest.ProcessId,
                ["processStartUtc"] = manifest.ProcessStartUtc,
                ["runId"] = manifest.RunId,
                ["startedUtc"] = manifest.StartedUtc,
                ["state"] = manifest.State,
                ["unrestrictedExecution"] = manifest.UnrestrictedExecution,
                ["warning"] = manifest.Warning
            },
            mode,
            output);
    }

    private static void WriteJsonResponse(GatewayClientHttpResponse response, OutputMode mode, TextWriter output)
    {
        var json = Encoding.UTF8.GetString(response.Body);
        if (mode == OutputMode.Json)
        {
            output.WriteLine(json);
            return;
        }

        GatewayClientJson.WriteTable(output, GatewayClientJson.ParseValue(json));
    }

    private static void WriteValue(object value, OutputMode mode, TextWriter output)
    {
        if (mode == OutputMode.Json)
        {
            output.WriteLine(GatewayClientJson.Serialize(value));
            return;
        }

        GatewayClientJson.WriteTable(output, value);
    }

    private static void RequireNoArguments(string command, IReadOnlyCollection<string> tail)
    {
        if (tail.Count == 0)
        {
            return;
        }

        var token = tail.First();
        if (token.StartsWith("-", StringComparison.Ordinal))
        {
            throw new GatewayCliUsageException($"Unknown option '{token}' for {command}.");
        }

        throw new GatewayCliUsageException($"Command '{command}' does not accept positional arguments.");
    }

    private static void WriteHelp(TextWriter output)
    {
        output.WriteLine("RimWorld Dev Gateway companion client");
        output.WriteLine();
        output.WriteLine("Usage: RimWorldDevGateway.Client <command> [options] [--manifest PATH] [--pid PID] [-o|--output json|table]");
        output.WriteLine();
        output.WriteLine("Commands:");
        output.WriteLine("  discover                         Validate and show the live session (token is redacted)");
        output.WriteLine("  status | ui-state                Read game or UI state");
        output.WriteLine("  logs [--after N] [--limit N]     Read structured logs");
        output.WriteLine("  screenshot --file PATH [--things HANDLE[,HANDLE...]] [--padding PIXELS]");
        output.WriteLine("  click --x N --y N [--button B]   Send a process-scoped click");
        output.WriteLine("  drag --start-x N --start-y N --end-x N --end-y N");
        output.WriteLine("  keys (--key K|--text TEXT) [--modifiers Ctrl,Shift]");
        output.WriteLine("  action NAME [--arguments JSON]   Invoke a semantic action");
        output.WriteLine("  execute-source FILE --managed DIR --contract DLL --entry-type TYPE");
        output.WriteLine("  automations                      List named automations");
        output.WriteLine("  run NAME [--arguments JSON]      Run a named automation");
        output.WriteLine("  automations run NAME ...         Alias for run");
        output.WriteLine("  quickstart DESCRIPTOR.json       Run quickstart.spawn");
        output.WriteLine("  shutdown                         Stop this gateway session");
        output.WriteLine();
        output.WriteLine("Global output: -o|--output json|table (default: table)");
        output.WriteLine("Exit codes: 0 success, 1 runtime failure, 2 invalid usage.");
        output.WriteLine();
        output.WriteLine("Examples:");
        output.WriteLine("  RimWorldDevGateway.Client status --pid 1234 -o json");
        output.WriteLine("  RimWorldDevGateway.Client screenshot --file crop.png --things Pawn_42,Building_9 --padding 24");
        output.WriteLine("  RimWorldDevGateway.Client execute-source inspect.cs --managed F:\\...\\Managed --contract RimWorldDevGateway.Contracts.dll --entry-type Inspect.Entry");
    }

    private static string Bound(string value) =>
        value.Length <= MaximumDiagnosticLength
            ? value
            : value.Substring(0, MaximumDiagnosticLength) + "...";

    private enum OutputMode
    {
        Json,
        Table
    }

    private sealed class GlobalOptions
    {
        private GlobalOptions(
            OutputMode output,
            string? manifestPath,
            int? expectedProcessId,
            IReadOnlyList<string> remaining)
        {
            Output = output;
            ManifestPath = manifestPath;
            ExpectedProcessId = expectedProcessId;
            Remaining = remaining;
        }

        public OutputMode Output { get; }

        public string? ManifestPath { get; }

        public int? ExpectedProcessId { get; }

        public IReadOnlyList<string> Remaining { get; }

        public static GlobalOptions Parse(IReadOnlyList<string> arguments)
        {
            var remaining = new List<string>();
            string? output = null;
            string? manifest = null;
            string? pidText = null;
            for (var index = 0; index < arguments.Count; index++)
            {
                var token = arguments[index];
                if (token is "-o" or "--output")
                {
                    output = TakeUniqueValue(arguments, ref index, token, output);
                }
                else if (token == "--manifest")
                {
                    manifest = TakeUniqueValue(arguments, ref index, token, manifest);
                }
                else if (token == "--pid")
                {
                    pidText = TakeUniqueValue(arguments, ref index, token, pidText);
                }
                else
                {
                    remaining.Add(token);
                }
            }

            var mode = output switch
            {
                null or "table" => OutputMode.Table,
                "json" => OutputMode.Json,
                _ => throw new GatewayCliUsageException("--output must be 'json' or 'table'.")
            };
            int? pid = null;
            if (pidText is not null)
            {
                if (!int.TryParse(pidText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedPid) || parsedPid <= 0)
                {
                    throw new GatewayCliUsageException("--pid must be a positive integer.");
                }

                pid = parsedPid;
            }

            return new GlobalOptions(mode, manifest, pid, remaining);
        }

        private static string TakeUniqueValue(
            IReadOnlyList<string> arguments,
            ref int index,
            string option,
            string? previous)
        {
            if (previous is not null)
            {
                throw new GatewayCliUsageException($"Option '{option}' may be specified only once.");
            }

            if (++index >= arguments.Count)
            {
                throw new GatewayCliUsageException($"Option '{option}' requires a value.");
            }

            return arguments[index];
        }
    }

    private sealed class CommandOptions
    {
        private readonly string command;
        private readonly Dictionary<string, string> values;
        private readonly HashSet<string> flags;

        private CommandOptions(
            string command,
            Dictionary<string, string> values,
            HashSet<string> flags,
            IReadOnlyList<string> positionals)
        {
            this.command = command;
            this.values = values;
            this.flags = flags;
            Positionals = positionals;
        }

        public IReadOnlyList<string> Positionals { get; }

        public static CommandOptions Parse(
            string command,
            IReadOnlyList<string> arguments,
            IEnumerable<string> permittedFlags,
            IEnumerable<string> permittedValues,
            int minimumPositionals,
            int maximumPositionals)
        {
            var allowedFlags = new HashSet<string>(permittedFlags, StringComparer.Ordinal);
            var allowedValues = new HashSet<string>(permittedValues, StringComparer.Ordinal);
            var flags = new HashSet<string>(StringComparer.Ordinal);
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            var positionals = new List<string>();
            for (var index = 0; index < arguments.Count; index++)
            {
                var token = arguments[index];
                if (!token.StartsWith("--", StringComparison.Ordinal))
                {
                    if (token.StartsWith("-", StringComparison.Ordinal))
                    {
                        throw new GatewayCliUsageException($"Unknown option '{token}' for {command}.");
                    }

                    positionals.Add(token);
                    continue;
                }

                var name = token.Substring(2);
                if (allowedFlags.Contains(name))
                {
                    if (!flags.Add(name))
                    {
                        throw new GatewayCliUsageException($"Option '--{name}' may be specified only once.");
                    }

                    continue;
                }

                if (!allowedValues.Contains(name))
                {
                    throw new GatewayCliUsageException($"Unknown option '{token}' for {command}.");
                }

                if (values.ContainsKey(name))
                {
                    throw new GatewayCliUsageException($"Option '--{name}' may be specified only once.");
                }

                if (++index >= arguments.Count)
                {
                    throw new GatewayCliUsageException($"Option '--{name}' requires a value.");
                }

                values.Add(name, arguments[index]);
            }

            if (positionals.Count < minimumPositionals || positionals.Count > maximumPositionals)
            {
                throw new GatewayCliUsageException(
                    minimumPositionals == maximumPositionals
                        ? $"Command '{command}' requires exactly {minimumPositionals} positional argument(s)."
                        : $"Command '{command}' requires between {minimumPositionals} and {maximumPositionals} positional arguments.");
            }

            return new CommandOptions(command, values, flags, positionals);
        }

        public bool Flag(string name) => flags.Contains(name);

        public string? Value(string name) => values.TryGetValue(name, out var value) ? value : null;

        public string Required(string name)
        {
            var value = Value(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new GatewayCliUsageException($"Command '{command}' requires --{name} VALUE.");
            }

            return value!;
        }

        public int RequiredInt(string name)
        {
            var text = Required(name);
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                throw new GatewayCliUsageException($"--{name} must be an integer.");
            }

            return value;
        }

        public int Int(string name, int defaultValue, int minimum, int maximum)
        {
            var text = Value(name);
            if (text is null)
            {
                return defaultValue;
            }

            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < minimum || value > maximum)
            {
                throw new GatewayCliUsageException($"--{name} must be an integer from {minimum} through {maximum}.");
            }

            return value;
        }

        public long Long(string name, long defaultValue, long minimum, long maximum)
        {
            var text = Value(name);
            if (text is null)
            {
                return defaultValue;
            }

            if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < minimum || value > maximum)
            {
                throw new GatewayCliUsageException($"--{name} must be an integer from {minimum} through {maximum}.");
            }

            return value;
        }
    }
}
