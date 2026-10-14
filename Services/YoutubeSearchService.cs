
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace GlassScreenMac.Services;

public sealed class YouTubeSearchService
{
    private static readonly HttpClient HttpClient = new();

    private readonly string _apiKey;

    public YouTubeSearchService(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException(
                "YouTube API key is required.", nameof(apiKey));

        _apiKey = apiKey;
    }

    public async Task<IReadOnlyList<YouTubeVideo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<YouTubeVideo>();

        var url =
            "https://www.googleapis.com/youtube/v3/search" +
            "?part=snippet" +
            "&type=video" +
            "&videoEmbeddable=true" +
            "&maxResults=20" +
            "&q=" + Uri.EscapeDataString(query) +
            "&key=" + Uri.EscapeDataString(_apiKey);

        using var response = await HttpClient.GetAsync(
            url, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<YouTubeSearchResponse>(
                cancellationToken: cancellationToken);

        var videos = new List<YouTubeVideo>();

        if (result?.Items == null)
            return videos;

        foreach (var item in result.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Id?.VideoId))
                continue;

            videos.Add(new YouTubeVideo(
                item.Id.VideoId,
                item.Snippet?.Title ?? "Untitled",
                item.Snippet?.ChannelTitle ?? "",
                item.Snippet?.Thumbnails?.Medium?.Url ?? ""));
        }

        return videos;
    }

    private sealed class YouTubeSearchResponse
    {
        [JsonPropertyName("items")]
        public List<YouTubeSearchItem>? Items { get; set; }
    }

    private sealed class YouTubeSearchItem
    {
        [JsonPropertyName("id")]
        public YouTubeVideoId? Id { get; set; }

        [JsonPropertyName("snippet")]
        public YouTubeSnippet? Snippet { get; set; }
    }

    private sealed class YouTubeVideoId
    {
        [JsonPropertyName("videoId")]
        public string? VideoId { get; set; }
    }

    private sealed class YouTubeSnippet
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("channelTitle")]
        public string? ChannelTitle { get; set; }

        [JsonPropertyName("thumbnails")]
        public YouTubeThumbnails? Thumbnails { get; set; }
    }

    private sealed class YouTubeThumbnails
    {
        [JsonPropertyName("medium")]
        public YouTubeThumbnail? Medium { get; set; }
    }

    private sealed class YouTubeThumbnail
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}

public sealed record YouTubeVideo(
    string VideoId,
    string Title,
    string ChannelTitle,
    string ThumbnailUrl);
