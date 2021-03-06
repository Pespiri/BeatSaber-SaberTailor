﻿using IPA.Utilities;
using SaberTailor.Settings;
using SaberTailor.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Xft;

namespace SaberTailor.Tweaks
{
    public class SaberLength : MonoBehaviour
    {
        public static string Name => "SaberLength";
        public static bool IsPreventingScoreSubmission => Configuration.Scale.ScaleHitBox;

#pragma warning disable IDE0051 // Used by MonoBehaviour
        private void Start() => Load();
#pragma warning restore IDE0051 // Used by MonoBehaviour

        private void Load()
        {
            // Allow the user to run in any mode, but don't allow ScoreSubmission
            if (IsPreventingScoreSubmission)
            {
                ScoreUtility.DisableScoreSubmission(Name);
            }
            else if (ScoreUtility.ScoreIsBlocked)
            {
                ScoreUtility.EnableScoreSubmission(Name);
            }

            StartCoroutine(ApplyGameCoreModifications());
        }

        private IEnumerator ApplyGameCoreModifications()
        {
            bool usingCustomModels = false;
            Saber defaultLeftSaber = null;
            Saber defaultRightSaber = null;
            GameObject LeftSaber = null;
            GameObject RightSaber = null;

            // Find and set the default sabers first
            IEnumerable<Saber> sabers = Resources.FindObjectsOfTypeAll<Saber>();
            foreach (Saber saber in sabers)
            {
                if (saber.saberType == SaberType.SaberB)
                {
                    defaultLeftSaber = saber;
                    LeftSaber = saber.gameObject;
                }
                else if (saber.saberType == SaberType.SaberA)
                {
                    defaultRightSaber = saber;
                    RightSaber = saber.gameObject;
                }
            }

            if (Utilities.Utils.IsPluginEnabled("Custom Sabers"))
            {
                // Wait a moment for CustomSaber to catch up
                yield return new WaitForSeconds(0.1f);
                GameObject customSaberClone = GameObject.Find("_CustomSaber(Clone)");

                // If customSaberClone is null, CustomSaber is most likely not replacing the default sabers.
                if (customSaberClone != null)
                {
                    LeftSaber = GameObject.Find("LeftSaber");
                    RightSaber = GameObject.Find("RightSaber");
                    usingCustomModels = true;
                }
                else
                {
                    Logger.log.Debug("Either the Default Sabers are selected or CustomSaber were too slow!");
                }
            }

            // Scaling default saber will affect its hitbox, so save the default hitbox positions first before scaling
            HitboxRevertWorkaround hitboxVariables = null;
            if (!usingCustomModels && !Configuration.Scale.ScaleHitBox)
            {
                hitboxVariables = new HitboxRevertWorkaround(defaultLeftSaber, defaultRightSaber);
            }

            // Rescale visible sabers (either default or custom)
            RescaleSaber(LeftSaber, Configuration.Scale.Length, Configuration.Scale.Girth);
            RescaleSaber(RightSaber, Configuration.Scale.Length, Configuration.Scale.Girth);

            // Scaling custom sabers will not change their hitbox, so a manual hitbox rescale is necessary, if the option is enabled
            if (usingCustomModels && Configuration.Scale.ScaleHitBox)
            {
                RescaleSaberHitBox(defaultLeftSaber, Configuration.Scale.Length);
                RescaleSaberHitBox(defaultRightSaber, Configuration.Scale.Length);
            }

            // Revert hitbox changes to default sabers, if hitbox scaling is disabled
            if (hitboxVariables != null)
            {
                hitboxVariables.RestoreHitbox();
            }

            IEnumerable<BasicSaberModelController> basicSaberModelControllers = Resources.FindObjectsOfTypeAll<BasicSaberModelController>();
            foreach (BasicSaberModelController basicSaberModelController in basicSaberModelControllers)
            {
                XWeaponTrail saberWeaponTrail = basicSaberModelController.GetField<XWeaponTrail, BasicSaberModelController>("_saberWeaponTrail");
                if (!usingCustomModels || saberWeaponTrail.name != "BasicSaberModel")
                {
                    RescaleWeaponTrail(saberWeaponTrail, Configuration.Scale.Length, usingCustomModels);
                }
            }

            yield return null;
        }

        private void RescaleSaber(GameObject saber, float lengthMultiplier, float widthMultiplier)
        {
            if (saber != null)
            {
                saber.transform.localScale = Vector3Extensions.Rescale(saber.transform.localScale, widthMultiplier, widthMultiplier, lengthMultiplier);
            }
        }

        private void RescaleSaberHitBox(Saber saber, float lengthMultiplier)
        {
            if (saber != null)
            {
                Transform topPos = saber.GetField<Transform, Saber>("_topPos");
                Transform bottomPos = saber.GetField<Transform, Saber>("_bottomPos");

                topPos.localPosition = Vector3Extensions.Rescale(topPos.localPosition, 1.0f, 1.0f, lengthMultiplier);
                bottomPos.localPosition = Vector3Extensions.Rescale(bottomPos.localPosition, 1.0f, 1.0f, lengthMultiplier);
            }
        }

        private void RescaleWeaponTrail(XWeaponTrail trail, float lengthMultiplier, bool usingCustomModels)
        {
            float trailWidth = trail.GetField<float, XWeaponTrail>("_trailWidth");
            trail.SetField("_trailWidth", trailWidth * lengthMultiplier);

            // Fix the local z position for the default trail on custom sabers
            if (usingCustomModels)
            {
                Transform pointEnd = trail.GetField<Transform, XWeaponTrail>("_pointEnd");
                pointEnd.localPosition = Vector3Extensions.Rescale(pointEnd.localPosition, 1.0f, 1.0f, pointEnd.localPosition.z * lengthMultiplier);
            }
        }

        /// <summary>
        /// Work-Around for reverting Saber Hit-box scaling
        /// </summary>
        private class HitboxRevertWorkaround
        {
            private readonly Transform leftSaberTop;
            private readonly Transform leftSaberBot;
            private readonly Transform rightSaberTop;
            private readonly Transform rightSaberBot;

            private Vector3 leftDefaultHitboxTopPos;
            private Vector3 leftDefaultHitboxBotPos;
            private Vector3 rightDefaultHitboxTopPos;
            private Vector3 rightDefaultHitboxBotPos;

            public HitboxRevertWorkaround(Saber defaultLeftSaber, Saber defaultRightSaber)
            {
                // Scaling default saber will affect its hitbox, so save the default hitbox positions first before scaling
                SetHitboxDefaultPosition(defaultLeftSaber, out leftSaberTop, out leftSaberBot);
                leftDefaultHitboxTopPos = leftSaberTop.position.Clone();
                leftDefaultHitboxBotPos = leftSaberBot.position.Clone();

                SetHitboxDefaultPosition(defaultRightSaber, out rightSaberTop, out rightSaberBot);
                rightDefaultHitboxTopPos = rightSaberTop.position.Clone();
                rightDefaultHitboxBotPos = rightSaberBot.position.Clone();
            }

            /// <summary>
            /// Restores the sabers original Hit-box scale
            /// </summary>
            public void RestoreHitbox()
            {
                leftSaberTop.position = leftDefaultHitboxTopPos;
                leftSaberBot.position = leftDefaultHitboxBotPos;
                rightSaberTop.position = rightDefaultHitboxTopPos;
                rightSaberBot.position = rightDefaultHitboxBotPos;
            }

            private void SetHitboxDefaultPosition(Saber saber, out Transform saberTop, out Transform saberBot)
            {
                saberTop = saber.GetField<Transform, Saber>("_topPos");
                saberBot = saber.GetField<Transform, Saber>("_bottomPos");
            }
        }
    }
}
