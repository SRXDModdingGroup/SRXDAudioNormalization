using HarmonyLib;
using Utility;

namespace SRXDAudioNormalization; 

public static class Patches {
    private static readonly float DEFAULT_INTERNAL_VOLUME = 0.298f;
    
    [HarmonyPatch(typeof(Track), nameof(Track.Update)), HarmonyPrefix]
    private static void Track_Update_Prefix(Track __instance) {
        var trackData = __instance.playStateFirst?.trackData;
        var mixerSettings = SoundEffectAssets.Instance.MixerSettings;
        float volume = DEFAULT_INTERNAL_VOLUME;

        if (trackData != null)
            volume *= AudioNormalization.GetVolumeMultiplierForTrackData(trackData);

        mixerSettings.mainMixer.SetFloat("MusicVolumeInternal", AudioUtils.LinearToDecibel(volume));
    }
}