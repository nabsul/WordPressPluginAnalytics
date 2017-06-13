using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using cloudscribe.HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage.Blob;
using WordPressOrgCrawler.Lib;

namespace WordPressOrgCrawler.Commands
{
    public class DownloadCommand : ICommand
    {
        private static string AllPlugins = "https://wordpress.org/plugins/page/1/?s";
        private static string WordpressPlugin = "https://wordpress.org/latest.zip";
        private HttpClient _client = new HttpClient();
        protected Config _config;
        protected StreamWriter _errors;
        private CloudBlobContainer _container;

        public async Task RunAsync(Config config)
        {
            _config = config;
            _container = _config.BlobContainer;
            _errors = new StreamWriter(File.OpenWrite("./errors.log"));

            var subCommand = _config.Args[2];
            switch (subCommand)
            {
                case "plugins":
                    await DownloadPluginsAsync();
                    break;
                case "wordpress":
                    await DownloadWordPressAsync();
                    break;
                default:
                    throw new Exception($"I don't know how to download {subCommand}");
            }
        }

        private async Task DownloadWordPressAsync()
        {
            var response = await _client.GetAsync(WordpressPlugin);
            var stream = await response.Content.ReadAsStreamAsync();
            var blob = _container.GetBlockBlobReference("wordpress/wordpress-latest.zip");
            await blob.UploadFromStreamAsync(stream);
        }

        private async Task DownloadPluginsAsync()
        {
            try
            {
                var hasMore = true;
                var pageNumber = 1;
                while(hasMore)
                {
                    Console.WriteLine($"Page number: {pageNumber}");
                    var url = AllPlugins.Replace("/1/", $"/{pageNumber}/");
                    hasMore = await ReadPluginListPageAsync(url);
                    pageNumber++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private async Task<bool> ReadPluginListPageAsync(string url)
        {
            var response = await _client.GetAsync(url);
            var stream = await response.Content.ReadAsStreamAsync();
            var doc = new HtmlDocument();
            doc.Load(stream);
            var nodes = doc.DocumentNode.SelectNodes("//h2[@class='entry-title']/a");
            var tasks = nodes.Select(n => HandlePluginPageAsync(n.Attributes["href"].Value)).ToList();
            await Task.WhenAll(tasks);

            // if the last item in the nav-links is a span (and not a link, we've reached the end)
            var linkNodes = doc.DocumentNode.SelectNodes("//div[@class='nav-links']");
            var last = linkNodes.Last().ChildNodes.Last();
            return last.Name != "span";
        }

        private async Task HandlePluginPageAsync(string url)
        {
            try
            {
                var zipUrl = await GetPluginZipFileAsync(url);
                var response = await _client.GetAsync(zipUrl);
                var path = response.RequestMessage.RequestUri.AbsolutePath.Substring(1); //skip the first slash
                var stream = await response.Content.ReadAsStreamAsync();
                var blob = _container.GetBlockBlobReference(path);
                await blob.UploadFromStreamAsync(stream);
                Console.WriteLine($"{url} uploaded");
            }
            catch (Exception e)
            {
                var message = $"{url} failed: {e.ToString()}";
                await _errors.WriteLineAsync(message);
                Console.WriteLine(message);
            }
        }

        public async Task<string> GetPluginZipFileAsync(string pluginPage)
        {
            var response = await _client.GetAsync(pluginPage);
            var stream = await response.Content.ReadAsStreamAsync();
            var doc = new HtmlDocument();
            doc.Load(stream);
            var button = doc.DocumentNode.SelectNodes("//div[@class='plugin-actions']/a").First();
            return button.Attributes["href"].Value;
        }
    }
}
