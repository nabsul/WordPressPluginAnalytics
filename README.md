# WordPress Plugin Analytics - Hooks Usage

This project consists of a command line tool that can be used to extract hook usage information from 
WordPress and all the public plugins. 

# Building the project

To build this project, make sure you have dotnet core installed, check out this code and run:
- `dotnet restore`
- `dotnet build`

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

- `dotnet run download` to copy WordPress and all the plugins to blob storage
- `dotnet run extract` to extract hook usage from WordPress and the plugins
- `dotnet run datalake upload` to upload the two extraction files to Data Lake storage
- `dotnet run datalake submit` to run the Analytics job
- `dotnet run datalake download` to download the results from Data Lake storage

You can also use `dotnet run datalake list-files` and `dotnet run datalake list-jobs` to see files and jobs in your data lake account.
