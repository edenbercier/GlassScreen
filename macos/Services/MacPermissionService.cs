using System;
using System.Diagnostics;

namespace GlassScreenMac.Services;

public static class MacPermissionService
{
    private const string InputMonitoringSettingsUrl =
        "x-apple.systempreferences:com.apple.preference.security?Privacy_ListenEvent";

    public static void OpenInputMonitoringSettings()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "open",
            ArgumentList =
            {
                InputMonitoringSettingsUrl
            },
            UseShellExecute = false
        });
    }
}