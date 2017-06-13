using Microsoft.WindowsAzure.Storage;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Blob;

namespace WordPressOrgCrawler.Lib
{
    public class Config
    {
        public string[] Args => Environment.GetCommandLineArgs();
        public string AzureCredentials { get; set; }
        public string AzureDomain { get; set; }
        public string AzureAppId { get; set; }
        public string AzureAppSecret { get; set; }
        public string DatalakeAccount { get; set; }
        public string BlobContainerName { get; set; }
        public CloudStorageAccount StorageAccount { get; set; }
        public CloudBlobClient BlobClient { get; set; }
        public CloudBlobContainer BlobContainer { get; set; }

        public Config()
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("./config.json"));
            AzureCredentials = config["StorageCredentials"];
            BlobContainerName = config["BlobContainerName"];
            AzureDomain = config["AppDomain"];
            AzureAppId = config["AppId"];
            AzureAppSecret = config["AppSecret"];
            DatalakeAccount = config["DatalakeAccount"];
            StorageAccount = CloudStorageAccount.Parse(AzureCredentials);
            BlobClient = StorageAccount.CreateCloudBlobClient();
            BlobContainer = BlobClient.GetContainerReference(BlobContainerName);
            BlobContainer.CreateIfNotExistsAsync().Wait();
        }
    }
}
