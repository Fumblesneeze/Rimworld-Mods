using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;

namespace RealRuinsCorpus;

public static class CorpusCli
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static Task<int> InvokeAsync(string[] args, TextWriter output, TextWriter error)
    {
        var root = new RootCommand("Safely download and measure a reproducible Real Ruins blueprint corpus.");
        var analyze = new Command("analyze", "Download or resume a corpus and write placement statistics.");
        var metadataLimit = new Option<int>("--metadata-limit") { DefaultValueFactory = _ => 5000 };
        var blueprintLimit = new Option<int>("--blueprint-limit") { DefaultValueFactory = _ => 2000 };
        var concurrency = new Option<int>("--concurrency") { DefaultValueFactory = _ => 12 };
        var outputDirectory = new Option<string>("--output-directory") { Required = true };
        var outputFormat = new Option<string>("--output", "-o") { DefaultValueFactory = _ => "table" };
        analyze.Options.Add(metadataLimit);
        analyze.Options.Add(blueprintLimit);
        analyze.Options.Add(concurrency);
        analyze.Options.Add(outputDirectory);
        analyze.Options.Add(outputFormat);
        analyze.SetAction(parseResult =>
        {
            try
            {
                var options = new CorpusRunOptions(
                    parseResult.GetValue(metadataLimit),
                    parseResult.GetValue(blueprintLimit),
                    parseResult.GetValue(concurrency),
                    parseResult.GetValue(outputDirectory)!,
                    parseResult.GetValue(outputFormat)!);
                var result = CorpusRunner.RunAsync(options, CancellationToken.None).GetAwaiter().GetResult();
                if (string.Equals(options.OutputFormat, "json", StringComparison.OrdinalIgnoreCase))
                {
                    output.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
                }
                else
                {
                    output.WriteLine($"Metadata: {result.MetadataCount:N0}");
                    output.WriteLine($"Blueprints parsed: {result.ParsedCount:N0}/{result.RequestedBlueprintCount:N0} target ({result.AttemptedBlueprintCount:N0} attempted)");
                    output.WriteLine($"Rejected: {result.RejectedCount:N0}");
                    output.WriteLine($"Summary: {result.SummaryPath}");
                }

                return result.ParsedCount >= options.BlueprintLimit ? 0 : 1;
            }
            catch (ArgumentException exception)
            {
                error.WriteLine(exception.Message);
                return 2;
            }
            catch (Exception exception)
            {
                error.WriteLine(exception.Message);
                return 1;
            }
        });
        root.Subcommands.Add(analyze);
        var render = new Command("render", "Render a blueprint body as a schematic SVG for visual review.");
        var blueprintPath = new Option<string>("--blueprint") { Required = true };
        var renderOutput = new Option<string>("--output-file") { Required = true };
        var scale = new Option<int>("--scale") { DefaultValueFactory = _ => 4 };
        render.Options.Add(blueprintPath);
        render.Options.Add(renderOutput);
        render.Options.Add(scale);
        render.SetAction(parseResult =>
        {
            try
            {
                var source = Path.GetFullPath(parseResult.GetValue(blueprintPath)!);
                var destination = Path.GetFullPath(parseResult.GetValue(renderOutput)!);
                using var stream = File.OpenRead(source);
                var analysis = BlueprintAnalyzer.Analyze(stream, Path.GetFileNameWithoutExtension(source));
                var svg = BlueprintSvgRenderer.Render(analysis, parseResult.GetValue(scale));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.WriteAllText(destination, svg, new System.Text.UTF8Encoding(false));
                output.WriteLine(destination);
                return 0;
            }
            catch (ArgumentException exception)
            {
                error.WriteLine(exception.Message);
                return 2;
            }
            catch (Exception exception)
            {
                error.WriteLine(exception.Message);
                return 1;
            }
        });
        root.Subcommands.Add(render);

        var shortlist = new Command(
            "shortlist",
            "Rank downloaded Real Ruins blueprints that contain compact residential, dining, cooking and food-storage context.");
        var blueprintDirectory = new Option<string>("--blueprint-directory") { Required = true };
        var shortlistLimit = new Option<int>("--limit") { DefaultValueFactory = _ => 24 };
        var shortlistOutput = new Option<string>("--output-file") { Required = true };
        shortlist.Options.Add(blueprintDirectory);
        shortlist.Options.Add(shortlistLimit);
        shortlist.Options.Add(shortlistOutput);
        shortlist.SetAction(parseResult =>
        {
            try
            {
                var result = FoodColonyShortlister.ShortlistAsync(
                    parseResult.GetValue(blueprintDirectory)!,
                    parseResult.GetValue(shortlistLimit),
                    CancellationToken.None).GetAwaiter().GetResult();
                var destination = Path.GetFullPath(parseResult.GetValue(shortlistOutput)!);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.WriteAllText(
                    destination,
                    JsonSerializer.Serialize(result, JsonOptions) + Environment.NewLine,
                    new System.Text.UTF8Encoding(false));
                output.WriteLine(destination);
                return result.Candidates.Count > 0 ? 0 : 1;
            }
            catch (ArgumentException exception)
            {
                error.WriteLine(exception.Message);
                return 2;
            }
            catch (Exception exception)
            {
                error.WriteLine(exception.Message);
                return 1;
            }
        });
        root.Subcommands.Add(shortlist);

        var parse = root.Parse(args);
        if (parse.Errors.Count > 0)
        {
            foreach (var parseError in parse.Errors)
            {
                error.WriteLine(parseError.Message);
            }

            return Task.FromResult(2);
        }

        return Task.FromResult(parse.Invoke(new InvocationConfiguration { Output = output, Error = error }));
    }
}
