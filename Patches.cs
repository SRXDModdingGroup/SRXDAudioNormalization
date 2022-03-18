using HarmonyLib;
using Utility;

namespace SRXDAudioNormalization; 

public static class Patches {
    [HarmonyPatch(typeof(Track), nameof(Track.Update)), HarmonyPrefix]
    private static void Track_Update_Prefix(Track __instance) => AudioNormalization.UpdateGain(__instance);
}