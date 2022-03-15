using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SMU.Utilities;
using SpinCore;
using SpinCore.UI;
using Utility;

namespace SRXDAudioNormalization;

[BepInDependency("com.pink.spinrhythm.moddingutils", "1.0.2")]
[BepInDependency("com.pink.spinrhythm.spincore")]
[BepInPlugin("SRXD.AudioNormalization", "AudioNormalization", "1.0.0.0")]
public class Plugin : SpinPlugin {
    public new static ManualLogSource Logger { get; private set; }
    
    public static Bindable<float> PreferredLoudness { get; private set; }
    
    public static Bindable<float> MaxGain { get; private set; }

    protected override void Awake() {
        base.Awake();

        Logger = base.Logger;
        PreferredLoudness = AddBindableConfig("PreferredLoudness", -4.0f);
        MaxGain = AddBindableConfig("MaxGain", 3.5f);
        AudioNormalization.Init();

        var harmony = new Harmony("AudioNormalization");
        
        harmony.PatchAll(typeof(Patches));
    }
    
    protected override void CreateMenus() {
        var root = CreateOptionsTab("Audio Normalization").UIRoot;

        SpinUI.CreateSlider("Preferred Loudness", root, -10f, 0f, valueDisplay: value => $"{value:F}db").Bind(PreferredLoudness);
        SpinUI.CreateSlider("Maximum Gain", root, 0f, 5f, valueDisplay: value => $"{value:F}db").Bind(MaxGain);
    }
}