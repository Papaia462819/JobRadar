namespace JobRadar.Api.Domain;

/// <summary>
/// How far the user has taken a job through their personal funnel.
/// Jobs start as <see cref="New"/> and calm down visually as they progress.
/// </summary>
public enum InteractionState
{
    New,
    Seen,
    Saved,
    Applied,
    Dismissed
}
