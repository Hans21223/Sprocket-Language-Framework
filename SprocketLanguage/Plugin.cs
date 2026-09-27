using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace SprocketLanguage;

/// Language: Sprocket in another language (Thai included), chosen in the game's Settings > General > Language. Other
/// mods' text too (Quality of Life, Battle Editor) when their lines are in the language file.
[BepInPlugin("local.sprocket.language", "Language", "0.1.0")]
public sealed class Plugin : BasePlugin
{
    internal static ManualLogSource ModLog = null!;
    internal static ConfigEntry<string> Chosen = null!;

    public override void Load()
    {
        ModLog = Log;
        Chosen = Config.Bind("Language", "Language", "", "The language shown: a file's name in the Languages folder next to SprocketLanguage.dll (th for Thai). Empty: English. Also in the game's Settings > General.");
        var harmony = new Harmony("local.sprocket.language");
        foreach (var part in new[] { typeof(Translation), typeof(SettingsOption) })
        {
            try { harmony.PatchAll(part); }
            catch (Exception ex) { Log.LogError($"{part.Name} disabled, could not attach to the game: {ex}"); }
        }
        Translation.Start();
        AddComponent<Ticker>();
        Log.LogInfo($"Language 0.1.0 loaded: {Translation.Available.Count} language files, showing {Translation.NameOf(Chosen.Value)}");
    }
}

/// Runs in every scene (on BepInEx's own object): the fonts and the text already on screen.
public sealed class Ticker : MonoBehaviour
{
    public Ticker(IntPtr pointer) : base(pointer) { }

    public void Update()
    {
        try { Translation.Update(); }
        catch (Exception ex) { Translation.Fail("update", ex); }
    }
}
