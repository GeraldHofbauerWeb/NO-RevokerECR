using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using NORevokerECR.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NORevokerECR.Game
{
    // Builds the EA-12 when the game has loaded its encyclopedia and makes it
    // known to the game: encyclopedia lists and network index, hangars that
    // offer the FS-12, and the FS-12's mission restrictions.
    internal static class Registry
    {
        internal static AircraftDefinition Ecr { get; private set; }

        private static AccessTools.FieldRef<Hangar, AircraftDefinition[]> hangarAircraft;

        internal static void Apply(Harmony harmony)
        {
            Patch(harmony, AccessTools.Method(typeof(Encyclopedia), "AfterLoad", Type.EmptyTypes), nameof(AfterLoadPostfix),
                "Encyclopedia.AfterLoad not found (game update?). The EA-12 is not built.");
            Patch(harmony, AccessTools.Method(typeof(Hangar), "Awake"), nameof(HangarAwakePostfix),
                "Hangar.Awake not found (game update?). The EA-12 may be missing from hangars.");
            Patch(harmony, AccessTools.Method(typeof(FactionHQ), "OnMissionLoad"), nameof(MissionLoadPostfix),
                "FactionHQ.OnMissionLoad not found (game update?). FS-12 restrictions do not carry over to the EA-12.");
            try
            {
                hangarAircraft = AccessTools.FieldRefAccess<Hangar, AircraftDefinition[]>("availableAircraft");
            }
            catch (Exception e)
            {
                Plugin.Fail("hangar field", e);
            }
        }

        private static void Patch(Harmony harmony, System.Reflection.MethodInfo target, string postfix, string missing)
        {
            if (target == null)
            {
                Plugin.Log.LogWarning(missing);
                return;
            }
            harmony.Patch(target, postfix: new HarmonyMethod(typeof(Registry), postfix));
        }

        // Runs once, right after the game numbered its own definitions. The
        // EA-12 and its two mounts go to the end of the index list, before any
        // Blueprinter content (Blueprinter appends frames later, after loading
        // its bundles), so the index is the same on every machine with the
        // same mods.
        private static void AfterLoadPostfix(Encyclopedia __instance)
        {
            if (Ecr != null) return;
            try
            {
                if (Plugin.DebugDump.Value)
                {
                    Inspector.Dump("FS-12", Builder.Aircraft(EcrSpec.OriginalKey));
                    Inspector.Dump("radar donor KR-67", Builder.Aircraft(EcrSpec.RadarDonorKey));
                    Inspector.Dump("optics donor SFB-81", Builder.Aircraft(EcrSpec.OpticsDonorKey));
                }

                string vanilla = VanillaCheck.Take();
                BuildResult built = Builder.Build();
                foreach (WeaponMount mount in built.Mounts)
                {
                    __instance.weaponMounts.Add(mount);
                    Encyclopedia.WeaponLookup[mount.jsonKey] = mount;
                    Index(__instance, mount);
                }
                __instance.aircraft.Add(built.Aircraft);
                Encyclopedia.Lookup[built.Aircraft.jsonKey] = built.Aircraft;
                Index(__instance, built.Aircraft);
                Reflect.Set(__instance, "aircraftAndVehiclesCache", null);
                Ecr = built.Aircraft;

                if (Plugin.DebugDump.Value) Inspector.Dump("EA-12", Ecr);
                Plugin.Log.LogInfo($"{EcrSpec.FullName} ready: index {((INetworkDefinition)Ecr).LookupIndex} of {__instance.IndexLookup.Count}, " +
                    $"prefab hash 0x{EcrSpec.PrefabHash:X8}, {built.Mounts.Count} internal mounts, empty weight {Ecr.mass:0} kg");
                AddToAllHangars();
                VanillaCheck.Verify(vanilla);
            }
            catch (Exception e)
            {
                Plugin.Fail("build", e);
            }
        }

        private static void Index(Encyclopedia encyclopedia, INetworkDefinition definition)
        {
            definition.LookupIndex = encyclopedia.IndexLookup.Count;
            encyclopedia.IndexLookup.Add(definition);
        }

        private static void HangarAwakePostfix(Hangar __instance)
        {
            AddToHangar(__instance);
        }

        // Hangars that were already loaded when the EA-12 was built.
        internal static void AddToAllHangars()
        {
            if (Ecr == null) return;
            foreach (Hangar hangar in Object.FindObjectsOfType<Hangar>(true)) AddToHangar(hangar);
        }

        private static void AddToHangar(Hangar hangar)
        {
            if (Ecr == null || hangarAircraft == null || hangar == null) return;
            try
            {
                ref AircraftDefinition[] available = ref hangarAircraft(hangar);
                if (available == null || Array.IndexOf(available, Ecr) >= 0) return;
                if (!available.Any(a => a != null && a.jsonKey == EcrSpec.OriginalKey)) return;
                available = available.Append(Ecr).ToArray();
            }
            catch (Exception e)
            {
                Plugin.Fail("hangar", e);
            }
        }

        // A mission that forbids the FS-12 or a weapon also forbids the EA-12
        // and its internal copy of that weapon.
        private static void MissionLoadPostfix(FactionHQ __instance)
        {
            if (Ecr == null) return;
            try
            {
                AddIf(__instance.restrictedAircraft, EcrSpec.OriginalKey, EcrSpec.JsonKey);
                foreach (InternalCopy copy in WeaponTable.InternalCopies)
                    AddIf(__instance.restrictedWeapons, copy.SourceKey, copy.Key);
            }
            catch (Exception e)
            {
                Plugin.Fail("restrictions", e);
            }
        }

        private static void AddIf(List<string> list, string ifPresent, string add)
        {
            if (list != null && list.Contains(ifPresent) && !list.Contains(add)) list.Add(add);
        }
    }
}
