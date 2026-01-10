namespace CPCRemote.UI.ViewModels;

/// <summary>
/// Represents a polling interval option for the dashboard.
/// </summary>
/// <param name="Display">The display text (e.g., "5s").</param>
/// <param name="Seconds">The interval value in seconds.</param>
public sealed record PollingIntervalOption(string Display, int Seconds);
