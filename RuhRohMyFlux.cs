using System;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.UIX;
using HarmonyLib;
using ResoniteModLoader;

public class RuhRohMyFlux : ResoniteMod
{
    public override string Author => "LeCloutPanda";
    public override string Name => "Ruh Roh My Flux";
    public override string Version => "1.0.0";
    public override string Link => "https://github.com/LeCloutPanda/RuhRohMyFlux";

    private enum posOption {
        groupCentre,
        latestNode,
        toolPosition
    }

    public static ModConfiguration config;
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("enabled", "Enabled", () => true);
    [AutoRegisterConfigKey] private static ModConfigurationKey<posOption> POS_OPTION = new ModConfigurationKey<posOption>("posOption", "Positon to play audio", () => posOption.groupCentre);
    [AutoRegisterConfigKey] private static ModConfigurationKey<bool> SPATIALIZE = new ModConfigurationKey<bool>("spatialize", "Spatialize audio", () => true);
    [AutoRegisterConfigKey] private static ModConfigurationKey<float> VOLUME = new ModConfigurationKey<float>("volume", "Audio volume", () => 1.0f);
    [AutoRegisterConfigKey] private static ModConfigurationKey<float> SPEED = new ModConfigurationKey<float>("speed", "Audio speed", () => 1.0f);
    [AutoRegisterConfigKey] private static ModConfigurationKey<string> ERROR_SOUND = new ModConfigurationKey<string>("errorSound", "Error sound", () => "resdb:///221eaa8ecf351bcc035678820fe2a96550b21183fa5c628153f621390db88831.brson");

    public override void OnEngineInit()
    {
        config = GetConfiguration();
        config.Save(true);

        Harmony harmony = new Harmony("dev.lecloutpanda.ruhrohmyflux");
        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(ProtoFluxTool), nameof(ProtoFluxTool.OnEquipped))]
    private class ProtoFluxToolPatch {
        [HarmonyPrefix]
        private static void Prefix(ProtoFluxTool __instance) {
            Slot assetsSlot = __instance.World.AssetsSlot.FindChildOrAdd("RuhRohMyFlux - Assets", false);
            SlotAssets.AttachAudioClip(assetsSlot, new System.Uri(config.GetValue(ERROR_SOUND)), true);
        }
    }

    [HarmonyPatch(typeof(ProtoFluxNodeGroup), nameof(ProtoFluxNodeGroup.Rebuild))]
    private class ProtoFluxNodeGroupPatch {
        [HarmonyPostfix]
        private static void Postfix(ProtoFluxNodeGroup __instance) {
            if (!config.GetValue(ENABLED)) return;

            if (!__instance.IsValid) {
                ProtoFluxTool tool = (ProtoFluxTool) __instance.World.LocalUser.GetActiveTool();
                ProtoFluxNode[] nodes = __instance.Nodes.ToArray();
                ProtoFluxNode node = nodes[nodes.Length - 1];
                Slot nodeSlot = node.Slot;

                if (node.IsRemoved) return;

                StaticAudioClip audioClip = SlotAssets.AttachAudioClip(__instance.World.AssetsSlot.FindChildOrAdd("RuhRohMyFlux - Assets", false), new System.Uri(config.GetValue(ERROR_SOUND)), true);
                AudioClipAssetMetadata audioClipAssetMetadata = nodeSlot.AttachComponent<AudioClipAssetMetadata>();
                audioClipAssetMetadata.AudioClip.Target = audioClip;
                
                nodeSlot.RunInUpdates(3, () => {
                    float3 pos = float3.Zero;
                    switch(config.GetValue(POS_OPTION)) {
                        case posOption.groupCentre: 
                            foreach (var nod in nodes) {
                                pos += nod.Slot.GlobalPosition;
                            }
                            pos /= nodes.Length;
                        break;
                        case posOption.latestNode: 
                            pos = nodeSlot.GlobalPosition;
                        break;
                        case posOption.toolPosition:
                            pos = tool.Slot.GlobalPosition;
                        break;
                    }
                    SlotAssets.PlayOneShot(__instance.World, pos, audioClip, config.GetValue(VOLUME), config.GetValue(SPATIALIZE), config.GetValue(SPEED), nodeSlot, AudioDistanceSpace.Global, true);
                    
                    node.RunInSeconds((float) audioClipAssetMetadata.Duration, () => {
                        audioClipAssetMetadata.Destroy();
                    });
                });
            }
        }
    }
}