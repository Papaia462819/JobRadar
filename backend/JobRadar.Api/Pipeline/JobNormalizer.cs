using JobRadar.Api.Domain;
using JobRadar.Api.Fetching;

namespace JobRadar.Api.Pipeline;

/// <summary>Turns a source-shaped RawJob into the canonical Job entity.</summary>
public sealed class JobNormalizer(RelevanceScorer scorer)
{
    public Job Normalize(RawJob raw)
    {
        var title = TextUtils.CleanInline(raw.Title);
        var company = TextUtils.CleanInline(raw.Company);
        var location = TextUtils.CleanInline(raw.Location);
        var description = TextUtils.HtmlToText(raw.Description);

        var isRemote = RemoteDetector.IsRemote(raw.RemoteHint, title, location, description);
        var language = LanguageDetector.Detect($"{title}\n{description}");
        var (score, matched) = scorer.Score(title, description, location, isRemote);

        // Dedup on the city part only ("Timișoara, Timis, Romania" → "timisoara")
        // so the same job listed with slightly different location strings
        // still collapses. Company+title carry most of the identity anyway.
        var city = location.Split(',')[0];
        var hash = TextUtils.Sha256Hex(
            $"{TextUtils.NormalizeForHash(company)}|{TextUtils.NormalizeForHash(title)}|{TextUtils.NormalizeForHash(city)}");

        return new Job
        {
            SourceName = raw.SourceName,
            ExternalId = raw.ExternalId,
            Title = title,
            Company = company,
            Location = location,
            IsRemote = isRemote,
            Url = raw.Url,
            Description = description,
            Language = language,
            RelevanceScore = score,
            MatchedKeywords = string.Join(",", matched),
            PostedDate = raw.PostedDate,
            FirstSeenAt = DateTime.UtcNow,
            Notified = false,
            InteractionState = InteractionState.New,
            DedupHash = hash
        };
    }
}
