namespace NORevokerECR.Core
{
    // The game compares Application.version for multiplayer: the host puts it
    // into its lobby, the lobby list only shows lobbies with the same version
    // and joining a different one fails with "Incorrect version". Adding the
    // mod's tag to it keeps players with and without the EA-12 apart, since
    // the EA-12 shifts the network numbering of definitions.
    public static class VersionTag
    {
        public const string Marker = "_NO-RevokerECR-v";

        public static string Append(string gameVersion, string modVersion)
        {
            string current = gameVersion ?? "";
            return current.Contains(Marker) ? current : current + Marker + modVersion;
        }
    }
}
