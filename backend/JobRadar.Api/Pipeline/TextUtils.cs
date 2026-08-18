using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace JobRadar.Api.Pipeline;

public static partial class TextUtils
{
    /// <summary>Convert source HTML (Arbeitnow descriptions etc.) to readable plain text.</summary>
    public static string HtmlToText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "";

        // Entities must be decoded BEFORE tag stripping: some sources
        // (Arbeitnow) send HTML-encoded markup (&lt;div&gt;…), occasionally
        // double-encoded — decoding after stripping would leave literal tags.
        var text = DecodeEncodedMarkup(html);
        text = ScriptStyleRegex().Replace(text, "");
        text = BlockCloseRegex().Replace(text, "\n");
        text = ListItemRegex().Replace(text, "\n• ");
        text = AnyTagRegex().Replace(text, "");
        text = WebUtility.HtmlDecode(text);
        text = text.Replace('\u00A0', ' '); // &nbsp;
        text = SpacesRegex().Replace(text, " ");
        text = BlankLinesRegex().Replace(text, "\n\n");
        return text.Trim();
    }

    /// <summary>Clean a single-line field: strip tags/entities, collapse whitespace.</summary>
    public static string CleanInline(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "";
        var text = AnyTagRegex().Replace(DecodeEncodedMarkup(s), "");
        text = WebUtility.HtmlDecode(text).Replace('\u00A0', ' ');
        return WhitespaceRegex().Replace(text, " ").Trim();
    }

    private static string DecodeEncodedMarkup(string text)
    {
        for (var i = 0; i < 3 && EncodedTagRegex().IsMatch(text); i++)
            text = WebUtility.HtmlDecode(text);
        return text;
    }

    /// <summary>Lowercase, diacritics stripped, alphanumeric words only — for stable hashing.</summary>
    public static string NormalizeForHash(string? s)
    {
        var lowered = StripDiacritics((s ?? "").ToLowerInvariant());
        return NonAlphanumericRegex().Replace(lowered, " ").Trim();
    }

    /// <summary>"Timișoara" → "Timisoara" (so both spellings hash/compare the same).</summary>
    public static string StripDiacritics(string s)
    {
        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string Sha256Hex(string s)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();

    // "&lt;p" / "&amp;lt;p" — markup that is itself HTML-encoded (possibly twice).
    [GeneratedRegex(@"&(amp;)*lt;\s*/?\s*[a-zA-Z]", RegexOptions.IgnoreCase)]
    private static partial Regex EncodedTagRegex();

    [GeneratedRegex(@"<\s*(script|style)[^>]*>.*?<\s*/\s*\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptStyleRegex();

    [GeneratedRegex(@"<\s*(br|/p|/div|/h[1-6]|/tr|/ul|/ol)\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockCloseRegex();

    [GeneratedRegex(@"<\s*li[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ListItemRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex AnyTagRegex();

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex SpacesRegex();

    [GeneratedRegex(@"\s*\n\s*\n[\s\n]*")]
    private static partial Regex BlankLinesRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRegex();
}
