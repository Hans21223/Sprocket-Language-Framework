using System.Text;
using System.Text.RegularExpressions;

namespace SprocketLanguage;

/// A translation file in XUnity.AutoTranslator's text format, and looking text up in it (no game code: tested offline).
/// One `English=translation` a line; `\n`, `\r`, `\t`, `\=` and `\\` are escapes; `//` starts a comment. Numbers match
/// by template: `{{A}} km/h=...` covers "40 km/h" and "12.5 km/h", each letter standing for the number in its place.
/// `r:"pattern"=replacement` lines are regular expressions ($1 for a group), tried when nothing else matches.
internal sealed class Translator
{
    readonly Dictionary<string, string> exact = new();
    readonly List<(Regex Pattern, string Replacement)> patterns = new();
    // Lines with {{A}}...: filed under their text with the numbers taken out, so a text finds its few candidates at
    // once. Each is matched whole, a letter taking one number; digits written in the line stay as written ("(G1)").
    readonly Dictionary<string, List<(Regex Match, string Value)>> templates = new();
    readonly Dictionary<string, string?> seen = new(); // looked up before: the translation, or null for none
    const string NumberPattern = @"\d+(?:[.,]\d+)?";
    static readonly Regex Number = new(NumberPattern, RegexOptions.Compiled);
    static readonly Regex Letter = new(@"\{\{([A-Z])\}\}", RegexOptions.Compiled);
    const int SeenLimit = 20000; // texts that change every frame (a speed, a timer) would otherwise pile up

    internal int Count => exact.Count + patterns.Count + templates.Values.Sum(l => l.Count);

    /// The text with its numbers (and a line's {{A}} letters) taken out.
    static string Skeleton(string s) => Number.Replace(Letter.Replace(s, ""), "");

    void AddTemplate(string key, string value)
    {
        var letters = new List<char>();
        var pattern = new StringBuilder("^");
        int at = 0;
        foreach (Match m in Letter.Matches(key))
        {
            pattern.Append(Regex.Escape(key.Substring(at, m.Index - at)));
            char letter = m.Groups[1].Value[0];
            pattern.Append(letters.Contains(letter) ? @"\k<" + letter + ">" : "(?<" + letter + ">" + NumberPattern + ")");
            letters.Add(letter);
            at = m.Index + m.Length;
        }
        pattern.Append(Regex.Escape(key.Substring(at))).Append('$');
        var skeleton = Skeleton(key);
        if (!templates.TryGetValue(skeleton, out var list)) templates[skeleton] = list = new();
        list.Add((new Regex(pattern.ToString()), value));
    }

    internal static Translator Parse(IEnumerable<string> lines)
    {
        var t = new Translator();
        foreach (var raw in lines)
        {
            var line = raw.TrimStart('\uFEFF').TrimEnd('\r');
            if (line.Length == 0 || line.StartsWith("//")) continue;
            if (line.StartsWith("r:\""))
            {
                int end = line.IndexOf("\"=", 3, StringComparison.Ordinal);
                if (end < 0) continue;
                try { t.patterns.Add((new Regex(line.Substring(3, end - 3), RegexOptions.Compiled), Unescape(line.Substring(end + 2)))); }
                catch (ArgumentException) { } // a pattern .NET can't read: skipped, not fatal
                continue;
            }
            int split = Split(line);
            if (split <= 0) continue;
            var key = Unescape(line.Substring(0, split));
            var value = Unescape(line.Substring(split + 1));
            if (value.Length == 0) continue;
            if (Letter.IsMatch(key)) t.AddTemplate(key, value);
            else t.exact[key] = value;
        }
        return t;
    }

    /// Where the first `=` that isn't escaped is, or -1.
    static int Split(string line)
    {
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '\\') { i++; continue; }
            if (line[i] == '=') return i;
        }
        return -1;
    }

    static string Unescape(string s)
    {
        if (s.IndexOf('\\') < 0) return s;
        var b = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] != '\\' || i + 1 == s.Length) { b.Append(s[i]); continue; }
            char c = s[++i];
            switch (c)
            {
                case 'n': b.Append('\n'); break;
                case 'r': b.Append('\r'); break;
                case 't': b.Append('\t'); break;
                case '=': case '\\': b.Append(c); break;
                default: b.Append('\\').Append(c); break; // not ours (a regex's \[): kept as written
            }
        }
        return b.ToString();
    }

    /// The translation of `text`, or null when the file has none.
    internal string? Translate(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        if (seen.TryGetValue(text, out var known)) return known;
        var found = Find(text);
        // Only what's worth keeping around: a text with numbers that has no translation changes often.
        if (found != null || !Number.IsMatch(text))
        {
            if (seen.Count >= SeenLimit) seen.Clear();
            seen[text] = found;
        }
        return found;
    }

    string? Find(string text)
    {
        if (exact.TryGetValue(text, out var value)) return value;
        // Numbers by letter: "40 km/h" matches "{{A}} km/h", and its translation gets the 40 back where {{A}} is.
        if (Number.IsMatch(text) && templates.TryGetValue(Skeleton(text), out var candidates))
            foreach (var (match, translation) in candidates)
                if (match.Match(text) is { Success: true } m)
                    return Letter.Replace(translation, l => m.Groups[l.Groups[1].Value] is { Success: true } g ? g.Value : l.Value);
        foreach (var (pattern, replacement) in patterns)
            if (pattern.IsMatch(text)) return pattern.Replace(text, replacement);
        // The same text with space round it (a padded label): translated inside, the space kept.
        var core = text.Trim();
        if (core.Length > 0 && core.Length < text.Length && Translate(core) is { } inner)
            return text.Substring(0, text.IndexOf(core, StringComparison.Ordinal)) + inner + text.Substring(text.IndexOf(core, StringComparison.Ordinal) + core.Length);
        // Several lines with no line of their own (a list that grows, like one line a gear): each line on its own.
        if (text.IndexOf('\n') >= 0)
        {
            var lines = text.Split('\n');
            bool any = false;
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].Length > 0 && Translate(lines[i]) is { } line) { lines[i] = line; any = true; }
            if (any) return string.Join("\n", lines);
        }
        return null;
    }
}
