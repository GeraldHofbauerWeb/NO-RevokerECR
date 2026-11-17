using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mirage;
using NORevokerECR.Core;
using NuclearOption.SavedMission;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NORevokerECR.Game
{
    internal sealed class BuildResult
    {
        public AircraftDefinition Aircraft;
        public readonly List<WeaponMount> Mounts = new List<WeaponMount>();
    }

    // Makes the EA-12 from the loaded FS-12: a copy of its prefab, definition
    // and parameters, changed as EcrSpec and WeaponTable say. The FS-12 itself
    // is never touched. Same pattern as the A19J mod (A-19 to A-19J).
    internal static class Builder
    {
        internal static readonly string[] OpticFields =
            { "visualRange", "magnification", "maxSpeed", "checkInterval", "alertCheckInterval", "shared" };
        internal static readonly string[] RadarFields =
            { "radarCone", "jamTolerance", "powerDraw", "visualRange", "magnification", "maxSpeed", "checkInterval", "alertCheckInterval" };

        private static GameObject storage;

        internal static BuildResult Build()
        {
            AircraftDefinition original = Aircraft(EcrSpec.OriginalKey)
                ?? throw new InvalidOperationException($"FS-12 ({EcrSpec.OriginalKey}) is not in the encyclopedia");
            if (Encyclopedia.Lookup.ContainsKey(EcrSpec.JsonKey))
                throw new InvalidOperationException($"the key {EcrSpec.JsonKey} is already taken (another mod?)");
            if (Resources.FindObjectsOfTypeAll<NetworkIdentity>().Any(n => n.PrefabHash == EcrSpec.PrefabHash))
                throw new InvalidOperationException($"prefab hash 0x{EcrSpec.PrefabHash:X8} is already taken (another mod?)");

            var result = new BuildResult();
            try
            {
                foreach (InternalCopy copy in WeaponTable.InternalCopies)
                {
                    WeaponMount m = InternalMount(copy);
                    if (m != null) result.Mounts.Add(m);
                }

                // An inactive parent keeps Awake from running on the template:
                // some Awake methods write into serialized state (countermeasure
                // stations), which every spawned copy would then inherit.
                storage = new GameObject("NORevokerECR_Storage");
                storage.SetActive(false);
                Object.DontDestroyOnLoad(storage);
                GameObject prefab = Object.Instantiate(original.unitPrefab, storage.transform, false);
                prefab.name = EcrSpec.JsonKey;

                AircraftDefinition def = Object.Instantiate(original);
                def.name = EcrSpec.JsonKey;
                def.jsonKey = EcrSpec.JsonKey;
                def.unitName = EcrSpec.FullName;
                def.code = EcrSpec.ShortName;
                def.description = EcrSpec.Description;
                def.value = EcrSpec.PriceMillions;
                def.unitPrefab = prefab;
                // A ScriptableObject reference: without its own copy, changing
                // rank or loadouts would change the FS-12 too.
                def.aircraftParameters = Object.Instantiate(original.aircraftParameters);
                def.aircraftParameters.name = EcrSpec.JsonKey + "_Parameters";
                def.aircraftParameters.aircraftName = EcrSpec.ShortName;
                def.aircraftParameters.rankRequired = EcrSpec.Rank;

                Aircraft aircraft = prefab.GetComponent<Aircraft>();
                ((Unit)aircraft).definition = def;
                NetworkIdentity identity = prefab.GetComponent<NetworkIdentity>();
                identity.PrefabHash = EcrSpec.PrefabHash;

                Step("flares", () => Flares(prefab));
                Step("radar", () => RadarLikeIfrit(prefab));
                Step("optical sensor", () => OpticsLikeDarkreach(prefab, aircraft));
                Step("capacitor", () => Capacitor(prefab));
                Step("laser designator", () => Laser(prefab));
                Step("empty weight", () => Mass(prefab));
                Step("weapons", () => Weapons(prefab, def, result.Mounts));

                identity.ClearNetworkBehaviourCache();
                def.CacheMass();
                Check(original, def);
                result.Aircraft = def;
                return result;
            }
            catch
            {
                if (storage != null) Object.Destroy(storage);
                foreach (WeaponMount m in result.Mounts) Object.Destroy(m);
                throw;
            }
        }

        // One change failing leaves that value as on the FS-12 and is logged;
        // the rest of the aircraft is still built. Every machine does the same,
        // so a failure cannot make peers disagree.
        private static void Step(string what, Action change)
        {
            try
            {
                change();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"EA-12 {what}: {e.Message} (left as on the FS-12)");
            }
        }

        private static void Flares(GameObject prefab)
        {
            FlareEjector[] ejectors = prefab.GetComponentsInChildren<FlareEjector>(true);
            if (ejectors.Length == 0) throw new InvalidOperationException("the FS-12 has no flare ejector");
            int[] counts = FlareSplit.Split(EcrSpec.Flares, ejectors.Select(e => e.ammo).ToArray());
            for (int i = 0; i < ejectors.Length; i++) ejectors[i].ammo = counts[i];
            Plugin.Log.LogInfo($"EA-12 flares: {string.Join(" + ", counts)} = {counts.Sum()}");
        }

        private static void RadarLikeIfrit(GameObject prefab)
        {
            Radar donor = Prefab(EcrSpec.RadarDonorKey).GetComponentInChildren<Radar>(true)
                ?? throw new InvalidOperationException("the Ifrit has no radar");
            Radar[] radars = prefab.GetComponentsInChildren<Radar>(true);
            if (radars.Length == 0) throw new InvalidOperationException("the FS-12 has no radar");
            if (radars.Length > 1) Plugin.Log.LogWarning($"EA-12 radar: the FS-12 has {radars.Length} radars, changing the first");
            radars[0].RadarParameters = donor.RadarParameters;
            Reflect.Copy(donor, radars[0], RadarFields);
            Plugin.Log.LogInfo($"EA-12 radar: maxRange {donor.RadarParameters.maxRange} m, clutter {donor.RadarParameters.clutterFactor}");
        }

        // The optical sensor is a plain TargetDetector (Radar is the only
        // subclass). The FS-12 has one (10 km, magnification 0.7); it gets the
        // Darkreach's values (magnification 2, speed limit, timing) and the
        // EA-12's own search range. "x2" in Seb's list is that
        // magnification, not a second sensor.
        private static void OpticsLikeDarkreach(GameObject prefab, Aircraft aircraft)
        {
            TargetDetector donor = Optical(Prefab(EcrSpec.OpticsDonorKey)).FirstOrDefault()
                ?? throw new InvalidOperationException("the Darkreach has no optical sensor");
            TargetDetector ours = Optical(prefab).FirstOrDefault();
            if (ours == null)
            {
                // Not the case in 0.34.6; kept so a game update cannot leave the EA-12 blind.
                UnitPart host = RootPart(prefab);
                ours = host.gameObject.AddComponent<TargetDetector>();
                Reflect.CopySerialized(donor, ours);
                Reflect.Set(ours, "attachedUnit", aircraft);
                Reflect.Set(ours, "part", host);
                Reflect.Set(ours, "scanner", host.transform);
                Reflect.Set(ours, "rotators", Array.CreateInstance(AccessTools.Inner(typeof(TargetDetector), "Rotator"), 0));
            }
            Reflect.Copy(donor, ours, OpticFields);
            Reflect.Set(ours, "visualRange", EcrSpec.OpticalRangeM);
            Plugin.Log.LogInfo("EA-12 optical sensor like the Darkreach's: " + Reflect.Describe(ours, "visualRange", "magnification"));
        }

        // The HUD value is maxCharge plus what built-in self-protection jammers
        // add in their Awake (RadarJammer.capacitance, 100 on the FS-12).
        // Jamming pods on the pylons do not change it.
        private static void Capacitor(GameObject prefab)
        {
            PowerSupply supply = prefab.GetComponentInChildren<PowerSupply>(true)
                ?? throw new InvalidOperationException("the FS-12 has no capacitor (PowerSupply)");
            float builtIn = prefab.GetComponentsInChildren<RadarJammer>(true).Sum(j => Reflect.Get<float>(j, "capacitance"));
            Reflect.Set(supply, "maxCharge", EcrSpec.CapacitorKJ - builtIn);
            Plugin.Log.LogInfo($"EA-12 capacitor: {EcrSpec.CapacitorKJ - builtIn} kJ + {builtIn} kJ built-in jammer = {EcrSpec.CapacitorKJ} kJ");
        }

        private static void Laser(GameObject prefab)
        {
            LaserDesignator laser = prefab.GetComponentInChildren<LaserDesignator>(true)
                ?? throw new InvalidOperationException("the FS-12 has no laser designator");
            Reflect.Set(laser, "maxTargets", EcrSpec.LaserTargets);
        }

        // Flight physics, the encyclopedia's empty weight and the selection
        // menu's weight all add up UnitPart.mass. The extra weight goes on the
        // main part, close to the centre of gravity.
        private static void Mass(GameObject prefab)
        {
            UnitPart part = RootPart(prefab);
            part.mass += EcrSpec.ExtraEmptyMassKg;
            Plugin.Log.LogInfo($"EA-12 empty weight: +{EcrSpec.ExtraEmptyMassKg} kg on {Inspector.PathOf(prefab, part.transform)}");
        }

        private static void Weapons(GameObject prefab, AircraftDefinition def, List<WeaponMount> created)
        {
            WeaponManager manager = prefab.GetComponentInChildren<WeaponManager>(true)
                ?? throw new InvalidOperationException("the FS-12 has no weapon manager");
            HardpointSet[] sets = manager.hardpointSets;
            var own = created.ToDictionary(m => m.jsonKey);
            var defaults = new WeaponMount[sets.Length];

            for (int s = 0; s < WeaponTable.Stations.Length; s++)
            {
                Station station = WeaponTable.Stations[s];
                int index = Array.FindIndex(sets, h => h.name == station.Name);
                if (index < 0)
                {
                    if (s >= sets.Length) continue;
                    Plugin.Log.LogWarning($"EA-12 weapons: no set named \"{station.Name}\", using set {s} \"{sets[s].name}\"");
                    index = s;
                }
                HardpointSet set = sets[index];
                var options = new List<WeaponMount>();
                // The game offers "empty" as a null first entry; keep it.
                if (set.weaponOptions.Count > 0 && set.weaponOptions[0] == null) options.Add(null);
                foreach (string key in station.Options)
                {
                    WeaponMount mount = Resolve(key, own);
                    if (mount != null) options.Add(mount);
                }
                set.weaponOptions = options;
                defaults[index] = Resolve(station.Default, own);
            }

            // loadouts[1] is what the hangar preselects until the player picks
            // something for this aircraft (LoadoutSelector.LoadDefaults).
            AircraftParameters p = def.aircraftParameters;
            p.loadouts = new List<Loadout>
            {
                new Loadout { weapons = Enumerable.Repeat<WeaponMount>(null, sets.Length).ToList() },
                new Loadout { weapons = defaults.ToList() },
            };
            // AI loadouts: the AI does not fly the EA-12 (no faction supply).
            p.StandardLoadouts = new StandardLoadout[0];
        }

        private static WeaponMount Resolve(string key, Dictionary<string, WeaponMount> own)
        {
            if (own.TryGetValue(key, out WeaponMount m)) return m;
            if (Encyclopedia.WeaponLookup.TryGetValue(key, out m)) return m;
            Plugin.Log.LogWarning($"EA-12 weapons: weapon mount {key} not found, left out");
            return null;
        }

        // A copy of a mount for carriage in the bays: no radar signature and no
        // drag, loaded or empty (Hardpoint and MountedMissile read only these
        // four fields), everything else as the original.
        private static WeaponMount InternalMount(InternalCopy copy)
        {
            if (!Encyclopedia.WeaponLookup.TryGetValue(copy.SourceKey, out WeaponMount source))
            {
                Plugin.Log.LogWarning($"EA-12 weapons: {copy.SourceKey} not found, {copy.Key} is not created");
                return null;
            }
            if (Encyclopedia.WeaponLookup.ContainsKey(copy.Key))
            {
                Plugin.Log.LogWarning($"EA-12 weapons: {copy.Key} already exists (another mod?), not created");
                return null;
            }
            WeaponMount mount = Object.Instantiate(source);
            mount.name = copy.Key;
            mount.jsonKey = copy.Key;
            mount.RCS = 0f;
            mount.emptyRCS = 0f;
            mount.drag = 0f;
            mount.emptyDrag = 0f;
            return mount;
        }

        private static void Check(AircraftDefinition original, AircraftDefinition def)
        {
            if (original.jsonKey != EcrSpec.OriginalKey || original.unitPrefab == def.unitPrefab
                || original.aircraftParameters == def.aircraftParameters)
                throw new InvalidOperationException("the FS-12 was changed while building the EA-12");
        }

        private static TargetDetector[] Optical(GameObject root)
        {
            return root.GetComponentsInChildren<TargetDetector>(true).Where(d => d.GetType() == typeof(TargetDetector)).ToArray();
        }

        private static UnitPart RootPart(GameObject prefab)
        {
            return prefab.GetComponent<UnitPart>()
                ?? prefab.GetComponentsInChildren<UnitPart>(true).OrderByDescending(p => p.mass).FirstOrDefault()
                ?? throw new InvalidOperationException("the FS-12 has no parts");
        }

        internal static AircraftDefinition Aircraft(string key)
        {
            return Encyclopedia.Lookup != null && Encyclopedia.Lookup.TryGetValue(key, out UnitDefinition u) ? u as AircraftDefinition : null;
        }

        private static GameObject Prefab(string key)
        {
            return Aircraft(key)?.unitPrefab ?? throw new InvalidOperationException($"{key} is not in the encyclopedia");
        }
    }
}
