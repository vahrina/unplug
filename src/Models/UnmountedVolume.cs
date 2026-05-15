namespace eject_flow.Models;

public sealed class UnmountedVolume
{
    public string Name { get; init; } = "";
    public string? Label { get; init; }
    public ulong SizeBytes { get; init; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Label) ? "unlabeled volume" : Label.Trim();

    public string SizeDisplay
    {
        get
        {
            if (SizeBytes == 0) return "";
            string[] units = { "b", "kb", "mb", "gb", "tb" };
            double s = SizeBytes;
            int u = 0;
            while (s >= 1024 && u < units.Length - 1) { s /= 1024; u++; }
            return $"{s:0.#} {units[u]}";
        }
    }
}
