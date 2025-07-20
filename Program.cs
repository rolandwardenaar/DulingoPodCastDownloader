using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.Json;

namespace PodcastDownloader
{
    class Program
    {
        private static readonly HttpClient _client = new HttpClient();
        private static readonly Dictionary<string, (string Url, string Name, bool IsLocal)> _podcastFeeds = new Dictionary<string, (string Url, string Name, bool IsLocal)>
        {
            { "1", ("https://anchor.fm/s/fc714074/podcast/rss", "DuolingoSpanishStories", false) },
            { "2", ("https://www.omnycontent.com/d/playlist/e73c998e-6e60-432f-8610-ae210140c5b1/b3c9b6e7-72ba-45c4-aff9-b1e7012d213b/092b66a8-4329-4183-bb12-b1e7012d216f/podcast.rss", "RadioAmbulante", false) },
            { "3", ("https://coffeebreaklanguages.com/feed/cbs", "CoffeeBreakSpanish", false) },
            { "4", ("http://newsinslowspanish.libsyn.com/rss", "NewsInSlowSpanish", false) },
            { "5", ("https://www.spanishpod101.com/feed", "SpanishPod101", false) },
            { "6", ("https://www.notesinspanish.com/feed", "NotesInSpanish", false) },
            { "7", ("https://www.spanishobsessed.com/feed", "SpanishObsessed", false) },
            { "8", ("https://www.spanishpodcast.net/feed", "SpanishPodcast", false) },
            { "9", ("https://www.podcastsinspanish.org/rss", "PodcastsInSpanish", false) },
            { "10", ("feed.txt", "LocalFeed", true) } // Lokale feed toegevoegd
        };
        private static readonly string _baseDownloadFolder = Path.Combine(Environment.CurrentDirectory, "Podcasts");
        private static CancellationTokenSource _cts = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Welcome to the Spanish Podcast Downloader!");
            Console.WriteLine("Press Ctrl+C to pause and save progress.");

            // Create base download folder
            if (!Directory.Exists(_baseDownloadFolder))
                Directory.CreateDirectory(_baseDownloadFolder);

            // Handle Ctrl+C for graceful interruption
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _cts.Cancel();
                Console.WriteLine("\nDownload interrupted. Saving progress...");
            };

            while (true)
            {
                var selectedFeed = ShowMenu();
                if (selectedFeed == null)
                    break;

                _cts = new CancellationTokenSource(); // Reset cancellation token for new download session
                try
                {
                    await DownloadPodcasts(selectedFeed.Value.Url, selectedFeed.Value.Name, selectedFeed.Value.IsLocal);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Download paused. You can resume by running the program again.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }

        static (string Url, string Name, bool IsLocal)? ShowMenu()
        {
            Console.WriteLine("\nAvailable Podcasts:");
            foreach (var podcastFeed in _podcastFeeds)
            {
                Console.WriteLine($"{podcastFeed.Key}. {podcastFeed.Value.Name} {(podcastFeed.Value.IsLocal ? "(Local)" : "")}");
            }
            Console.WriteLine("0. Exit");
            Console.Write("\nSelect a podcast (0-{0}): ", _podcastFeeds.Count);
            string? input = Console.ReadLine();

            if (input == "0")
                return null;

            if (_podcastFeeds.TryGetValue(input ?? "", out var feed))
                return feed;

            Console.WriteLine("Invalid selection. Please try again.");
            return ShowMenu();
        }

        static async Task DownloadPodcasts(string rssUrl, string podcastName, bool isLocal)
        {
            string podcastFolder = Path.Combine(_baseDownloadFolder, podcastName);
            string progressFile = Path.Combine(_baseDownloadFolder, $"progress_{podcastName}.json");

            // Create podcast-specific folder
            if (!Directory.Exists(podcastFolder))
                Directory.CreateDirectory(podcastFolder);

            // Load progress or initialize new
            var progress = LoadProgress(progressFile);
            var downloadedUrls = new HashSet<string>(progress.DownloadedEpisodes);

            // Fetch and parse RSS feed
            string rssContent;
            try
            {
                if (isLocal)
                {
                    if (!File.Exists(rssUrl))
                    {
                        Console.WriteLine($"\nError: Local feed file '{rssUrl}' not found.");
                        return;
                    }
                    rssContent = await File.ReadAllTextAsync(rssUrl, _cts.Token);
                }
                else
                {
                    rssContent = await _client.GetStringAsync(rssUrl, _cts.Token);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"\nFailed to fetch RSS feed: {ex.Message}");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError reading feed: {ex.Message}");
                return;
            }

            XDocument doc;
            try
            {
                doc = XDocument.Parse(rssContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFailed to parse RSS feed: {ex.Message}");
                return;
            }

            var episodes = doc.Descendants("item")
                .Select(item => new
                {
                    Title = item.Element("title")?.Value,
                    Url = item.Element("enclosure")?.Attribute("url")?.Value,
                    PubDate = item.Element("pubDate")?.Value
                })
                .Where(e => e.Url != null)
                .OrderBy(e => e.PubDate) // Oldest first for sequential numbering
                .ToList();

            if (!episodes.Any())
            {
                Console.WriteLine($"\nWarning: No episodes with audio files found in the RSS feed for {podcastName}.");
                Console.WriteLine("This may be a transcription feed or the feed lacks <enclosure> tags with MP3 URLs.");
                Console.WriteLine("Please verify the RSS feed URL or try a different feed.");
                return;
            }

            Console.WriteLine($"\nFound {episodes.Count} episodes for {podcastName}.");
            int totalEpisodes = episodes.Count;
            int completedEpisodes = downloadedUrls.Count;
            Console.WriteLine($"Already downloaded: {completedEpisodes}/{totalEpisodes}");

            for (int i = 0; i < episodes.Count; i++)
            {
                var episode = episodes[i];
                if (downloadedUrls.Contains(episode.Url))
                    continue;

                // Sanitize title and create filename with sequence number
                string safeTitle = Regex.Replace(episode.Title ?? $"Episode_{i + 1}", "[^a-zA-Z0-9\\-_ ]", "").Trim();
                if (string.IsNullOrEmpty(safeTitle))
                    safeTitle = $"Episode_{i + 1}";
                string fileName = $"{(i + 1):D3}_{safeTitle}.mp3";
                string filePath = Path.Combine(podcastFolder, fileName);

                Console.WriteLine($"\nDownloading ({i + 1}/{totalEpisodes}): {episode.Title}");

                try
                {
                    // Download with progress reporting
                    using var response = await _client.GetAsync(episode.Url, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
                    response.EnsureSuccessStatusCode();

                    long? contentLength = response.Content.Headers.ContentLength;
                    using var stream = await response.Content.ReadAsStreamAsync(_cts.Token);
                    using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                    byte[] buffer = new byte[8192];
                    long totalBytesRead = 0;
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, _cts.Token)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), _cts.Token);
                        totalBytesRead += bytesRead;

                        if (contentLength.HasValue)
                        {
                            double progressPercent = (double)totalBytesRead / contentLength.Value * 100;
                            Console.Write($"\rProgress: {progressPercent:F2}% ({totalBytesRead / 1024 / 1024} MB / {contentLength.Value / 1024 / 1024} MB)");
                        }
                    }

                    Console.WriteLine("\nDownload completed.");
                    downloadedUrls.Add(episode.Url);
                    progress.DownloadedEpisodes = downloadedUrls.ToList();
                    SaveProgress(progress, progressFile);
                    completedEpisodes++;
                    Console.WriteLine($"Total progress: {completedEpisodes}/{totalEpisodes}");
                }
                catch (OperationCanceledException)
                {
                    // Clean up partial download
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                    SaveProgress(progress, progressFile);
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nFailed to download {episode.Title}: {ex.Message}");
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
            }

            Console.WriteLine($"\nAll episodes for {podcastName} downloaded successfully!");
        }

        static Progress LoadProgress(string progressFile)
        {
            if (File.Exists(progressFile))
            {
                string json = File.ReadAllText(progressFile);
                return JsonSerializer.Deserialize<Progress>(json) ?? new Progress();
            }
            return new Progress();
        }

        static void SaveProgress(Progress progress, string progressFile)
        {
            string json = JsonSerializer.Serialize(progress, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(progressFile, json);
        }
    }

    class Progress
    {
        public List<string> DownloadedEpisodes { get; set; } = new List<string>();
    }
}