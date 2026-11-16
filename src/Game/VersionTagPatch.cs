using System;
using HarmonyLib;
using NORevokerECR.Core;
using UnityEngine;

namespace NORevokerECR.Game
{
    // Appends "_NO-RevokerECR-v<version>" to Application.version. Normal
    // priority, so this runs before Blueprinter's postfix (Priority.Last),
    // which appends its own tag and shortens strings over 100 characters
    // with our tag already included.
    internal static class VersionTagPatch
    {
        private static bool logged;

        internal static void Apply(Harmony harmony)
        {
            var getter = AccessTools.PropertyGetter(typeof(Application), nameof(Application.version));
            if (getter == null)
            {
                Plugin.Log.LogWarning("Application.version getter not found. Lobbies cannot tell players with and without the EA-12 apart.");
                return;
            }
            harmony.Patch(getter, postfix: new HarmonyMethod(typeof(VersionTagPatch), nameof(Postfix)));
        }

        private static void Postfix(ref string __result)
        {
            try
            {
                __result = VersionTag.Append(__result, MyPluginInfo.PLUGIN_VERSION);
                if (!logged)
                {
                    logged = true;
                    Plugin.Log.LogInfo($"Game version with the EA-12 tag: {__result} (lobbies show only players with the same mods)");
                }
            }
            catch (Exception e)
            {
                Plugin.Fail("version tag", e);
            }
        }
    }
}
