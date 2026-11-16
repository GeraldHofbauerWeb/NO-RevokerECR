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
        public const string Description =
            "Electronic combat and reconnaissance variant of the FS-12 Revoker. A larger capacitor, " +
            "two optical sensors and a laser designator for two targets take room from the internal bays. " +
            "The wing pylons carry radar jamming pods by default.";

        public const int Rank = 4;
        public const float PriceMillions = 105f;
        public const int Flares = 86;
        public const int OpticalSensors = 2;
        // Base value; each JammingPod1 adds 100 kJ in game (two pods: 800 kJ).
        public const float CapacitorKJ = 600f;
        public const int LaserTargets = 2;
        public const float ExtraEmptyMassKg = 1020f;
    }
}
