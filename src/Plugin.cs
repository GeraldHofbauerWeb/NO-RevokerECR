using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using NORevokerECR.Game;
using UnityEngine.SceneManagement;

namespace NORevokerECR
{
    // Adds the EA-12 Revoker ECR: a separate aircraft built from the FS-12
    // Revoker at game start, with Seb's sensor, power, weight and weapon
    // changes. The FS-12 itself stays as it is. Every player in a multiplayer
    // session needs the same version of this mod.
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("NuclearOption.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static BepInEx.Logging.ManualLogSource Log;
        internal static ConfigEntry<bool> DebugDump;

        private static readonly HashSet<string> reported = new HashSet<string>();

        private void Awake()
        {
            Log = Logger;
            DebugDump = Config.Bind("Debug", "DebugDump", false,
                "At game start, write the sensors, radar, countermeasures, capacitor, laser designator, part masses and weapon options " +
                "of the FS-12, the Ifrit, the Darkreach and the finished EA-12 to the BepInEx log.");

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            VersionTagPatch.Apply(harmony);
            Registry.Apply(harmony);
            SceneManager.sceneLoaded += (scene, mode) => Registry.AddToAllHangars();
            // Everything this mod hooks into, so a log shows at a glance that
            // no game code beyond these is touched.
            Log.LogInfo("Hooks: " + string.Join(", ",
                harmony.GetPatchedMethods().Select(m => m.DeclaringType?.Name + "." + m.Name).OrderBy(n => n)));
        }

        // Logs each distinct failure once. Every hook catches its own exceptions
        // so that a problem in this mod never breaks the game's code path.
        internal static void Fail(string where, Exception e)
        {
            string key = where + ": " + e.GetType().Name + ": " + e.Message;
            if (reported.Add(key)) Log.LogError(key + "\n" + e.StackTrace);
        }
    }
}
