﻿using System.IO;
using System.Threading.Tasks;
using Business.StorageAzureServices.Interfaces;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Business.StorageAzureServices.Implementation
{
    public class AzureBlobStorage : IAzureBlobStorage
    {
        private readonly AzureBlobSetings _settings; 
        public AzureBlobStorage(AzureBlobSetings settings)
        {
            _settings = settings;
        }
        private async Task<CloudBlobContainer> GetContainerAsync()
        {
            //Account
            CloudStorageAccount storageAccount = new CloudStorageAccount(
         new StorageCredentials(_settings.StorageAccount, _settings.StorageKey), false);

            //Client
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            //Container
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(_settings.ContainerName);
            await blobContainer.CreateIfNotExistsAsync();

            return blobContainer;
        }

        private async Task<CloudBlockBlob> GetBlockBlobAsync(string blobName)
        {
            //Container
            CloudBlobContainer blobContainer = await GetContainerAsync();

            //Blob
            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(blobName);

            return blockBlob;
        }


        public async Task UploadAsync(string blobName, Stream stream)
        {
            //Blob
            CloudBlockBlob blockBlob = await GetBlockBlobAsync(blobName);

            //Upload
            stream.Position = 0;
            await blockBlob.UploadFromStreamAsync(stream);
        }

        public async Task<string> DownloadAsync(string blobName)
        {
            //Blob
            CloudBlockBlob blockBlob = await GetBlockBlobAsync(blobName);

            //Download
            StreamReader sr=null;
            try
            {
                using (var stream = new MemoryStream())
                {
                    await blockBlob.DownloadToStreamAsync(stream);
                    stream.Position = 0;
                    sr = new StreamReader(stream);
                    return sr.ReadToEnd();
                }
            }
            finally
            {
                if (sr != null)
                {
                    sr.Dispose();
                }
            }
        }

        public async Task DeleteAsync(string blobName)
        {
            //Blob
            CloudBlockBlob blockBlob = await GetBlockBlobAsync(blobName);

            //Delete the blob
            await blockBlob.DeleteAsync();
        }

        public async Task<bool> ExistBlob(string blobName)
        {
            //Blob
            CloudBlockBlob blockBlob = await GetBlockBlobAsync(blobName);

            return await blockBlob.ExistsAsync();
        }
    }
}
