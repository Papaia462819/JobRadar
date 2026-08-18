using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace JobRadar.Api.Pipeline;

public sealed class ProfileKeyword
{
    public string Term { get; set; } = "";
    public int Weight { get; set; } = 1;
}

public sealed class ProfileOptions
{
    public int TitleWeightMultiplier { get; set; } = 3;
    public int TimisoaraBoost { get; set; } = 4;
    public int RemoteBoost { get; set; } = 2;
    public List<ProfileKeyword> Keywords { get; set; } = [];
}

/// <summary>
/// Scores a job against the keyword profile in configuration ("Profile").
/// Title hits count TitleWeightMultiplier× a description hit; jobs in
/// Timișoara or fully remote get a small boost so the relevance sort
/// surfaces jobs the user can actually take.
/// </summary>
public sealed class RelevanceScorer
{
    private readonly ProfileOptions _opts;
    private readonly List<(string Term, int Weight, Regex Pattern)> _keywords;

    public RelevanceScorer(IOptions<ProfileOptions> options)
    {
        _opts = options.Value;
        // Custom word boundaries because \b fails on terms like "c#" and ".net":
        // the char before/after the term must not be a letter or digit.
        _keywords = _opts.Keywords
            .Where(k => !string.IsNullOrWhiteSpace(k.Term))
            .Select(k => (
                k.Term,
                k.Weight,
                new Regex($@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(k.Term.Trim())}(?![\p{{L}}\p{{N}}])",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled)))
            .ToList();
    }

    public (int Score, IReadOnlyList<string> Matched) Score(
        string title, string description, string location, bool isRemote)
    {
        var score = 0;
        var matched = new List<string>();

        foreach (var (term, weight, pattern) in _keywords)
        {
            var inTitle = pattern.IsMatch(title);
            var inDescription = pattern.IsMatch(description);

            if (inTitle) score += weight * _opts.TitleWeightMultiplier;
            if (inDescription) score += weight;
            if (inTitle || inDescription) matched.Add(term);
        }

        // "timi" catches Timișoara/Timisoara/Timis in either spelling.
        if (location.Contains("timi", StringComparison.OrdinalIgnoreCase))
            score += _opts.TimisoaraBoost;
        if (isRemote)
            score += _opts.RemoteBoost;

        return (score, matched);
    }
}
