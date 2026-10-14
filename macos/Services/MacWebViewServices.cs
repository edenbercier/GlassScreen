using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace GlassScreenMac.Services;

public static class MacWebViewService
{
    private const string ObjectiveC =
        "/usr/lib/libobjc.A.dylib";

    public static void EnableElementFullscreen(NativeWebView webView)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var handle = webView.TryGetPlatformHandle();

        if (handle == null ||
            handle.Handle == IntPtr.Zero)
        {
            return;
        }

        IntPtr wkWebView = handle.Handle;

        IntPtr configuration =
            SendIntPtr(
                wkWebView,
                GetSelector("configuration"));

        if (configuration == IntPtr.Zero)
        {
            return;
        }

        IntPtr preferences =
            SendIntPtr(
                configuration,
                GetSelector("preferences"));

        if (preferences == IntPtr.Zero)
        {
            return;
        }

        SendBool(
            preferences,
            GetSelector("setElementFullscreenEnabled:"),
            true);
    }

    private static IntPtr GetSelector(string name)
    {
        return sel_registerName(name);
    }

    private static IntPtr SendIntPtr(
        IntPtr receiver,
        IntPtr selector)
    {
        return objc_msgSend_IntPtr(
            receiver,
            selector);
    }

    private static void SendBool(
        IntPtr receiver,
        IntPtr selector,
        bool value)
    {
        objc_msgSend_Bool(
            receiver,
            selector,
            value);
    }

    [DllImport(ObjectiveC)]
    private static extern IntPtr sel_registerName(
        string name);

    [DllImport(
        ObjectiveC,
        EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_IntPtr(
        IntPtr receiver,
        IntPtr selector);

    [DllImport(
        ObjectiveC,
        EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_Bool(
        IntPtr receiver,
        IntPtr selector,
        [MarshalAs(UnmanagedType.I1)] bool value);
}