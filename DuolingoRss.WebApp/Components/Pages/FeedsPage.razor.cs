using Microsoft.AspNetCore.Components;
using PodcastDownloader.Library;

namespace DuolingoRss.WebApp.Components.Pages;

public partial class FeedsPage
{
    [Inject] public PodcastDbService DbService { get; set; } = default!;
    public List<PodcastFeed> Feeds { get; set; } = new();
    public PodcastFeed NewFeed { get; set; } = new();
    public PodcastFeed EditFeedModel { get; set; } = new();
    public bool IsAdding { get; set; } = false;
    public bool IsEditing { get; set; } = false;
    public string TestResult { get; set; } = "";

    protected override void OnInitialized()
    {
        Feeds = DbService.GetFeeds();
    }

    public void ShowAddFeed()
    {
        IsAdding = true;
        NewFeed = new PodcastFeed();
    }
    public void CancelAdd() => IsAdding = false;
    public void AddFeed()
    {
        DbService.AddFeed(NewFeed);
        Feeds = DbService.GetFeeds();
        IsAdding = false;
    }
    public void EditFeed(PodcastFeed feed)
    {
        EditFeedModel = new PodcastFeed { Id = feed.Id, Name = feed.Name, Url = feed.Url, IsLocal = feed.IsLocal };
        IsEditing = true;
    }
    public void CancelEdit() => IsEditing = false;
    public void SaveEdit()
    {
        DbService.UpdateFeed(EditFeedModel);
        Feeds = DbService.GetFeeds();
        IsEditing = false;
    }
    public void DeleteFeed(PodcastFeed feed)
    {
        DbService.DeleteFeed(feed.Id);
        Feeds = DbService.GetFeeds();
    }
    public async void TestFeed(PodcastFeed feed)
    {
        TestResult = await DbService.TestFeedUrlAsync(feed.Url, feed.IsLocal);
        StateHasChanged();
    }
}
