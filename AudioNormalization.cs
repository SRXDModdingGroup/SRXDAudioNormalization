using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Utility;

namespace SRXDAudioNormalization; 

public static class AudioNormalization {
    private static float preferredLoudnessLinear;
    private static float maxGainLinear;
    private static Dictionary<string, float> peaksForTrackData = new();
    
    public static void Init() {
        Plugin.PreferredLoudness.BindAndInvoke(value => preferredLoudnessLinear = AudioUtils.DecibelToLinear(value));
        Plugin.MaxGain.BindAndInvoke(value => maxGainLinear = AudioUtils.DecibelToLinear(value));
    }

    public static void UpdateGain(Track track) {
        float volume = PlayerSettingsData.Instance.MusicVolume.Value / 100f;

        if (!track.IsInEditMode) {
            var trackData = track.playStateFirst?.trackData;

            if (trackData != null)
                volume *= GetVolumeMultiplierForTrackData(trackData);
        }

        var mixerSettings = SoundEffectAssets.Instance.MixerSettings;
        
        mixerSettings.mainMixer.SetFloat(mixerSettings.musicVolume, AudioUtils.LinearToDecibel(volume));
    }

    private static float GetVolumeMultiplierForTrackData(PlayableTrackData trackData) {
        string hash = trackData.TrackInfoRef.UniqueName;

        lock (peaksForTrackData) {
            if (peaksForTrackData.TryGetValue(hash, out float peak)) {
                if (peak < 0f)
                    return 1f;
                
                return Mathf.Min(preferredLoudnessLinear / peak, maxGainLinear);
            }
        }
        
        if (trackData.RawAudioDataHash == 0 && !trackData.LoadAllRawClipInfo())
            return 1f;
        
        AudioData clip = default;
        bool found = false;
        
        foreach (var subData in trackData.TrackDataList) {
            foreach (var assetReference in subData.clipInfoAssetReferences) {
                if (!assetReference.TryGetLoadedAsset(out var result)
                    || !result.clipAssetReference.TryGetHandle<AudioClip, AudioClipAssetHandle>(out var handle))
                    continue;
                
                clip = handle.RawData;
                found = true;

                break;
            }

            if (found)
                break;
        }

        if (!found)
            return 1f;

        float[] data = clip.Data;

        if (data == null)
            return 1f;
        
        lock (peaksForTrackData)
            peaksForTrackData.Add(hash, -1f);

        Task.Run(() => CalculatePeak(hash, clip.Data, clip.Channels, clip.Frequency));

        return 1f;
    }

    private static void CalculatePeak(string hash, float[] data, int channels, int frequency) {
        int peakWidth = 2 * frequency;
        float[] buffer = new float[peakWidth];
        double runningSum = 0d;
        double peak = 0d;

        for (int i = 0, j = 0; i < data.Length; i += channels, j++) {
            float max = 0f;
            
            for (int k = i; k < i + channels; k++) {
                float sample = data[k];
                float square = sample * sample;

                if (square > max)
                    max = square;
            }

            int index = j % peakWidth;

            runningSum += max - buffer[index];
            buffer[index] = max;

            if (runningSum > peak)
                peak = runningSum;
        }

        peak = Math.Sqrt(peak / peakWidth);
        
        // Plugin.Logger.LogMessage($"RMS: {rms:0.000}");
        // Plugin.Logger.LogMessage($"Peak: {peak:0.000}");

        lock (peaksForTrackData)
            peaksForTrackData[hash] = (float) peak;
    }
}