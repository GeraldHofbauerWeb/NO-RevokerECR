using System;
using HarmonyLib;
using NORevokerECR.Core;
using UnityEngine;

namespace NORevokerECR.Game
{
    // The EA-12's capacitor grows by 100 kJ for every jamming pod on its
    // pylons (600 kJ without pods, 800 with two). In the game a pod is a
    // JammingPod weapon that only draws power and never adds capacitance, so
    // this is the EA-12's own rule; other aircraft are left alone.
    internal static class JammerPods
    {
        internal static void Apply(Harmony harmony)
        {
            var attach = AccessTools.Method(typeof(JammingPod), nameof(JammingPod.AttachToUnit), new[] { typeof(Unit) });
            if (attach == null)
            {
                Plugin.Log.LogWarning("JammingPod.AttachToUnit not found (game update?). Jamming pods do not add capacitor charge.");
                return;
            }
            harmony.Patch(attach, postfix: new HarmonyMethod(typeof(JammerPods), nameof(AttachPostfix)));
        }

        private static void AttachPostfix(JammingPod __instance, Unit unit)
        {
            try
            {
                if (!(unit is Aircraft aircraft) || aircraft.definition == null || aircraft.definition.jsonKey != EcrSpec.JsonKey) return;
                PowerSupply supply = aircraft.GetPowerSupply();
                if (supply == null || __instance.GetComponent<PodCapacity>() != null) return;
                supply.ModifyCapacitance(EcrSpec.PodCapacitorKJ);
                __instance.gameObject.AddComponent<PodCapacity>().Bind(supply, EcrSpec.PodCapacitorKJ);
            }
            catch (Exception e)
            {
                Plugin.Fail("jamming pod", e);
            }
        }
    }

    // Lives on the pod and takes its share back when the pod is removed
    // (loadout change, Hardpoint.RemoveMount, aircraft destroyed).
    internal sealed class PodCapacity : MonoBehaviour
    {
        private PowerSupply supply;
        private float amount;

        internal void Bind(PowerSupply powerSupply, float kJ)
        {
            supply = powerSupply;
            amount = kJ;
        }

        private void OnDestroy()
        {
            if (supply != null) supply.ModifyCapacitance(-amount);
        }
    }
}
