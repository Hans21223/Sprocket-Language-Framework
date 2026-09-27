using HarmonyLib;
using Il2CppInterop.Runtime;
using Sprocket.SettingConfiguration;
using Sprocket.UI;
using UnityEngine.Events;

namespace SprocketLanguage;

/// Settings > General: a Language list, English and each language file. Choosing one shows it at once.
[HarmonyPatch]
internal static class SettingsOption
{
    [HarmonyPostfix, HarmonyPatch(typeof(GeneralSettingsMenu), nameof(GeneralSettingsMenu.DisplayGUI))]
    static void Draw(IGUILayout layout)
    {
        try
        {
            var codes = new List<string> { "" };
            codes.AddRange(Translation.Available.Keys);
            int at = Math.Max(0, codes.IndexOf(Translation.Current));
            const string Tip = "Sprocket in another language (the Language mod). Other mods' text too, where the language file has it.";
            if (layout.TryCast<DynamicGUI.DynamicGUILayout>() is { } list)
            {
                var names = new Il2CppSystem.Collections.Generic.List<string>();
                foreach (var code in codes) names.Add(Translation.NameOf(code));
                list.Dropdown("Language", names.Cast<Il2CppSystem.Collections.Generic.IReadOnlyList<string>>(), at,
                    DelegateSupport.ConvertDelegate<UnityAction<int>>(new Action<int>(i => Translation.Set(codes[i])))!, Tip);
            }
            else if (layout.TryCast<IGUIElementDrawer>() is { } drawer)
            {
                // Not the game's usual layout: a button that steps through the languages.
                var tip = new UITooltip("Language", Tip);
                drawer.Button($"Language: {Translation.NameOf(codes[at])}  (click to change)",
                    DelegateSupport.ConvertDelegate<UnityAction>(new Action(() => Translation.Set(codes[(at + 1) % codes.Count])))!, ref tip);
            }
        }
        catch (Exception ex) { Translation.Fail("settings", ex); }
    }
}
