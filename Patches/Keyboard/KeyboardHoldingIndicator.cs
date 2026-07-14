using Cysharp.Threading.Tasks;
using DG.Tweening;
using HarmonyLib;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using XSOverlay;

namespace xsoverlay_tweak.Patches.Keyboard
{
    internal class KeyboardHoldingIndicator
    {
        private class ButtonData
        {
            public int State = 0;
        }
        private static readonly ConditionalWeakTable<KeyboardKey, ButtonData> ButtonDictionary = new();

        private const float animationDuration = 0.05f;
        private static readonly FieldInfo seqField = AccessTools.Field(typeof(ButtonAnimator), "Seq");

        [HarmonyPatch(typeof(KeyboardKey), "VirtualKeyboardEvent")]
        [HarmonyPostfix]
        public static void VirtualKeyboardEvent(KeyboardKey __instance, bool ___IsKeyDown)
        {
            if (IsEnable())
            {
                if (__instance.IsKeyToggle) // Caps Lock
                    __instance.IsKeyHeld = !___IsKeyDown;

                DoKeyDownAnimation(__instance);
            }
        }

        [HarmonyPatch(typeof(KeyboardKey), nameof(KeyboardKey.ReleaseStickyKeyEvent))]
        [HarmonyPostfix]
        public static void ReleaseStickyKeyEvent(KeyboardKey __instance)
        {
            if (IsEnable())
                DoKeyDownAnimation(__instance);
        }

        [HarmonyPatch(typeof(KeyboardKey), nameof(KeyboardKey.LockKeyDown))]
        [HarmonyPostfix]
        public static void LockKeyDown(KeyboardKey __instance)
        {
            if (IsEnable() && __instance.IsDoubleTappable)
                DoKeyDownAnimation(__instance);
        }

        [HarmonyPatch(typeof(KeyboardKey), nameof(KeyboardKey.HoldKeyDown))]
        [HarmonyPostfix]
        public static void HoldKeyDown(KeyboardKey __instance)
        {
            if (IsEnable())
                DoKeyDownAnimation(__instance);
        }

        private static void DoKeyDownAnimation(KeyboardKey key)
        {
            ButtonData Data = ButtonDictionary.GetOrCreateValue(key);
            Transform transform = key.transform;

            bool isDown = key.IsKeyHeld;
            bool isSticky = key.KeyIsCurrentlyHoldLocked;
            bool isHolding = key.HoldingKeyButtonDown;

            if (isDown || isSticky || isHolding)
            {
                if (isSticky)
                {
                    if (Data.State != 2) // Sticky
                    {
                        Data.State = 2;
                        XSTools.ExecuteOnMainThread(async () =>
                        {
                            await UniTask.DelayFrame(2); // Wait for ButtonAnimator.OnClickAnimateButton() to play
                            StopOriginalAnimation(key.GetComponentInChildren<ButtonAnimator>(true));

                            transform.DOKill();
                            transform.DOScale(new Vector3(0.75f, 0.75f, 1f), animationDuration).SetEase(Ease.OutQuad).SetUpdate(UpdateType.Normal, true);
                        });
                    }
                }
                else if (Data.State != 1) // Hold
                {
                    Data.State = 1;
                    XSTools.ExecuteOnMainThread(async () =>
                    {
                        await UniTask.DelayFrame(2);
                        StopOriginalAnimation(key.GetComponentInChildren<ButtonAnimator>(true));

                        transform.DOKill();
                        transform.DOScale(new Vector3(0.9f, 0.9f, 1f), animationDuration).SetEase(Ease.OutQuad).SetUpdate(UpdateType.Normal, true);
                    });
                }
            }
            else if (Data.State == 1 || Data.State == 2) // Normal
            {
                Data.State = 0;
                XSTools.ExecuteOnMainThread(async delegate
                {
                    await UniTask.DelayFrame(2);
                    StopOriginalAnimation(key.GetComponentInChildren<ButtonAnimator>(true));

                    transform.DOKill();
                    transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutQuad).SetUpdate(UpdateType.Normal, true);
                });
            }
        }

        private static void StopOriginalAnimation(ButtonAnimator animator)
        {
            if (animator != null && seqField != null)
            {
                Sequence Seq = (Sequence)seqField.GetValue(animator);
                Seq?.Pause();
            }
        }

        private static bool IsEnable()
        {
            return XConfig.KeyboardHoldingIndicator.Value;
        }
    }
}