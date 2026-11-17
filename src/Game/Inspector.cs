using System.Linq;
using UnityEngine;

namespace NORevokerECR.Game
{
    // With DebugDump on, writes what the EA-12 is built from and what came out:
    // sensors, radar, countermeasures, power, laser, part masses and weapons.
    // Values that live in prefab data cannot be read from the game's code, so
    // this is how the build is checked against Seb's numbers.
    internal static class Inspector
    {
        private static readonly string[] DetectorFields =
            { "visualRange", "magnification", "maxSpeed", "checkInterval", "alertCheckInterval", "shared" };

        internal static void Dump(string label, AircraftDefinition def)
        {
            if (def == null || def.unitPrefab == null)
            {
                Plugin.Log.LogInfo($"[dump] {label}: not found");
                return;
            }
            GameObject root = def.unitPrefab;
            AircraftParameters p = def.aircraftParameters;
            Plugin.Log.LogInfo($"[dump] ==== {label}: {def.jsonKey} \"{def.unitName}\" code={def.code} value={def.value} " +
                $"rank={p?.rankRequired} cachedMass={def.mass} {Reflect.Describe(def.aircraftInfo, "maxWeight", "emptyWeight")}");

            foreach (TargetDetector d in root.GetComponentsInChildren<TargetDetector>(true))
            {
                string extra = d is Radar r
                    ? $" radar: maxRange={r.RadarParameters.maxRange} maxSignal={r.RadarParameters.maxSignal} minSignal={r.RadarParameters.minSignal} " +
                      $"clutter={r.RadarParameters.clutterFactor} doppler={r.RadarParameters.dopplerFactor}, " +
                      Reflect.Describe(r, "radarCone", "jamTolerance", "powerDraw")
                    : "";
                Plugin.Log.LogInfo($"[dump]   {d.GetType().Name} @ {PathOf(root, d.transform)}: {Reflect.Describe(d, DetectorFields)}" +
                    $", part={Reflect.Describe(d, "part")}{extra}");
            }
            foreach (Countermeasure c in root.GetComponentsInChildren<Countermeasure>(true))
                Plugin.Log.LogInfo($"[dump]   {c.GetType().Name} @ {PathOf(root, c.transform)}: \"{c.displayName}\" ammo={c.ammo}" +
                    (c is RadarJammer ? ", " + Reflect.Describe(c, "capacitance") : ""));
            foreach (PowerSupply s in root.GetComponentsInChildren<PowerSupply>(true))
                Plugin.Log.LogInfo($"[dump]   PowerSupply @ {PathOf(root, s.transform)}: {Reflect.Describe(s, "maxCharge", "maxPower", "chargePerRPM", "powerSources")}");
            foreach (LaserDesignator l in root.GetComponentsInChildren<LaserDesignator>(true))
                Plugin.Log.LogInfo($"[dump]   LaserDesignator @ {PathOf(root, l.transform)}: {Reflect.Describe(l, "maxTargets", "range", "unitPart")}");

            UnitPart[] parts = root.GetComponentsInChildren<UnitPart>(true);
            Plugin.Log.LogInfo($"[dump]   parts: {parts.Length}, sum {parts.Sum(x => x.mass)} kg; " +
                string.Join("; ", parts.OrderByDescending(x => x.mass).Take(8).Select(x => $"{PathOf(root, x.transform)}={x.mass}")));

            WeaponManager wm = root.GetComponentInChildren<WeaponManager>(true);
            if (wm != null && wm.hardpointSets != null)
            {
                for (int i = 0; i < wm.hardpointSets.Length; i++)
                {
                    HardpointSet h = wm.hardpointSets[i];
                    string excludes = h.precludingHardpointSets != null && h.precludingHardpointSets.Count > 0
                        ? $", excludes sets [{string.Join(", ", h.precludingHardpointSets)}]"
                        : "";
                    Plugin.Log.LogInfo($"[dump]   set {i} \"{h.name}\" ({h.hardpoints?.Count} hardpoints{excludes}): " +
                        string.Join(", ", h.weaponOptions.Select(m => m == null ? "-" : m.jsonKey)));
                }
            }
            if (p != null)
            {
                Plugin.Log.LogInfo($"[dump]   loadouts {p.loadouts?.Count}: " + string.Join(" | ",
                    (p.loadouts ?? new System.Collections.Generic.List<NuclearOption.SavedMission.Loadout>())
                    .Select(l => string.Join(", ", l.weapons.Select(m => m == null ? "-" : m.jsonKey)))) +
                    $"; standardLoadouts {p.StandardLoadouts?.Length}");
            }
        }

        // A weapon mount's prefab can bring its own systems, e.g. the Medusa's
        // radome, whose Radar replaces the aircraft's radar when mounted.
        internal static void DumpMount(string key)
        {
            if (!Encyclopedia.WeaponLookup.TryGetValue(key, out WeaponMount mount) || mount == null || mount.prefab == null)
            {
                Plugin.Log.LogInfo($"[dump] mount {key}: not found");
                return;
            }
            GameObject root = mount.prefab;
            Plugin.Log.LogInfo($"[dump] ==== mount {key} \"{mount.name}\": RCS={mount.RCS} drag={mount.drag} prefab={root.name}");
            foreach (TargetDetector d in root.GetComponentsInChildren<TargetDetector>(true))
            {
                string extra = d is Radar r
                    ? $" radar: maxRange={r.RadarParameters.maxRange} maxSignal={r.RadarParameters.maxSignal} minSignal={r.RadarParameters.minSignal} " +
                      $"clutter={r.RadarParameters.clutterFactor} doppler={r.RadarParameters.dopplerFactor}, " +
                      Reflect.Describe(r, "radarCone", "jamTolerance", "powerDraw")
                    : "";
                Plugin.Log.LogInfo($"[dump]   {d.GetType().Name} @ {PathOf(root, d.transform)}: {Reflect.Describe(d, DetectorFields)}{extra}");
            }
            foreach (MonoBehaviour b in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!(b is TargetDetector))
                    Plugin.Log.LogInfo($"[dump]   component {b.GetType().Name} @ {PathOf(root, b.transform)}");
            }
        }

        internal static string PathOf(GameObject root, Transform t)
        {
            if (t == null) return "?";
            if (t == root.transform) return "<root>";
            string path = t.name;
            for (Transform p = t.parent; p != null && p != root.transform; p = p.parent)
                path = p.name + "/" + path;
            return path;
        }
    }
}
