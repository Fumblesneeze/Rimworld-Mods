using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Verse;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class VerseGatewayDefSourceTests
{
    [Test]
    public void Exact_type_and_name_filters_use_direct_lookup_and_stop_at_page_plus_one()
    {
        var database = new RecordingDatabase(
            new ProbeDef { defName = "Alpha" },
            new ProbeDef { defName = "Beta" },
            new ProbeDef { defName = "Gamma" });
        var source = new VerseGatewayDefSource(database);
        var query = new GatewayDefSourceQuery(
            new[] { typeof(ProbeDef).FullName! },
            new[] { "Gamma", "Alpha", "Beta" },
            Array.Empty<string>(),
            afterDatabaseType: null,
            afterDefName: null,
            limit: 1);

        var page = source.CapturePage(query, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(page.Records.Select(record => record.Value.defName), Is.EqualTo(new[] { "Alpha" }));
            Assert.That(page.Truncated, Is.True);
            Assert.That(database.ResolveCalls, Is.EqualTo(1));
            Assert.That(database.NamedLookups, Is.EqualTo(new[] { "Alpha", "Beta" }));
            Assert.That(database.AllTypeEnumerationCount, Is.Zero);
            Assert.That(database.DefEnumerationCount, Is.Zero);
        });
    }

    [Test]
    public void Unnamed_query_scans_one_bounded_database_and_stops_before_later_database_boundaries()
    {
        var database = new ScanningDatabase(
            new Dictionary<Type, Func<IEnumerable<Def>>>
            {
                [typeof(AProbeDef)] = () => new Def[]
                {
                    new AProbeDef { defName = "Charlie" },
                    new AProbeDef { defName = "Alpha" },
                    new AProbeDef { defName = "Bravo" }
                },
                [typeof(ZProbeDef)] = () => new Def[] { new ZProbeDef { defName = "Later" } }
            });
        var source = new VerseGatewayDefSource(database);

        var page = source.CapturePage(
            new GatewayDefSourceQuery(
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                null,
                null,
                limit: 1),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(page.Records.Select(record => record.Value.defName), Is.EqualTo(new[] { "Alpha" }));
            Assert.That(page.Truncated, Is.True);
            Assert.That(database.EnumerationCounts[typeof(AProbeDef)], Is.EqualTo(1));
            Assert.That(database.EnumerationCounts[typeof(ZProbeDef)], Is.Zero);
        });
    }

    [Test]
    public void Broken_database_is_an_explicit_bounded_warning_and_does_not_hide_later_databases()
    {
        var database = new ScanningDatabase(
            new Dictionary<Type, Func<IEnumerable<Def>>>
            {
                [typeof(AProbeDef)] = () => throw new InvalidOperationException("broken database"),
                [typeof(ZProbeDef)] = () => new Def[] { new ZProbeDef { defName = "Healthy" } }
            });
        var source = new VerseGatewayDefSource(database);

        var page = source.CapturePage(
            new GatewayDefSourceQuery(
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                null,
                null,
                limit: 4),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(page.Records.Select(record => record.Value.defName), Is.EqualTo(new[] { "Healthy" }));
            Assert.That(page.SourceWarnings, Has.Some.Property("Code").EqualTo("def_database_enumeration_failed"));
            Assert.That(page.SourceWarnings[0].Message.Length, Is.LessThanOrEqualTo(1024));
        });
    }

    [Test]
    public void Unrelated_operation_cancellation_is_isolated_without_reading_virtual_exception_text()
    {
        var failure = new HostileOperationCanceledException();
        var database = new ScanningDatabase(
            new Dictionary<Type, Func<IEnumerable<Def>>>
            {
                [typeof(AProbeDef)] = () => throw failure,
                [typeof(ZProbeDef)] = () => new Def[] { new ZProbeDef { defName = "Healthy" } }
            });
        var source = new VerseGatewayDefSource(database);

        var page = source.CapturePage(
            new GatewayDefSourceQuery(
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                null,
                null,
                limit: 4),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(page.Records.Select(record => record.Value.defName), Is.EqualTo(new[] { "Healthy" }));
            Assert.That(page.SourceWarnings, Has.Some.Property("Code").EqualTo("def_database_enumeration_failed"));
            Assert.That(page.SourceWarnings[0].Message, Does.Contain("diagnostic message suppressed"));
            Assert.That(failure.VirtualTextReadCount, Is.Zero);
        });
    }

    [Test]
    public void Non_reversible_def_identity_is_omitted_with_a_warning_while_later_defs_remain_pageable()
    {
        var database = new ScanningDatabase(
            new Dictionary<Type, Func<IEnumerable<Def>>>
            {
                [typeof(AProbeDef)] = () => new Def[]
                {
                    new AProbeDef { defName = "Bad\ud800Identity" },
                    new AProbeDef { defName = "Healthy" }
                }
            });
        var source = new VerseGatewayDefSource(database);

        var page = source.CapturePage(
            new GatewayDefSourceQuery(
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                null,
                null,
                limit: 4),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(page.Records.Select(record => record.Value.defName), Is.EqualTo(new[] { "Healthy" }));
            Assert.That(page.SourceWarnings, Has.Some.Property("Code").EqualTo("def_identity_not_reversible"));
        });
    }

    [Test]
    public void Unnamed_query_enforces_one_aggregate_candidate_scan_limit_across_databases()
    {
        var first = new AProbeDef { defName = "Repeated-A" };
        var second = new BProbeDef { defName = "Repeated-B" };
        var third = new ZProbeDef { defName = "Repeated-Z" };
        var database = new ScanningDatabase(
            new Dictionary<Type, Func<IEnumerable<Def>>>
            {
                [typeof(AProbeDef)] = () => Enumerable.Repeat<Def>(
                    first,
                    VerseGatewayDefSource.MaximumDefsScannedPerDatabase),
                [typeof(BProbeDef)] = () => Enumerable.Repeat<Def>(
                    second,
                    VerseGatewayDefSource.MaximumDefsScannedPerDatabase),
                [typeof(ZProbeDef)] = () => Enumerable.Repeat<Def>(third, 1)
            });
        var source = new VerseGatewayDefSource(database);

        var exception = Assert.Throws<GatewayDefExportException>(() =>
            source.CapturePage(
                new GatewayDefSourceQuery(
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    null,
                    null,
                    limit: GatewayDefExporter.MaximumPageSize),
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo("def_source_scan_limit"));
            Assert.That(database.EnumerationCounts[typeof(AProbeDef)], Is.EqualTo(1));
            Assert.That(database.EnumerationCounts[typeof(BProbeDef)], Is.EqualTo(1));
            Assert.That(database.EnumerationCounts[typeof(ZProbeDef)], Is.EqualTo(1));
        });
    }

    [Test]
    public void Database_type_discovery_counts_rejected_candidates_against_its_scan_limit()
    {
        var database = new InvalidTypeScanningDatabase();
        var source = new VerseGatewayDefSource(database);

        var exception = Assert.Throws<GatewayDefExportException>(() =>
            source.CapturePage(
                new GatewayDefSourceQuery(
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    null,
                    null,
                    limit: 1),
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo("def_database_type_scan_limit"));
            Assert.That(
                database.TypeCandidatesRead,
                Is.EqualTo(VerseGatewayDefSource.MaximumDatabaseTypesScanned + 1));
            Assert.That(database.DefEnumerationCount, Is.Zero);
        });
    }

    private sealed class RecordingDatabase : IGatewayVerseDefDatabase
    {
        private readonly IReadOnlyDictionary<string, Def> defs;

        public RecordingDatabase(params Def[] defs)
        {
            this.defs = defs.ToDictionary(def => def.defName, StringComparer.Ordinal);
        }

        public int ResolveCalls { get; private set; }

        public int AllTypeEnumerationCount { get; private set; }

        public int DefEnumerationCount { get; private set; }

        public List<string> NamedLookups { get; } = new();

        public Type? ResolveDefType(string fullName)
        {
            ResolveCalls++;
            return string.Equals(fullName, typeof(ProbeDef).FullName, StringComparison.Ordinal)
                ? typeof(ProbeDef)
                : null;
        }

        public IEnumerable<Type> AllDatabaseTypes()
        {
            AllTypeEnumerationCount++;
            yield return typeof(ProbeDef);
        }

        public Def? GetNamed(Type databaseType, string defName)
        {
            NamedLookups.Add(defName);
            return defs.TryGetValue(defName, out var value) ? value : null;
        }

        public IEnumerable<Def> AllDefs(Type databaseType)
        {
            DefEnumerationCount++;
            throw new InvalidOperationException("Exact-name export must not enumerate a database.");
        }
    }

    private sealed class ScanningDatabase : IGatewayVerseDefDatabase
    {
        private readonly IReadOnlyDictionary<Type, Func<IEnumerable<Def>>> sources;

        public ScanningDatabase(IReadOnlyDictionary<Type, Func<IEnumerable<Def>>> sources)
        {
            this.sources = sources;
            EnumerationCounts = sources.Keys.ToDictionary(type => type, _ => 0);
        }

        public Dictionary<Type, int> EnumerationCounts { get; }

        public Type? ResolveDefType(string fullName) =>
            sources.Keys.SingleOrDefault(type => string.Equals(type.FullName, fullName, StringComparison.Ordinal));

        public IEnumerable<Type> AllDatabaseTypes() => sources.Keys;

        public Def? GetNamed(Type databaseType, string defName) =>
            sources[databaseType]().SingleOrDefault(def => string.Equals(def.defName, defName, StringComparison.Ordinal));

        public IEnumerable<Def> AllDefs(Type databaseType)
        {
            EnumerationCounts[databaseType]++;
            return sources[databaseType]();
        }
    }

    private sealed class InvalidTypeScanningDatabase : IGatewayVerseDefDatabase
    {
        public int TypeCandidatesRead { get; private set; }

        public int DefEnumerationCount { get; private set; }

        public Type? ResolveDefType(string fullName) => null;

        public IEnumerable<Type> AllDatabaseTypes()
        {
            for (var index = 0; index <= VerseGatewayDefSource.MaximumDatabaseTypesScanned; index++)
            {
                TypeCandidatesRead++;
                yield return null!;
            }

            TypeCandidatesRead++;
            yield return typeof(AProbeDef);
        }

        public Def? GetNamed(Type databaseType, string defName) => null;

        public IEnumerable<Def> AllDefs(Type databaseType)
        {
            DefEnumerationCount++;
            return new Def[] { new AProbeDef { defName = "TooLate" } };
        }
    }

    private sealed class ProbeDef : Def
    {
    }

    private sealed class AProbeDef : Def
    {
    }

    private sealed class BProbeDef : Def
    {
    }

    private sealed class ZProbeDef : Def
    {
    }

    private sealed class HostileOperationCanceledException : OperationCanceledException
    {
        public int VirtualTextReadCount { get; private set; }

        public override string Message
        {
            get
            {
                VirtualTextReadCount++;
                throw new InvalidOperationException("Message must not be invoked.");
            }
        }

        public override string ToString()
        {
            VirtualTextReadCount++;
            throw new InvalidOperationException("ToString must not be invoked.");
        }
    }
}
