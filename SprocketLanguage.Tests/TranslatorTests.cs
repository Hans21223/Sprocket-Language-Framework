using SprocketLanguage;

/// The translation file: plain lines, escapes, number templates, regular expressions, padding, and what isn't there.
static class TranslatorTests
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("translator: " + message); }

    /// The repository's folder (the one with SprocketMod.props), found from where the tests run.
    static string Root()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "SprocketMod.props"))) return dir.FullName;
        throw new Exception("translator: can't find the repository folder");
    }

    public static void Run()
    {
        var t = Translator.Parse(new[]
        {
            "﻿// a comment",
            "Mirroring=มิเรอร์",
            @"Next mission (ENTER)\nReplay (SPACE)=ภารกิจถัดไป (ENTER)\nเล่นซ้ำ (SPACE)",
            @"<color\=yellow>Largest step \= {{A}}rpm (G1)=<color\=yellow>ช่วงห่างมากสุด \= {{A}}rpm (G1)",
            "{{A}}m {{B}}s Elapsed={{A}} นาที {{B}} วินาที",
            "{{A}} km/h={{A}} กม./ชม.",
            @"r:""^(.+) \[Gunner\]$""=$1 [พลยิง]",
            "Untranslated=",
        });
        Check(t.Count == 6, $"6 entries read (got {t.Count})");
        Check(t.Translate("Mirroring") == "มิเรอร์", "a plain line");
        Check(t.Translate("Next mission (ENTER)\nReplay (SPACE)") == "ภารกิจถัดไป (ENTER)\nเล่นซ้ำ (SPACE)", "\\n in both sides");
        Check(t.Translate("<color=yellow>Largest step = 1760rpm (G1)") == "<color=yellow>ช่วงห่างมากสุด = 1760rpm (G1)", "\\= and a number template");
        Check(t.Translate("3m 12s Elapsed") == "3 นาที 12 วินาที", "two numbers, each in its place");
        Check(t.Translate("12.5 km/h") == "12.5 กม./ชม.", "a decimal is one number");
        Check(t.Translate("Main gun [Gunner]") == "Main gun [พลยิง]", "a regular expression with a group");
        Check(t.Translate("  Mirroring ") == "  มิเรอร์ ", "padding kept round the translation");
        Check(t.Translate("Untranslated") == null && t.Translate("Nothing like it") == null && t.Translate("") == null, "no translation: null");
        Check(t.Translate("Mirroring") == "มิเรอร์", "the second look-up (remembered) gives the same");
        Check(t.Translate("40 km/h\nMirroring\nNot in the file\n") == "40 กม./ชม.\nมิเรอร์\nNot in the file\n", "several lines with no line of their own: each line on its own");
        // The shipped Thai file, and another pack when one is given (LANGUAGE_TEST_PACK=its file): every line reads, and
        // a line that only matches by its numbers is found.
        var packs = new List<string> { Path.Combine(Root(), "Release", "BepInEx", "plugins", "SprocketLanguage", "Languages", "th.txt") };
        if (Environment.GetEnvironmentVariable("LANGUAGE_TEST_PACK") is { Length: > 0 } extra) packs.Add(extra);
        foreach (var pack in packs)
        {
            var lines = File.ReadAllLines(pack);
            var real = Translator.Parse(lines);
            int entries = lines.Count(l => l.Length > 0 && !l.StartsWith("//"));
            Check(real.Count >= entries - 5, $"{Path.GetFileName(pack)}: {real.Count} of {entries} lines read");
            Check(real.Translate("(<color=#FFD123>45</color>/67)mm 10.5<sup>o</sup>\n<color=#C0C0C0ff>(Effective/Line-of-sight/Raw)\n") is { } armour && armour.Contains("45") && armour.Contains("10.5"),
                  $"{Path.GetFileName(pack)}: the armour tooltip found by its numbers");
            Console.WriteLine($"PACK_OK: {Path.GetFileName(pack)}, {real.Count} entries");
        }
        var thai = Translator.Parse(File.ReadAllLines(packs[0]));
        Check(thai.Translate("Mantlet") == "หน้ากากปืน" && thai.Translate("Rivets") == "หมุด", "th.txt: the words chosen for mantlet and rivets");
        Console.WriteLine("TRANSLATOR_TESTS_OK: plain, escapes, number templates, regex, padding, shipped Thai");
    }
}
