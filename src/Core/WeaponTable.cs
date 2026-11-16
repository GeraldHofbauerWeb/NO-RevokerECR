using System.Collections.Generic;

namespace NORevokerECR.Core
{
    // One hardpoint set of the aircraft: its name as the game shows it, the
    // weapon mounts it offers (game jsonKeys) and the one preselected.
    public sealed class Station
    {
        public string Name;
        public string Default;
        public string[] Options;
    }

    // A weapon mount the mod creates: a copy of SourceKey without radar
    // signature and drag, for carriage in the internal bays.
    public sealed class InternalCopy
    {
        public string Key;
        public string SourceKey;
    }

    // The EA-12's weapon options (Seb, 2026-11-16). The four hardpoint sets
    // are the FS-12's; only what they offer changes. Every set keeps the
    // game's "empty" choice first, so that is not listed here.
    public static class WeaponTable
    {
        public static readonly InternalCopy[] InternalCopies =
        {
            new InternalCopy { Key = "EA12_AGM1_double_line_internal", SourceKey = "AGM1_double_line" },
            new InternalCopy { Key = "EA12_AGM_scanner1_double_line_internal", SourceKey = "AGM_scanner1_double_line" },
        };

        public static readonly Station[] Stations =
        {
            new Station
            {
                Name = "Internal Cannon",
                Default = "gun_20mm_internal_500",
                Options = new[] { "gun_20mm_internal_500" },
            },
            new Station
            {
                Name = "Internal Weapon Bays",
                Default = "AAM2_single_internal",
                Options = new[]
                {
                    "AAM1_single_internal", "AAM2_single_internal", "AAM3_double_internal", "AAM4_single_internal",
                    "AGM_heavy_internal",
                    "bomb_125_internalx2_line", "bomb_glide1_double_inernal", "bomb_250_internal", "bomb_250_glide_internal",
                    "EA12_AGM1_double_line_internal", "EA12_AGM_scanner1_double_line_internal",
                },
            },
            new Station
            {
                Name = "Wing Pylons",
                Default = "JammingPod1",
                Options = new[]
                {
                    "JammingPod1",
                    "AAM1_single", "AAM1_double", "AAM1_triple",
                    "AAM2_single", "AAM2_double", "AAM2_triple",
                    "AAM4_single", "AAM4_double",
                    "AGM_heavyx2", "AGM_heavy_triple",
                    "ARM1_single", "ARM1_double",
                    "AShM2_single", "AShM2_double", "AShM3_single",
                    "Rocket2_4Pod", "Rocket2_4Podx3",
                    "bomb_500_single", "bomb_500_double", "bomb_500_glide_single", "bomb_penetrator1_mount",
                    "bomb_250_triple", "bomb_250_glide_double", "bomb_glide1_triple",
                },
            },
            new Station
            {
                Name = "Wingtip Pylons",
                Default = "ARM1_single",
                Options = new[] { "ARM1_single", "AAM1_single", "AAM2_single", "AAM4_single" },
            },
        };

        public static IEnumerable<string> AllKeys()
        {
            foreach (Station s in Stations)
                foreach (string o in s.Options)
                    yield return o;
        }
    }
}
