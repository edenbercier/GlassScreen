using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GlassScreenMac.Native;

public static class MacNativeMethods
{
    private const string CoreGraphics =
        "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    private const string CoreFoundation =
        "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    public const uint WindowListOptionOnScreenOnly = 1;
    public const uint WindowListExcludeDesktopElements = 16;

    public const uint NullWindowId = 0;

    private const uint Utf8Encoding = 0x08000100;

    private const int CfNumberSInt32Type = 3;

    [DllImport(CoreGraphics)]
    public static extern IntPtr CGWindowListCopyWindowInfo(
        uint option,
        uint relativeToWindow);

    [DllImport(CoreFoundation)]
    public static extern nint CFArrayGetCount(
        IntPtr array);

    [DllImport(CoreFoundation)]
    public static extern IntPtr CFArrayGetValueAtIndex(
        IntPtr array,
        nint index);

    [DllImport(CoreFoundation)]
    public static extern IntPtr CFDictionaryGetValue(
        IntPtr dictionary,
        IntPtr key);

    [DllImport(CoreFoundation)]
    public static extern IntPtr CFStringCreateWithCString(
        IntPtr allocator,
        string value,
        uint encoding);

    [DllImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool CFStringGetCString(
        IntPtr handle,
        StringBuilder buffer,
        nint bufferSize,
        uint encoding);

    [DllImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool CFNumberGetValue(
        IntPtr number,
        int numberType,
        out int value);

    [DllImport(CoreFoundation)]
    public static extern void CFRelease(
        IntPtr handle);

    public static string? GetDictionaryString(
        IntPtr dictionary,
        string keyName)
    {
        IntPtr key =
            CFStringCreateWithCString(
                IntPtr.Zero,
                keyName,
                Utf8Encoding);

        try
        {
            IntPtr value =
                CFDictionaryGetValue(
                    dictionary,
                    key);

            if (value == IntPtr.Zero)
            {
                return null;
            }

            var buffer =
                new StringBuilder(4096);

            bool success =
                CFStringGetCString(
                    value,
                    buffer,
                    buffer.Capacity,
                    Utf8Encoding);

            return success
                ? buffer.ToString()
                : null;
        }
        finally
        {
            CFRelease(key);
        }
    }

    public static int? GetDictionaryInt(
        IntPtr dictionary,
        string keyName)
    {
        IntPtr key =
            CFStringCreateWithCString(
                IntPtr.Zero,
                keyName,
                Utf8Encoding);

        try
        {
            IntPtr value =
                CFDictionaryGetValue(
                    dictionary,
                    key);

            if (value == IntPtr.Zero)
            {
                return null;
            }

            bool success =
                CFNumberGetValue(
                    value,
                    CfNumberSInt32Type,
                    out int number);

            return success
                ? number
                : null;
        }
        finally
        {
            CFRelease(key);
        }
    }
    private const string SkyLight =
        "/System/Library/PrivateFrameworks/SkyLight.framework/SkyLight";

    [DllImport(SkyLight)]
    public static extern int CGSMainConnectionID();

    [DllImport(SkyLight)]
    public static extern int CGSSetWindowAlpha(
        int connectionId,
        uint windowId,
        float alpha);
}