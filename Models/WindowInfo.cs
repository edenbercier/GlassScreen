namespace GlassScreen.Models;

public class WindowInfo
{
    public IntPtr Handle { get; }

    public string Title { get; }

    public WindowInfo(IntPtr handle, string title)
    {
        Handle = handle;
        Title = title;
    }

    public override string ToString()
    {
        return Title;
    }
}