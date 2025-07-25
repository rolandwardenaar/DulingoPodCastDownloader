using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PodcastDownloader.BlazorApp;
using PodcastDownloader;
using System;
using PodcastDownloader.Library;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

string dbPath = System.IO.Path.Combine(Environment.CurrentDirectory, "podcasts.db");
var dbService = new PodcastDbService(dbPath);
dbService.Initialize();
dbService.SeedFeeds(); 
builder.Services.AddSingleton(dbService);

builder.Services.AddSingleton(sp => new RssDownloadService(new HttpClient(), System.IO.Path.Combine(Environment.CurrentDirectory, "Podcasts"), sp.GetRequiredService<PodcastDbService>()));

await builder.Build().RunAsync();
