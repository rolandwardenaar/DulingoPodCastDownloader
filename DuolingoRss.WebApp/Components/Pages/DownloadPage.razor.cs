using Microsoft.AspNetCore.Components;
using PodcastDownloader.Library;
using System.Linq;
using System.Threading;
using System.IO;
using System.Web;

namespace DuolingoRss.WebApp.Components.Pages;

public partial class DownloadPage
{
    [Inject] public PodcastDbService DbService { get; set; } = default!;
    [Inject] public RssDownloadService DownloadService { get; set; } = default!;
    public List<PodcastFeed> Feeds { get; set; } = new();
    private int? _selectedFeedId;
    public int? SelectedFeedId
    {
        get => _selectedFeedId;
        set
        {
            if (_selectedFeedId != value)
            {
                _selectedFeedId = value;
                UpdateDownloadedFiles();
                SelectedFileToPlay = null;
                ProgressText = "";
            }
        }
    }
    public bool IsDownloading { get; set; } = false;
    public string CurrentFeedName { get; set; } = "";
    public string ProgressText { get; set; } = "";
    public CancellationTokenSource? DownloadCts { get; set; }
    public List<string> DownloadedFiles { get; set; } = new();
    public string? SelectedFileToPlay { get; set; }

    protected override void OnInitialized()
    {
        Feeds = DbService.GetFeeds();
        UpdateDownloadedFiles();
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
            await DownloadService.DownloadPodcastsWithProgress(feed.Url, feed.Name, feed.IsLocal, DownloadCts.Token, p => { ProgressText = p; 
                UpdateDownloadedFiles();
                StateHasChanged();
            });
            ProgressText = "Download complete.";

            // Refresh the table after download
            UpdateDownloadedFiles();
            StateHasChanged();
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

    public void UpdateDownloadedFiles()
    {
        DownloadedFiles.Clear();
        if (SelectedFeedId == null) return;
        var feed = Feeds.FirstOrDefault(f => f.Id == SelectedFeedId);
        if (feed == null) return;

        // Use the configured download folder
        var folder = Path.Combine("E:/Data/PodCasts/DuolingoApp", feed.Name);
        if (Directory.Exists(folder))
        {
            DownloadedFiles = Directory.GetFiles(folder, "*.mp3")
                .OrderByDescending(f => File.GetCreationTime(f))
                .Select(Path.GetFileName)
                .ToList();
        }
        else
        {
            DownloadedFiles = new List<string>();
        }
    }

    public void PlayFile(string fileName)
    {
        var feed = Feeds.FirstOrDefault(f => f.Id == SelectedFeedId);
        if (feed == null) return;

        // Encode the file name and feed name
        var encodedFileName = HttpUtility.UrlEncode(fileName);
        var encodedFeedName = HttpUtility.UrlEncode(feed.Name);

        // Combine feed name and file name in the URL
        SelectedFileToPlay = $"{encodedFeedName}/{encodedFileName}";
    }
}
