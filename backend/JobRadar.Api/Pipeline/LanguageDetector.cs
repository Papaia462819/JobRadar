using System.Text.RegularExpressions;

namespace JobRadar.Api.Pipeline;

/// <summary>
/// Lightweight stopword/diacritic heuristic. Detects "ro", "en" or "de" —
/// "de" is included because Arbeitnow carries many German listings and
/// mislabelling those as English would pollute the language filter.
/// </summary>
public static partial class LanguageDetector
{
    private static readonly HashSet<string> RoWords = new(StringComparer.Ordinal)
    {
        "și", "si", "să", "sa", "pentru", "din", "care", "este", "sunt", "avem",
        "echipa", "echipă", "cerinte", "cerințe", "experienta", "experiență",
        "cunostinte", "cunoștințe", "dezvoltator", "companie", "beneficii",
        "salariu", "candidatul", "responsabilitati", "responsabilități",
        "cautam", "căutăm", "munca", "muncă", "lucru", "angajam", "angajăm"
    };

    private static readonly HashSet<string> EnWords = new(StringComparer.Ordinal)
    {
        "the", "and", "with", "for", "you", "your", "our", "we", "are", "team",
        "experience", "skills", "requirements", "developer", "looking", "join",
        "work", "will", "have", "knowledge", "about", "role", "years"
    };

    private static readonly HashSet<string> DeWords = new(StringComparer.Ordinal)
    {
        "und", "der", "die", "das", "mit", "für", "wir", "sie", "du", "dein",
        "deine", "erfahrung", "kenntnisse", "entwickler", "aufgaben", "bieten",
        "bei", "auf", "als", "oder", "werden", "unsere", "sowie", "bereich",
        "unternehmen", "sind", "nicht", "eine", "einen"
    };

    public static string Detect(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "en";

        var lowered = text.ToLowerInvariant();
        int ro = 0, en = 0, de = 0;

        // Language-specific letters are a strong signal on their own.
        foreach (var c in lowered)
        {
            switch (c)
            {
                case 'ă' or 'â' or 'î' or 'ș' or 'ț' or 'ş' or 'ţ': ro++; break;
                case 'ä' or 'ö' or 'ü' or 'ß': de++; break;
            }
        }

        // Cap tokens so huge descriptions stay cheap.
        var tokens = WordRegex().Matches(lowered).Take(500);
        foreach (Match m in tokens)
        {
            var word = m.Value;
            if (RoWords.Contains(word)) ro += 2;
            if (EnWords.Contains(word)) en += 2;
            if (DeWords.Contains(word)) de += 2;
        }

        var max = Math.Max(ro, Math.Max(en, de));
        if (max == 0) return "en";
        if (max == ro) return "ro";
        if (max == de) return "de";
        return "en";
    }

    [GeneratedRegex(@"[\p{L}]+")]
    private static partial Regex WordRegex();
}
