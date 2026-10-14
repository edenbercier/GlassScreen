namespace GlassScreenMac.Models;

public sealed class WindowInfo
{
    public WindowInfo(
        uint windowId,
        int ownerPid,
        string ownerName,
        string title)
    {
        WindowId = windowId;
        OwnerPid = ownerPid;
        OwnerName = ownerName;
        Title = title;
    }

    public uint WindowId { get; }

    public int OwnerPid { get; }

    public string OwnerName { get; }

    public string Title { get; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Title)
            ? OwnerName
            : $"{OwnerName} — {Title}";

    public override string ToString() => DisplayName;
}