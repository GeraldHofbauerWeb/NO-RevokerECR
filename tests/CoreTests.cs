using System.Collections.Generic;
using System.Linq;
using NORevokerECR.Core;
using Xunit;

namespace NORevokerECR.Tests
{
    public class FlareSplitTests
    {
        [Fact]
        public void TwoEqualEjectorsShareEvenly()
        {
            Assert.Equal(new[] { 43, 43 }, FlareSplit.Split(86, new[] { 30, 30 }));
        }

        [Fact]
        public void ProportionalAndExactSum()
        {
            int[] split = FlareSplit.Split(86, new[] { 10, 20, 30 });
            Assert.Equal(86, split.Sum());
            Assert.True(split[0] < split[1] && split[1] < split[2]);
        }

        [Fact]
        public void OddTotalGivesTheExtraToTheFirst()
        {
            Assert.Equal(new[] { 44, 43 }, FlareSplit.Split(87, new[] { 30, 30 }));
        }

        [Fact]
        public void EmptyEjectorsAreFilledEvenly()
        {
            Assert.Equal(new[] { 43, 43 }, FlareSplit.Split(86, new[] { 0, 0 }));
        }

        [Fact]
        public void SingleEjectorGetsAll()
        {
            Assert.Equal(new[] { 86 }, FlareSplit.Split(86, new[] { 60 }));
        }

        [Fact]
        public void NoEjectorsNoFlares()
        {
            Assert.Empty(FlareSplit.Split(86, new int[0]));
        }
    }

    public class WeaponTableTests
    {
        [Fact]
        public void FourStationsAsOnTheFs12()
        {
            Assert.Equal(new[] { "Internal Cannon", "Internal Weapon Bays", "Wing Pylons", "Wingtip Pylons" },
                WeaponTable.Stations.Select(s => s.Name));
        }

        [Fact]
        public void EveryDefaultIsOneOfItsOptions()
        {
            foreach (Station s in WeaponTable.Stations)
                Assert.Contains(s.Default, s.Options);
        }

        [Fact]
        public void NoStationListsAMountTwice()
        {
            foreach (Station s in WeaponTable.Stations)
                Assert.Equal(s.Options.Length, s.Options.Distinct().Count());
        }

        [Fact]
        public void InternalCopiesAreOfferedInTheBays()
        {
            Station bays = WeaponTable.Stations.Single(s => s.Name == "Internal Weapon Bays");
            foreach (InternalCopy c in WeaponTable.InternalCopies)
            {
                Assert.Contains(c.Key, bays.Options);
                Assert.StartsWith("EA12_", c.Key);
                Assert.NotEqual(c.Key, c.SourceKey);
            }
        }

        [Fact]
        public void DefaultsMatchSebsList()
        {
            Assert.Equal(new[] { "gun_20mm_internal_500", "AAM2_single_internal", "JammingPod1", "ARM1_single" },
                WeaponTable.Stations.Select(s => s.Default));
        }

        [Fact]
        public void CorrectedNamesAreUsed()
        {
            var all = new HashSet<string>(WeaponTable.AllKeys());
            Assert.Contains("bomb_glide1_double_inernal", all);   // the game's own spelling
            Assert.Contains("bomb_penetrator1_mount", all);
            Assert.DoesNotContain("AGM_heavy_double", all);
        }

        [Fact]
        public void WingPylonsFitWithoutScrolling()
        {
            Station wing = WeaponTable.Stations.Single(s => s.Name == "Wing Pylons");
            Assert.DoesNotContain("AGM_heavyx2", wing.Options);
            Assert.DoesNotContain("AAM1_single", wing.Options);
            Assert.Contains("AGM_heavy_triple", wing.Options);
            Assert.Equal(23, wing.Options.Length);
            Assert.Contains("AAM1_single", WeaponTable.Stations.Single(s => s.Name == "Wingtip Pylons").Options);
        }

        [Fact]
        public void NothingNuclear()
        {
            foreach (string key in WeaponTable.AllKeys())
            {
                foreach (string marker in new[] { "nuclear", "Nuke", "20kt", "Genie" })
                    Assert.DoesNotContain(marker, key, System.StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    public class SpecTests
    {
        [Fact]
        public void PrefabHashSpellsEA12()
        {
            Assert.Equal("EA12", string.Concat(new[] { 24, 16, 8, 0 }.Select(s => (char)((EcrSpec.PrefabHash >> s) & 0xFF))));
        }

        [Fact]
        public void CapacitorIsAFixed750()
        {
            Assert.Equal(750f, EcrSpec.CapacitorKJ);
            // Pods no longer add anything, so there is no per-pod value at all.
            Assert.Null(typeof(EcrSpec).GetField("PodCapacitorKJ"));
        }

        [Fact]
        public void DescriptionIsSebsText()
        {
            Assert.StartsWith("Electronic combat and reconnaissance variant of the FS-12 Revoker.", EcrSpec.Description);
            Assert.Contains("at the cost of increased weight", EcrSpec.Description);
            Assert.EndsWith("reduced internal weapon bay volume. Not nuclear-capable.", EcrSpec.Description);
        }

        [Fact]
        public void WeightAndOpticsAfterTheFirstTest()
        {
            Assert.Equal(2020f, EcrSpec.ExtraEmptyMassKg);
            Assert.Equal(10700f, 8680f + EcrSpec.ExtraEmptyMassKg);   // FS-12 empty weight 8680 kg (dump)
            Assert.Equal(16000f, EcrSpec.OpticalRangeM);
            // 4096 m battlefield grid on the tested map: 16 km = 4 squares, 12 km = 3
            Assert.Equal(4, (int)System.Math.Ceiling(EcrSpec.OpticalRangeM / 4096f));
        }

        [Fact]
        public void KeysDifferFromTheOriginal()
        {
            Assert.NotEqual(EcrSpec.OriginalKey, EcrSpec.JsonKey);
            Assert.Equal("EA-12", EcrSpec.ShortName);
            Assert.StartsWith(EcrSpec.ShortName + " ", EcrSpec.FullName);
        }
    }
}

namespace NORevokerECR.Tests
{
    public class ServerTests
    {
        // A process filter on "NuclearOption.exe" kept the plugin out of the
        // dedicated server (NuclearOptionServer), so the server reported a
        // version without the EA-12 tag and players with the mod could not
        // see it in the server list.
        [Fact]
        public void PluginHasNoProcessFilter()
        {
            var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
            while (dir != null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "src", "Plugin.cs")))
                dir = dir.Parent;
            Assert.NotNull(dir);
            string plugin = System.IO.File.ReadAllText(System.IO.Path.Combine(dir.FullName, "src", "Plugin.cs"));
            Assert.DoesNotContain("[BepInProcess(", plugin);
        }
    }

    public class VersionTagTests
    {
        [Fact]
        public void AppendsTheModVersion()
        {
            Assert.Equal("0.34.6_NO-RevokerECR-v0.2.0", VersionTag.Append("0.34.6", "0.2.0"));
        }

        [Fact]
        public void DoesNotAppendTwice()
        {
            string once = VersionTag.Append("0.34.6", "0.2.0");
            Assert.Equal(once, VersionTag.Append(once, "0.2.0"));
        }

        [Fact]
        public void KeepsWhatOtherModsAddedBefore()
        {
            Assert.Equal("0.34.6_other-v1_NO-RevokerECR-v0.2.0", VersionTag.Append("0.34.6_other-v1", "0.2.0"));
        }

        [Fact]
        public void NullGameVersionStillTagged()
        {
            Assert.Equal("_NO-RevokerECR-v0.2.0", VersionTag.Append(null, "0.2.0"));
        }
    }
}
