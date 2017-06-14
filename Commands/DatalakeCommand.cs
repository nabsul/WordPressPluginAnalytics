using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WordPressPluginAnalytics.Lib;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Azure.Management.DataLake.Store;
using Microsoft.Azure.Management.DataLake.Analytics;
using Newtonsoft.Json;
using Microsoft.Rest;
using System.IO;
using Microsoft.Azure.Management.DataLake.Store.Models;
using FileType = Microsoft.Azure.Management.DataLake.Store.Models.FileType;
using Microsoft.Azure.Management.DataLake.Analytics.Models;
using System.Linq;

namespace WordPressPluginAnalytics.Commands
{
    public class DatalakeCommand : ICommand
    {
        private Config _config;
        private ServiceClientCredentials _credentials;

        public async Task RunAsync(Config config)
        {
            _config = config;
            _credentials = await ApplicationTokenProvider.LoginSilentAsync(_config.AzureDomain, _config.AzureAppId, _config.AzureAppSecret);
            var subCommand = _config.Args[2];
            var lookup = new Dictionary<string, Func<Task>>()
            {
                {"list-files", ListFilesAsync},
                {"list-jobs", ListJobsAsync},
                {"upload", UploadAsync},
                {"download", DownloadAsync},
                {"submit", SubmitJobAsync},
            };

            if(!lookup.TryGetValue(subCommand, out var func))
            {
                throw new Exception($"I don't know how to {subCommand}");
            }

            await func();
        }

        private async Task ListFilesAsync()
        {
            var client = new DataLakeStoreFileSystemManagementClient(_credentials);
            var account = "wordpressanalytics";
            var path = "/";
            var result = await client.FileSystem.ListFileStatusAsync(account, path);
            foreach(var item in result.FileStatuses.FileStatus)
            {
                if(item.Type != FileType.FILE)
                {
                    continue;
                }
                Console.WriteLine($"{item.PathSuffix} - {item.Length}");
            }
        }

        private async Task ListJobsAsync()
        {
            var jobClient = new DataLakeAnalyticsJobManagementClient(_credentials);
            var jobs =(await jobClient.Job.ListAsync("wordpressanalytics")).OrderBy(j => j.SubmitTime);
            foreach(var job in jobs)
            {
                Console.WriteLine($"{job.JobId} - {job.SubmitTime} - {job.EndTime} - {job.Result.ToString()}");
            }
        }

        private async Task SubmitJobAsync()
        {
            var script = File.ReadAllText("./Scripts/AnalyzeHooks.usql")
                .Replace("@@WordPressExtraction@@", $"{_config.BlobContainerName}-wordpress.tsv.gz")
                .Replace("@@PluginsExtraction@@", $"{_config.BlobContainerName}-plugin.tsv.gz")
                .Replace("@@Output@@", $"{_config.BlobContainerName}-hook_usage.tsv");
            
            var jobClient = new DataLakeAnalyticsJobManagementClient(_credentials);
            var jobProps = new USqlJobProperties(script);
            var info = new JobInformation($"WordPress Hook Usage Analysis - {_config.BlobContainerName}", JobType.USql, jobProps, priority: 1, degreeOfParallelism: 1);
            var result = await jobClient.Job.CreateAsync(_config.DatalakeAccount, Guid.NewGuid(), info);
            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
        }


        private async Task UploadAsync()
        {
            var client = new DataLakeStoreFileSystemManagementClient(_credentials);
            await UploadFileAsync(client, $"{_config.BlobContainerName}-wordpress.tsv.gz");
            await UploadFileAsync(client, $"{_config.BlobContainerName}-plugin.tsv.gz");
        }

        private const int FileChunkSize = 20000000; // upload file in 10M byte chunks

        private async Task UploadFileAsync(DataLakeStoreFileSystemManagementClient client, string fileName)
        {
            var destinationPath = $"/{fileName}";
            var buffer = new byte[FileChunkSize];
            var uploadChunks = new List<string>();
            int chunkNum = 0;

            using (var stream = File.OpenRead(fileName))
            {
                int numBytes = 0;
                while(0 < (numBytes = await stream.ReadAsync(buffer, 0, FileChunkSize)))
                {
                    var chunkPath = $"{destinationPath}_{chunkNum++}";
                    Console.WriteLine($"uploading {chunkPath}");
                    await client.FileSystem.CreateAsync(_config.DatalakeAccount, chunkPath, new MemoryStream(buffer, 0, numBytes));
                    uploadChunks.Add(chunkPath);
                }
            }

            Console.WriteLine($"Concatenating {uploadChunks.Count} chunks...");
            await client.FileSystem.ConcatAsync(_config.DatalakeAccount, destinationPath, uploadChunks);

            Console.WriteLine($"Deleting chunks...");
            foreach(var chunk in uploadChunks)
            {
                await client.FileSystem.DeleteAsync(_config.DatalakeAccount, chunk);
            }            
        }

        private async Task DownloadAsync()
        {
            var client = new DataLakeStoreFileSystemManagementClient(_credentials);
            var fileName = $"{_config.BlobContainerName}-hook_usage.tsv";
            var stream = await client.FileSystem.OpenAsync(_config.DatalakeAccount, $"/{fileName}");
            var output = File.OpenWrite(fileName);
            await stream.CopyToAsync(output);
        }
    }
}
