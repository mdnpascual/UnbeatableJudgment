using System;
using System.Collections.Generic;
using System.Text;
using MelonLoader;
using HarmonyLib;
using Rhythm;
using TMPro;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace UnbeatableJudgment.Patches
{
    public static class GamePatches
    {
        [HarmonyPatch(typeof(BackgroundAccuracyDisplayboard), nameof(BackgroundAccuracyDisplayboard.Display))]
        public static class Patch_OnJudgementDisplay
        {
            private static void Postfix(AttackInfo attack, Score score)
            {
                //Melon<Core>.Logger.Msg($"Precise: {attack.GetPreciseAccuracyText(150f)}");
                string delayString = attack.GetPreciseAccuracyText(150f);
                string[] scoreSplit = delayString.Split(' ');

                if (scoreSplit[0] == "+")
                {
                    UnbeatableJudgment.Core.PushJudgementOffset(float.Parse(scoreSplit[1]));
                } else
                {
                    UnbeatableJudgment.Core.PushJudgementOffset(float.Parse(scoreSplit[1]) * -1);
                } 
            }
        }
    }
}
