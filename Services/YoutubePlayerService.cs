using System;
using System.Text.RegularExpressions;

namespace GlassScreenMac.Services;

public static class YouTubePlayerService
{
    private static readonly Regex VideoIdPattern =
        new("^[a-zA-Z0-9_-]{11}$", RegexOptions.Compiled);

    public static string BuildPlayerHtml(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId) ||
            !VideoIdPattern.IsMatch(videoId))
        {
            throw new ArgumentException(
                "Invalid YouTube video ID.", nameof(videoId));
        }

        return $$"""
                 <!DOCTYPE html>
                 <html lang="en">
                 <head>
                     <meta charset="UTF-8">
                     <meta name="viewport"
                           content="width=device-width, initial-scale=1">

                     <style>
                         html, body {
                             margin: 0;
                             padding: 0;
                             width: 100%;
                             height: 100%;
                             overflow: hidden;
                             background: transparent;
                         }

                         #player {
                             position: absolute;
                             inset: 0;
                             width: 100%;
                             height: 100%;
                             border: 0;
                         }
                     </style>
                 </head>

                 <body>
                     <iframe
                         id="player"
                         src="https://www.youtube.com/embed/{{videoId}}?playsinline=1&fs=0"
                         title="GlassScreen YouTube Player"
                         referrerpolicy="strict-origin-when-cross-origin"
                         allow="autoplay; encrypted-media; picture-in-picture"
                         allowfullscreen="false">
                     </iframe>
                 </body>
                 </html>
                 """;
    }
}