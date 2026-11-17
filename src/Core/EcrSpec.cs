namespace NORevokerECR.Core
{
    // The EA-12 Revoker ECR as Seb specified it on 2026-11-16. Everything not
    // listed here stays as on the vanilla FS-12 Revoker.
    public static class EcrSpec
    {
        // Game jsonKeys (NO-Companion export, game 0.34.6).
        public const string OriginalKey = "Fighter1";      // FS-12 Revoker
        public const string RadarDonorKey = "Multirole1";  // KR-67 Ifrit
        public const string OpticsDonorKey = "Darkreach";  // SFB-81 Darkreach

        public const string JsonKey = "EA12";
        // Mirage prefab hash of the cloned prefab: ASCII "EA12". It must differ
        // from the FS-12's, or clients would spawn an FS-12 for it.
        public const int PrefabHash = 0x45413132;

        public const string FullName = "EA-12 Revoker ECR";
        public const string ShortName = "EA-12";
        // Seb's text (2026-11-17).
        public const string Description =
            "Electronic combat and reconnaissance variant of the FS-12 Revoker. Upgrades include improved radar, " +
            "advanced optical sensors for ground target detection and power capacity for two jamming pods at the cost " +
            "of increased weight and reduced internal weapon bay volume. Not nuclear-capable.";

        public const int Rank = 4;
        public const float PriceMillions = 105f;
        public const int Flares = 86;
        // Capacitor as the HUD shows it without pods. It includes the 100 kJ the
        // FS-12's built-in self-protection jammer adds at spawn (FS-12: 400 + 100).
        public const float CapacitorKJ = 600f;
        // Added per jamming pod on a pylon. Vanilla pods add nothing (the
        // JammingPod weapon has no capacitance), this is the EA-12's own rule.
        public const float PodCapacitorKJ = 100f;
        public const int LaserTargets = 2;
        // Empty weight over the FS-12 (8680 + 2020 = 10 700 kg); max weight stays.
        public const float ExtraEmptyMassKg = 2020f;
        // Optical sensor search range; everything else as the Darkreach's.
        // The game searches in whole battlefield grid squares (4096 m on the
        // tested map): 16 km gives 4 squares, 10 and 12 km both give 3.
        public const float OpticalRangeM = 16000f;
    }
}
