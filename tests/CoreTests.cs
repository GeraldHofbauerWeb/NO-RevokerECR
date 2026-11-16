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
            Assert.Contains("AGM_heavyx2", all);
            Assert.Contains("bomb_penetrator1_mount", all);
            Assert.DoesNotContain("AGM_heavy_double", all);
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
        public void KeysDifferFromTheOriginal()
        {
            Assert.NotEqual(EcrSpec.OriginalKey, EcrSpec.JsonKey);
            Assert.Equal("EA-12", EcrSpec.ShortName);
            Assert.StartsWith(EcrSpec.ShortName + " ", EcrSpec.FullName);
        }
    }
}
