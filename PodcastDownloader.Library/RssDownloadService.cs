public async Task DownloadPodcastsWithProgress(string feedUrl, string feedName, bool isLocal, CancellationToken cancellationToken, Action<string> progressCallback, Action refreshGridCallback)
{
    // Simulate download logic
    int completedEpisodes = 0;
    int totalEpisodes = 10; // Example total episodes

    for (int i = 0; i < totalEpisodes; i++)
    {
        await Task.Delay(1000, cancellationToken); // Simulate download delay
        completedEpisodes++;

        // Trigger progress callback
        progressCallback?.Invoke($"Total progress: {completedEpisodes}/{totalEpisodes}");

        // Trigger grid refresh callback
        refreshGridCallback?.Invoke();
    }
}