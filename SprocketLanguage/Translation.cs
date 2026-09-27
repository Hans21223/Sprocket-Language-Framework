using HarmonyLib;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace SprocketLanguage;

/// Swapping the game's text for the chosen language's, and back. The files are the Languages folder next to
/// SprocketLanguage.dll, one a language, named by its code (th.txt), in XUnity.AutoTranslator's format (see
/// Translator). Text is swapped as the game hands it to TextMeshPro, what's already on screen is looked at twice a
/// second, and other mods' simple on-screen text (GUI.Label / GUI.Box) is swapped as it's drawn. Each swapped text
/// remembers its English, so going back to English puts it back at once.
[HarmonyPatch]
internal static class Translation
{
    internal static readonly SortedDictionary<string, string> Available = new(); // code -> file
    static Translator? table;
    static string shown = "";
    static readonly Dictionary<IntPtr, (string English, string Shown)> swapped = new();
    static readonly Dictionary<IntPtr, bool> typedInto = new(); // text boxes the player types in: never swapped
    // Fonts made from Windows' own files, behind the game's (which have no Thai letters): Thai, and Chinese for a zh-CN
    // file. Built from the player's copy, so no font is shipped. Their letters are drawn as they're first needed.
    static readonly string[] FontFiles = { "LeelawUI.ttf", "msyh.ttc" };
    static readonly List<TMP_FontAsset> extraFonts = new();
    static bool fontsMade;
    static readonly HashSet<IntPtr> fontsDone = new();
    static float nextScan, nextFonts;
    static readonly HashSet<string> failures = new();

    static string Folder => Path.Combine(Path.GetDirectoryName(typeof(Translation).Assembly.Location)!, "Languages");

    internal static string NameOf(string code) => code switch
    {
        "" => "English",
        "th" => "ไทย (Thai)",
        _ => code,
    };

    internal static string Current => shown;

    internal static void Start()
    {
        if (Directory.Exists(Folder))
            foreach (var file in Directory.GetFiles(Folder, "*.txt"))
                Available[Path.GetFileNameWithoutExtension(file)] = file;
        Set(Plugin.Chosen.Value.Trim());
    }

    /// Shows `code`'s language ("" for English), and keeps it for next time.
    internal static void Set(string code)
    {
        if (code.Length > 0 && !Available.ContainsKey(code))
        {
            Plugin.ModLog.LogWarning($"No {code}.txt in {Folder}: showing English");
            code = "";
        }
        Plugin.Chosen.Value = code;
        if (code == shown && (code.Length == 0 || table != null)) return;
        PutBack();
        shown = code;
        table = null;
        if (code.Length > 0)
        {
            table = Translator.Parse(File.ReadLines(Available[code]));
            Plugin.ModLog.LogInfo($"Showing {NameOf(code)}: {table.Count} translations from {Path.GetFileName(Available[code])}");
        }
        nextScan = 0;
    }

    /// Every swapped text still showing its translation gets its English back.
    static void PutBack()
    {
        if (swapped.Count == 0) return;
        var saved = table;
        table = null; // so the setter doesn't swap it again
        int back = 0;
        foreach (var o in UnityEngine.Object.FindObjectsOfType(Il2CppType.Of<TMP_Text>()))
            if (o.TryCast<TMP_Text>() is { } t && swapped.TryGetValue(t.Pointer, out var was) && t.text == was.Shown) { t.text = was.English; back++; }
        swapped.Clear();
        table = saved;
        Plugin.ModLog.LogInfo($"English back on {back} texts on screen");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.text), MethodType.Setter)]
    static void Swap(TMP_Text __instance, ref string value)
    {
        if (table == null || string.IsNullOrEmpty(value)) return;
        try
        {
            if (!TypedInto(__instance) && table.Translate(value) is { } t)
            {
                Remember(__instance.Pointer, value, t);
                value = t;
            }
        }
        catch (Exception ex) { Fail("text", ex); }
    }

    // Other mods' simple on-screen text (Quality of Life's hotkeys box and messages, the Battle Editor's panels).
    [HarmonyPrefix, HarmonyPatch(typeof(GUI), nameof(GUI.Label), typeof(Rect), typeof(string))]
    static void Label(ref string text) => SwapDrawn(ref text);

    [HarmonyPrefix, HarmonyPatch(typeof(GUI), nameof(GUI.Box), typeof(Rect), typeof(string))]
    static void Box(ref string text) => SwapDrawn(ref text);

    [HarmonyPrefix, HarmonyPatch(typeof(GUI), nameof(GUI.Box), typeof(Rect), typeof(string), typeof(GUIStyle))]
    static void StyledBox(ref string text) => SwapDrawn(ref text);

    static void SwapDrawn(ref string text)
    {
        if (table == null || string.IsNullOrEmpty(text)) return;
        try { if (table.Translate(text) is { } t) text = t; }
        catch (Exception ex) { Fail("on-screen text", ex); }
    }

    static void Remember(IntPtr text, string english, string translation)
    {
        if (swapped.Count > 50000) swapped.Clear(); // objects come and go with the scenes
        swapped[text] = (english, translation);
    }

    /// From the Ticker, every frame: the fonts now and then, and with a language shown, the text on screen.
    internal static void Update()
    {
        if (Time.unscaledTime >= nextFonts) { nextFonts = Time.unscaledTime + 2; AddFonts(); }
        if (table == null || Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 0.5f;
        if (typedInto.Count > 5000) typedInto.Clear();
        foreach (var o in UnityEngine.Object.FindObjectsOfType(Il2CppType.Of<TMP_Text>()))
            if (o.TryCast<TMP_Text>() is { } t && t.text is { Length: > 0 } s && !TypedInto(t) && table.Translate(s) is { } tr && tr != s)
            {
                Remember(t.Pointer, s, tr);
                t.text = tr;
            }
    }

    /// The text a text box shows what the player types into: left alone (a design named "Engine" stays "Engine").
    static bool TypedInto(TMP_Text t)
    {
        if (!typedInto.TryGetValue(t.Pointer, out bool typed))
            typedInto[t.Pointer] = typed = t.GetComponentInParent<TMP_InputField>() is { } box && box.textComponent?.Pointer == t.Pointer;
        return typed;
    }

    /// Windows' fonts behind every TextMeshPro font the game has loaded (fonts load with the scenes, so again now and
    /// then). Also in English: the Language list shows "ไทย".
    static void AddFonts()
    {
        if (!fontsMade)
        {
            fontsMade = true;
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            foreach (var name in FontFiles)
            {
                var path = Path.Combine(dir, name);
                if (!File.Exists(path)) continue;
                var font = TMP_FontAsset.CreateFontAsset(path, 0, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                if (font == null) { Plugin.ModLog.LogWarning($"Couldn't make a font from {name}"); continue; }
                font.name = "Language " + Path.GetFileNameWithoutExtension(name);
                font.hideFlags = HideFlags.DontUnloadUnusedAsset;
                extraFonts.Add(font);
            }
            Plugin.ModLog.LogInfo($"Fonts behind the game's: {string.Join(", ", extraFonts.Select(f => f.name))}");
        }
        if (extraFonts.Count == 0) return;
        var ours = extraFonts.Select(f => f.Pointer).ToHashSet();
        foreach (var o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<TMP_FontAsset>()))
        {
            if (o.TryCast<TMP_FontAsset>() is not { } font || ours.Contains(font.Pointer) || !fontsDone.Add(font.Pointer)) continue;
            font.fallbackFontAssetTable ??= new Il2CppSystem.Collections.Generic.List<TMP_FontAsset>();
            foreach (var extra in extraFonts)
                if (!font.fallbackFontAssetTable.Contains(extra)) font.fallbackFontAssetTable.Add(extra);
        }
    }

    /// An error in a hook runs inside the game's own code: logged once (per kind), never thrown back.
    internal static void Fail(string what, Exception ex)
    {
        if (failures.Add(what + ex.Message)) Plugin.ModLog.LogError($"Language ({what}): {ex}");
    }
}
