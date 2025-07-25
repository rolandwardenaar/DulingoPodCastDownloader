using Microsoft.AspNetCore.Components;
using PodcastDownloader.Library;
using System.Linq;
using System.Threading;

namespace PodcastDownloader.BlazorApp.Pages;

public partial class DownloadPage
{
    [Inject] public PodcastDbService DbService { get; set; } = default!;
    [Inject] public RssDownloadService DownloadService { get; set; } = default!;
    public List<PodcastFeed> Feeds { get; set; } = new();
    public int? SelectedFeedId { get; set; }
    public bool IsDownloading { get; set; } = false;
    public string CurrentFeedName { get; set; } = "";
    public string ProgressText { get; set; } = "";
    public CancellationTokenSource? DownloadCts { get; set; }

    protected override void OnInitialized()
    {
        Feeds = DbService.GetFeeds();
    }

    public async void StartDownload()
    {
        if (SelectedFeedId == null) return;
        var feed = Feeds.FirstOrDefault(f => f.Id == SelectedFeedId);
        if (feed == null) return;
        IsDownloading = true;
        CurrentFeedName = feed.Name;
        ProgressText = "Starting...";
        DownloadCts = new CancellationTokenSource();
        try
        {
            await DownloadService.DownloadPodcastsWithProgress(feed.Url, feed.Name, feed.IsLocal, DownloadCts.Token, p => { ProgressText = p; StateHasChanged(); });
            ProgressText = "Download complete.";
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Download stopped.";
        }
        catch (Exception ex)
        {
            ProgressText = $"Error: {ex.Message}";
        }
        IsDownloading = false;
        StateHasChanged();
    }

    public void StopDownload()
    {
        DownloadCts?.Cancel();
    }
}
