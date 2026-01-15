using HarmonyLib;
using Rhythm;
using UnityEngine;
using DG.Tweening;

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

        [HarmonyPatch(typeof(DefaultNote), "Start")]
        public static class Patch_DefaultNote_Start
        {
            private static void Prefix(DefaultNote __instance)
            {
                __instance.transform.DOScale(new Vector3(UnbeatableJudgment.Core.nSize, UnbeatableJudgment.Core.nSize, UnbeatableJudgment.Core.nSize), 0.01f);
            }
        }

        [HarmonyPatch(typeof(DoubleNote), "Start")]
        public static class Patch_DoubleNote_Start
        {
            private static void Prefix(HoldNote __instance)
            {
                __instance.transform.DOScale(new Vector3(UnbeatableJudgment.Core.nSize, UnbeatableJudgment.Core.nSize, UnbeatableJudgment.Core.nSize), 0.01f);
            }
        }
    }
}
