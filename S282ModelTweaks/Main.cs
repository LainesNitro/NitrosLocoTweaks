using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using CarChanger.Common.Configs;
using CarChanger.Game.Components;
using DV.Logic.Job;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace S282ModelTweaks;

public static class Main
{
    public static UnityModManager.ModEntry Instance { get; private set; } = null!;

    internal static Dictionary<AppliedChange, AxleChanger> axleDictionary = [];
    internal static Dictionary<AppliedChange, DrivetrainChanger> drivetrainDictionary = [];


    static bool Load(UnityModManager.ModEntry modEntry)
    {
        Instance = modEntry;
        try
        {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

        }
        catch (Exception ex)
        {
            modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.UnpatchAll(modEntry.Info.Id);
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(AppliedChange))]
public static class PatchAppliedChange
{
    [HarmonyPatch(nameof(AppliedChange.ApplyS282730A))]
    [HarmonyPostfix]
    public static void ApplyS282730A(AppliedChange __instance, LocoS282730AConfig config)
    {
        if (config.ModificationId.Contains("Axles"))
        {
            if (Main.axleDictionary.TryGetValue(__instance, out var axleChanger))
            {
                axleChanger.Reset();
                Main.axleDictionary.Remove(__instance);
            }

            var axleRoot = config.BodyPrefab?.transform.Find("Axles");

            if (axleRoot == null)
            {
                Main.Instance.Logger.Error("Unable to find axle root prefab!");
                return;
            }

            axleChanger = new(__instance.MatHolder, axleRoot.gameObject);

            Main.axleDictionary.Add(__instance, axleChanger);

            __instance._body.transform.Find("Axles").gameObject.SetActive(false);
        }

        if (config.ModificationId.Contains("Drivetrain"))
        {
            if (Main.drivetrainDictionary.TryGetValue(__instance, out var drivetrainChanger))
            {
                drivetrainChanger.Reset();
                Main.drivetrainDictionary.Remove(__instance);
            }
            var drivetrainRoot = config.BodyPrefab?.transform.Find("Drivetrain");
            
            if (drivetrainRoot == null)
            {
                Main.Instance.Logger.Error("Unable to find drivetrain root prefab!");
                return;
            }

            drivetrainChanger = new(__instance.MatHolder, drivetrainRoot.gameObject);

            Main.drivetrainDictionary.Add(__instance, drivetrainChanger);

            __instance._body.transform.Find("Drivetrain").gameObject.SetActive(false);
        }
    }

    [HarmonyPatch(nameof(AppliedChange.ReturnToDefault))]
    [HarmonyPostfix]
    public static void ReturnToDefault(AppliedChange __instance)
    {
        if (__instance.Config != null)
        {
            if (__instance.Config.ModificationId.Contains("Axles"))
            {
                if (Main.axleDictionary.TryGetValue(__instance, out var axleChanger))
                {
                    axleChanger.Reset();
                    Main.axleDictionary.Remove(__instance);
                }
            }
            if (__instance.Config.ModificationId.Contains("Drivetrain"))
            {
                if (Main.drivetrainDictionary.TryGetValue(__instance, out var drivetrainChanger))
                {
                    drivetrainChanger.Reset();
                    Main.drivetrainDictionary.Remove(__instance);
                }
            }
        }
    }
}