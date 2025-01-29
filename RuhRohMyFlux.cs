using System.Linq;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using ResoniteModLoader;

public class RuhRohMyFlux : ResoniteMod
{
    public override string Author => "LeCloutPanda";
    public override string Name => "Ruh Roh My Flux";
    public override string Version => "1.0.0";
    public override string Link => "https://github.com/LeCloutPanda/RuhRohMyFlux";

    public static ModConfiguration config;
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("enabled", "Enabled", () => true);
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> LOCAL_ONLY = new ModConfigurationKey<bool>("localOnly", "Play audio locally only <color=red>(Leave this on so you don't annoy others)", () => true);
    [AutoRegisterConfigKey] private static ModConfigurationKey<float> VOLUME = new ModConfigurationKey<float>("volume", "Audio volume", () => 1.0f);
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> SPATIALIZE = new ModConfigurationKey<bool>("spatialize", "Spatialize audio", () => true);
    [AutoRegisterConfigKey] private static ModConfigurationKey<float> SPEED = new ModConfigurationKey<float>("speed", "Audio speed", () => 1.0f);
    [AutoRegisterConfigKey] private static ModConfigurationKey<string> ERROR_SOUND = new ModConfigurationKey<string>("errorSound", "Error sound", () => "resdb:///221eaa8ecf351bcc035678820fe2a96550b21183fa5c628153f621390db88831.brson");

    public override void OnEngineInit()
    {
        config = GetConfiguration();
        config.Save(true);

        Harmony harmony = new Harmony("dev.lecloutpanda.ruhrohmyflux");
        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(ProtoFluxNodeGroup), nameof(ProtoFluxNodeGroup.Rebuild))]
    private class ProtoFluxNodeGroupPatch {
        [HarmonyPostfix]
        private static void Postfix(ProtoFluxNodeGroup __instance) {
            if (!__instance.IsValid) {
                ProtoFluxNode[] nodes = __instance.Nodes.ToArray();
                Slot node = nodes[nodes.Length - 1].Slot;
                if (node.IsRemoved) return;
                float3 position = node.GlobalPosition;
                AudioClipAssetMetadata audioClipAssetMetadata = node.AttachComponent<AudioClipAssetMetadata>();
                StaticAudioClip audioClip = SlotAssets.AttachAudioClip(node, new System.Uri(config.GetValue(ERROR_SOUND)), true);
                audioClip.Persistent = false;
                audioClipAssetMetadata.AudioClip.Target = audioClip;

                node.RunInUpdates(3, () => {
                    SlotAssets.PlayOneShot(node, audioClip, config.GetValue(VOLUME), config.GetValue(SPATIALIZE), config.GetValue(SPEED), true, AudioDistanceSpace.Global, config.GetValue(LOCAL_ONLY));
                    node.RunInSeconds((float) audioClipAssetMetadata.Duration, () => {
                        audioClip.Destroy();
                        audioClipAssetMetadata.Destroy();
                    });
                });

            }
        }
    }
}