using System;
using System.IO;
using System.Threading.Tasks;
using WordPressOrgCrawler.Lib;
using System.IO.Compression;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Linq;
using System.Collections.Concurrent;

namespace WordPressOrgCrawler.Commands
{
    public class ExtractCommand : ICommand
    {
        const int Parallelism = 10;
         private StreamWriter _errors;
        private GZipStream _zip;
        private CloudBlobContainer _container;

        public async Task RunAsync(Config config)
        {
            var subDirectoryName = config.Args[2];
            _errors = new StreamWriter(File.OpenWrite("./errors.log"));
            var outputName = $"{config.BlobContainerName}-{subDirectoryName}.tsv.gz";
            if(File.Exists(outputName))
            {
                File.Delete(outputName);
            }
            
            using (var file = File.Open(outputName, FileMode.OpenOrCreate))
            using (var zip = new GZipStream(file, CompressionLevel.Optimal))
            {
                _zip = zip;
                _container = config.BlobContainer;
                await ExtractAllBlobsAsync($"{subDirectoryName}/");
            }
        }

        public async Task ExtractAllBlobsAsync(string path)
        {
            var isComplete = false;
            BlobContinuationToken token = null;
            while (!isComplete)
            {
                var response = await _container.ListBlobsSegmentedAsync(path, token);

                foreach(var blob in response.Results)
                {
                    await ZipToExtractionAsync(blob.Uri.AbsolutePath);
                }

                token = response.ContinuationToken;
                isComplete = null == token;
            }
        }

        private async Task ZipToExtractionAsync(string name)
        {
            try
            {
                await ZipToExtractionInternalAsync(name);
            }
            catch (Exception e)
            {
                Console.WriteLine("failed");
                await LogError($"Failed: {name}");
                await LogError(e.ToString());
            }
        }

        private async Task LogError(string message)
        {
            Console.WriteLine(message);
            await _errors.WriteLineAsync(message);
        }

        private async Task ZipToExtractionInternalAsync(string name)
        {
            if (!name.EndsWith(".zip"))
            {
                return;
            }

            var from = name.Substring(name.IndexOf('/', 1) + 1);
            Console.Write($"Extracting from {from} ");

            using (var zipStream = await _container.GetBlobReference(from).OpenReadAsync())
            using (var zipExtractor = new ZipExtractor(from, zipStream))
            using (var extractionStream = new ExtractionStream(zipExtractor.Extract()))
            {
                await extractionStream.CopyToAsync(_zip);
                Console.WriteLine($"Bytes written: {extractionStream.Position}");
            }
        }
    }
}
