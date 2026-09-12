using System;
using HarmonyLib;
using UnityEngine;

namespace DadsBetterVal;

[HarmonyPatch(typeof(Hud), "Awake")]
internal static class ClockHud
{
    private sealed class ClockDisplay : MonoBehaviour
    {
        private GUIStyle? _style;
        private void OnGUI()
        {
            if (!DadsBetterValPlugin.ClockEnabled.Value || EnvMan.instance == null || Player.m_localPlayer == null || !Hud.IsUserHidden()) return;
            _style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
            _style.fontSize = DadsBetterValPlugin.ClockFontSize.Value;
            _style.normal.textColor = Color.white;
            float fraction = EnvMan.instance.GetDayFraction();
            DateTime time = DateTime.Today.AddHours(fraction * 24d);
            string format = DadsBetterValPlugin.ClockFormat.Value.Replace("DAY", EnvMan.instance.GetDay().ToString());
            GUI.Label(new Rect(Screen.width * 0.35f, 8f, Screen.width * 0.3f, 60f), time.ToString(format), _style);
        }
    }
    private static void Postfix(Hud __instance) { if (__instance.GetComponent<ClockDisplay>() == null) __instance.gameObject.AddComponent<ClockDisplay>(); }
}

