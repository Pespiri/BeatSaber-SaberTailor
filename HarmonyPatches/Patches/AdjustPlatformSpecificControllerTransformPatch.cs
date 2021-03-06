﻿using HarmonyLib;
using SaberTailor.Settings;
using UnityEngine;

namespace SaberTailor.HarmonyPatches
{
    [HarmonyPatch(typeof(VRPlatformHelper))]
    [HarmonyPatch("AdjustPlatformSpecificControllerTransform")]
    internal class AdjustPlatformSpecificControllerTransformPatch
    {
        private static void Prefix(Transform transform, ref Vector3 addPosition, ref Vector3 addRotation)
        {
            addPosition = Vector3.zero;
            addRotation = Vector3.zero;

            // Always check for sabers first and modify and exit out immediately if found
            if (transform.gameObject.name == "LeftSaber" || transform.gameObject.name.Contains("Saber A"))
            {
                transform.Translate(Configuration.Grip.PosLeft);
                transform.Rotate(Configuration.Grip.RotLeft);
                return;
            }
            else if (transform.gameObject.name == "RightSaber" || transform.gameObject.name.Contains("Saber B"))
            {
                transform.Translate(Configuration.Grip.PosRight);
                transform.Rotate(Configuration.Grip.RotRight);
                return;
            }

            // Check settings if modifications should also apply to menu hilts
            if (Configuration.Grip.ModifyMenuHiltGrip != false)
            {
                if (transform.gameObject.name == "ControllerLeft")
                {
                    transform.Translate(Configuration.Grip.PosLeft);
                    transform.Rotate(Configuration.Grip.RotLeft);
                    return;
                }
                else if (transform.gameObject.name == "ControllerRight")
                {
                    transform.Translate(Configuration.Grip.PosRight);
                    transform.Rotate(Configuration.Grip.RotRight);
                    return;
                }
            }
        }
    }
}
