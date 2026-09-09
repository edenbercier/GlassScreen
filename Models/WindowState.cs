namespace GlassScreen.Models;

public class WindowState
{
    public IntPtr Handle { get; }

    public long OriginalExtendedStyle { get; }

    public bool WasTopMost { get; }

    public WindowState(
        IntPtr handle,
        long originalExtendedStyle,
        bool wasTopMost)
    {
        Handle = handle;
        OriginalExtendedStyle = originalExtendedStyle;
        WasTopMost = wasTopMost;
    }
}