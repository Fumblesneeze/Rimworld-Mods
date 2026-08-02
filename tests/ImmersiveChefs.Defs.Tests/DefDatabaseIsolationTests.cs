using NUnit.Framework;
using Verse;

namespace ImmersiveChefs.Defs.Tests;

[TestFixture]
[NonParallelizable]
public sealed class DefDatabaseIsolationTests
{
    [Test]
    public void Constructed_test_def_is_available_only_inside_its_database_scope()
    {
        var def = new CulinaryProbeDef
        {
            defName = "IC_TestCookware",
            label = "test cookware",
            cleanliness = -0.25f,
            capacity = 16
        };

        Assert.That(DefDatabase<CulinaryProbeDef>.GetNamedSilentFail(def.defName), Is.Null);

        using (DefDatabaseScope<CulinaryProbeDef>.Register(def))
        {
            var loaded = DefDatabase<CulinaryProbeDef>.GetNamedSilentFail("IC_TestCookware");

            Assert.Multiple(() =>
            {
                Assert.That(loaded, Is.SameAs(def));
                Assert.That(loaded.label, Is.EqualTo("test cookware"));
                Assert.That(loaded.cleanliness, Is.EqualTo(-0.25f));
                Assert.That(loaded.capacity, Is.EqualTo(16));
            });
        }

        Assert.That(DefDatabase<CulinaryProbeDef>.GetNamedSilentFail("IC_TestCookware"), Is.Null);
    }

    [Test]
    public void All_constructed_required_defs_are_available_only_inside_one_database_scope()
    {
        var cookware = new CulinaryProbeDef
        {
            defName = "IC_RequiredCookware",
            cleanliness = 0.5f,
            capacity = 4
        };
        var dishwasher = new CulinaryProbeDef
        {
            defName = "IC_RequiredDishwasher",
            cleanliness = 1f,
            capacity = 16
        };

        Assert.Multiple(() =>
        {
            Assert.That(DefDatabase<CulinaryProbeDef>.GetNamedSilentFail(cookware.defName), Is.Null);
            Assert.That(DefDatabase<CulinaryProbeDef>.GetNamedSilentFail(dishwasher.defName), Is.Null);
        });

        using (DefDatabaseScope<CulinaryProbeDef>.Register(cookware, dishwasher))
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    DefDatabase<CulinaryProbeDef>.GetNamedSilentFail(cookware.defName),
                    Is.SameAs(cookware));
                Assert.That(
                    DefDatabase<CulinaryProbeDef>.GetNamedSilentFail(dishwasher.defName),
                    Is.SameAs(dishwasher));
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(DefDatabase<CulinaryProbeDef>.GetNamedSilentFail(cookware.defName), Is.Null);
            Assert.That(DefDatabase<CulinaryProbeDef>.GetNamedSilentFail(dishwasher.defName), Is.Null);
        });
    }

    [Test]
    public void Database_scope_refuses_a_nonempty_database_without_changing_name_or_short_hash_lookups()
    {
        var first = new CulinaryProbeDef { defName = "IC_PreexistingFirst", shortHash = 501 };
        var second = new CulinaryProbeDef { defName = "IC_PreexistingSecond", shortHash = 502 };
        var scoped = new CulinaryProbeDef { defName = "IC_Scoped", shortHash = 503 };
        DefDatabaseScope<CulinaryProbeDef>? unexpectedScope = null;

        try
        {
            DefDatabase<CulinaryProbeDef>.Add(new[] { first, second });
            var shortHashDatabase = GetShortHashDatabase();
            shortHashDatabase.Add(first.shortHash, first);
            shortHashDatabase.Add(second.shortHash, second);

            Assert.That(DefDatabase<CulinaryProbeDef>.GetByShortHash(501), Is.SameAs(first));
            Assert.That(
                () => unexpectedScope = DefDatabaseScope<CulinaryProbeDef>.Register(scoped),
                Throws.TypeOf<InvalidOperationException>());

            Assert.That(
                DefDatabase<CulinaryProbeDef>.AllDefsListForReading,
                Is.EqualTo(new[] { first, second }));
            Assert.Multiple(() =>
            {
                Assert.That(DefDatabase<CulinaryProbeDef>.GetNamedSilentFail(first.defName), Is.SameAs(first));
                Assert.That(DefDatabase<CulinaryProbeDef>.GetNamedSilentFail(second.defName), Is.SameAs(second));
                Assert.That(DefDatabase<CulinaryProbeDef>.GetNamedSilentFail(scoped.defName), Is.Null);
                Assert.That(DefDatabase<CulinaryProbeDef>.GetByShortHash(501), Is.SameAs(first));
                Assert.That(DefDatabase<CulinaryProbeDef>.GetByShortHash(502), Is.SameAs(second));
                Assert.That(DefDatabase<CulinaryProbeDef>.GetByShortHash(503), Is.Null);
            });
        }
        finally
        {
            unexpectedScope?.Dispose();
            DefDatabase<CulinaryProbeDef>.Clear();
        }
    }

    private static IDictionary<ushort, CulinaryProbeDef> GetShortHashDatabase()
    {
        var field = typeof(DefDatabase<CulinaryProbeDef>).GetField(
            "defsByShortHash",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (IDictionary<ushort, CulinaryProbeDef>)field!.GetValue(null)!;
    }

    public sealed class CulinaryProbeDef : Def
    {
        public float cleanliness;
        public int capacity;
    }
}
