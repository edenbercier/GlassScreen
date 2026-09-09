namespace GlassScreen.Models;

public class WindowControlState
{
    public WindowInfo Window { get; }

    public int Opacity { get; set; } = 100;

    public bool ClickThrough { get; set; }

    public bool AlwaysOnTop { get; set; }

    public WindowControlState(WindowInfo window)
    {
        Window = window;
    }
}