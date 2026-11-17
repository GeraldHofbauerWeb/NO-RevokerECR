using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mirage;
using NORevokerECR.Core;
using UnityEngine;

namespace NORevokerECR.Game
{
    // Shows at every start that building the EA-12 leaves the game's own
    // aircraft and weapons alone. Everything the EA-12 is copied from or reads
    // values from (FS-12, Ifrit, Darkreach, the two source weapon mounts) is
    // written down before the build and compared after it.
    internal static class VanillaCheck
    {
        private static readonly string[] AircraftKeys = { EcrSpec.OriginalKey, EcrSpec.RadarDonorKey, EcrSpec.OpticsDonorKey };

        internal static string Take()
        {
            try
            {
                var sb = new StringBuilder();
                foreach (string key in AircraftKeys) Aircraft(sb, key);
                foreach (InternalCopy c in WeaponTable.InternalCopies)
                {
                    sb.AppendLine(Encyclopedia.WeaponLookup.TryGetValue(c.SourceKey, out WeaponMount m) && m != null
                        ? $"{c.SourceKey}: {m.name} RCS={F(m.RCS)} emptyRCS={F(m.emptyRCS)} drag={F(m.drag)} emptyDrag={F(m.emptyDrag)}"
                        : $"{c.SourceKey}: missing");
                }
                return sb.ToString();
            }
            catch (Exception e)
            {
                Plugin.Fail("vanilla check", e);
                return null;
            }
        }

        internal static void Verify(string before)
        {
            if (before == null) return;
            string after = Take();
            if (after == null) return;
            string[] was = Lines(before), now = Lines(after);
            if (before == after)
            {
                Plugin.Log.LogInfo($"Vanilla check passed: FS-12, KR-67, SFB-81 and the source weapon mounts are unchanged " +
                    $"after building the EA-12 ({was.Length} entries compared)");
                return;
            }
            foreach (string line in was.Except(now)) Plugin.Log.LogError("Vanilla check, before: " + line);
            foreach (string line in now.Except(was)) Plugin.Log.LogError("Vanilla check, after:  " + line);
            Plugin.Log.LogError("Vanilla check FAILED: building the EA-12 changed the game's own aircraft or weapons. Please report this.");
        }

        private static void Aircraft(StringBuilder sb, string key)
        {
            AircraftDefinition def = Builder.Aircraft(key);
            if (def == null)
            {
                sb.AppendLine(key + ": missing");
                return;
            }
            string k = key + " ";
            sb.AppendLine(k + $"definition {def.name} \"{def.unitName}\" code={def.code} value={F(def.value)} mass={F(def.mass)} " +
                $"index={((INetworkDefinition)def).LookupIndex}");
            sb.AppendLine(k + "description " + def.description);

            AircraftParameters p = def.aircraftParameters;
            if (p != null)
            {
                sb.AppendLine(k + $"parameters {p.name} \"{p.aircraftName}\" rank={p.rankRequired} standardLoadouts={p.StandardLoadouts?.Length}");
                if (p.loadouts != null)
                {
                    for (int i = 0; i < p.loadouts.Count; i++)
                        sb.AppendLine(k + $"loadout {i}: {Keys(p.loadouts[i].weapons)}");
                }
            }

            GameObject root = def.unitPrefab;
            if (root == null)
            {
                sb.AppendLine(k + "prefab missing");
                return;
            }
            NetworkIdentity identity = root.GetComponent<NetworkIdentity>();
            Aircraft aircraft = root.GetComponent<Aircraft>();
            sb.AppendLine(k + $"prefab {root.name} hash=0x{(identity != null ? identity.PrefabHash : 0):X8} " +
                $"definition={(aircraft != null && ((Unit)aircraft).definition != null ? ((Unit)aircraft).definition.jsonKey : "none")}");

            foreach (Countermeasure c in root.GetComponentsInChildren<Countermeasure>(true))
                sb.AppendLine(k + $"{c.GetType().Name} @ {Inspector.PathOf(root, c.transform)}: ammo={c.ammo}" +
                    (c is RadarJammer ? ", " + Reflect.Describe(c, "capacitance") : ""));
            foreach (TargetDetector d in root.GetComponentsInChildren<TargetDetector>(true))
            {
                string radar = d is Radar r
                    ? $", maxRange={F(r.RadarParameters.maxRange)} maxSignal={F(r.RadarParameters.maxSignal)} minSignal={F(r.RadarParameters.minSignal)} " +
                      $"clutter={F(r.RadarParameters.clutterFactor)} doppler={F(r.RadarParameters.dopplerFactor)}, " + Reflect.Describe(r, Builder.RadarFields)
                    : "";
                sb.AppendLine(k + $"{d.GetType().Name} @ {Inspector.PathOf(root, d.transform)}: {Reflect.Describe(d, Builder.OpticFields)}{radar}");
            }
            foreach (PowerSupply s in root.GetComponentsInChildren<PowerSupply>(true))
                sb.AppendLine(k + $"PowerSupply @ {Inspector.PathOf(root, s.transform)}: {Reflect.Describe(s, "maxCharge")}");
            foreach (LaserDesignator l in root.GetComponentsInChildren<LaserDesignator>(true))
                sb.AppendLine(k + $"LaserDesignator @ {Inspector.PathOf(root, l.transform)}: {Reflect.Describe(l, "maxTargets", "range")}");
            foreach (UnitPart part in root.GetComponentsInChildren<UnitPart>(true))
                sb.AppendLine(k + $"part {Inspector.PathOf(root, part.transform)}: mass={F(part.mass)}");

            WeaponManager wm = root.GetComponentInChildren<WeaponManager>(true);
            if (wm != null && wm.hardpointSets != null)
            {
                foreach (HardpointSet set in wm.hardpointSets)
                    sb.AppendLine(k + $"set \"{set.name}\" ({set.hardpoints?.Count} hardpoints): {Keys(set.weaponOptions)}");
            }
        }

        private static string Keys(IEnumerable<WeaponMount> mounts)
        {
            return mounts == null ? "none" : string.Join(", ", mounts.Select(m => m == null ? "-" : m.jsonKey));
        }

        private static string F(float value) => Reflect.Format(value);

        private static string[] Lines(string text) =>
            text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
