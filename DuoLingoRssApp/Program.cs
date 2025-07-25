using PodcastDownloader.Library;

namespace PodcastDownloader
{
    class Program
    {
        private static readonly HttpClient _client = new HttpClient();
        private static readonly string _baseDownloadFolder = Path.Combine(Environment.CurrentDirectory, "Podcasts");
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        private static readonly string _dbPath = Path.Combine(Environment.CurrentDirectory, "podcasts.db");

        static async Task Main(string[] args)
        {
            Console.WriteLine("Welcome to the Spanish Podcast Downloader!");
            Console.WriteLine("Press Ctrl+C to pause and save progress.");

            if (!Directory.Exists(_baseDownloadFolder))
                Directory.CreateDirectory(_baseDownloadFolder);

            PodcastDbService dbService = new PodcastDbService(_dbPath);
            dbService.Initialize();
            dbService.SeedFeeds(); // Seed feeds if not present

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _cts.Cancel();
                Console.WriteLine("\nDownload interrupted. Saving progress...");
            };

            var rssService = new RssDownloadService(_client, _baseDownloadFolder, dbService);

            while (true)
            {
                var selectedFeed = ShowMenu(dbService);
                if (selectedFeed == null)
                    break;

                _cts = new CancellationTokenSource();
                try
                {
                    await rssService.DownloadPodcasts(selectedFeed.Value.Url, selectedFeed.Value.Name, selectedFeed.Value.IsLocal, _cts.Token);
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

        static (string Url, string Name, bool IsLocal)? ShowMenu(PodcastDbService dbService)
        {
            var feeds = dbService.GetFeeds();
            Console.WriteLine("\nAvailable Podcasts:");
            foreach (var feed in feeds)
            {
                Console.WriteLine($"{feed.Id}. {feed.Name} {(feed.IsLocal ? "(Local)" : "")}");
            }
            Console.WriteLine("0. Exit");
            Console.Write("\nSelect a podcast (0-{0}): ", feeds.Count);
            string? input = Console.ReadLine();

            if (input == "0")
                return null;

            var selected = feeds.Find(f => f.Id.ToString() == input);
            if (selected != null)
                return (selected.Url, selected.Name, selected.IsLocal);

            Console.WriteLine("Invalid selection. Please try again.");
            return ShowMenu(dbService);
        }
    }

    class Progress
    {
        public List<string> DownloadedEpisodes { get; set; } = new List<string>();
    }
}