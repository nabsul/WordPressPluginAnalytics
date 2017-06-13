#WordPress Analytics - Hooks Usage

The project consists of a series of tools used to extract and analyze hook usage data in public WordPress.org plugins.

# Initial setup configuration

The tool relies on Azure blob storage and data lake analytics to perform its operations. 
Config settings should be placed in `config.json`. You can use `config.template.json` as a template. 
Here's some more information about these settings:

- StorageCredentials: The connection string used to connect to you blog storage account
- BlobContainerName: The blog container name to store everything in
- AppDomain: This is normally the "domain" name that you see when you hover your mouse over your name in the upper right corder of the Azure portal
- AppId: You'll need to create an "App Registration" in Azure and grant the app access to your Data Lake accounts
- AppSecret: Create an access key for the App that you register
- DatalakeAccount:  The name of the Data Lake Analytics and storage accounts

# Usage

Here are the commands/steps for running this tool:

- `dotnet run download [wordpress/plugins]` to copy WordPress and all the plugins to blob storage
- `dotnet run extract [wordpress/plugin]` to extract hook usage from WordPress and the plugins
- `dotnet run datalake upload [file] [destination]` to upload the two extraction files to Data Lake storage
- `dotnet run datalake submit` to run the Analytics job
- `dotnet run datalake download [file]` to download the results from Data Lake storage
