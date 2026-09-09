using System.Text;
using GlassScreen.Models;
using GlassScreen.Native;

namespace GlassScreen.Services;

public class WindowService
{
    public List<WindowInfo> GetOpenWindows()
    {
        var windows = new List<WindowInfo>();

        NativeMethods.EnumWindows(
            (handle, _) =>
            {
                // Ignore invisible windows
                if (!NativeMethods.IsWindowVisible(handle))
                {
                    return true;
                }

                int titleLength =
                    NativeMethods.GetWindowTextLength(handle);

                // Ignore windows with no title
                if (titleLength <= 0)
                {
                    return true;
                }

                var titleBuilder =
                    new StringBuilder(titleLength + 1);

                NativeMethods.GetWindowText(
                    handle,
                    titleBuilder,
                    titleBuilder.Capacity
                );

                string title =
                    titleBuilder.ToString();

                if (!string.IsNullOrWhiteSpace(title))
                {
                    windows.Add(
                        new WindowInfo(
                            handle,
                            title
                        )
                    );
                }

                // true means:
                // keep enumerating the remaining windows
                return true;
            },
            IntPtr.Zero
        );

        return windows
            .OrderBy(window => window.Title)
            .ToList();
    }
}