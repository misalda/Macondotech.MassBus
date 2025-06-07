using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using MassTransit;
using MassTransit.MessageData;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CTM.EnterpriseBus.Common.Infrastructure
{
    public class BlobMessageDataRepository : IMessageDataRepository
    {
        private const string DocumentContainerName = "servicebus-oversized-messages";
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<BlobMessageDataRepository> _logger;
        private readonly HttpClient _httpClient;
        private readonly BlobContainerClient _containerClient;

        public BlobMessageDataRepository(
            BlobServiceClient blobServiceClient, 
            ILogger<BlobMessageDataRepository> logger,
            HttpClient httpClient)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
            _httpClient = httpClient;
            _containerClient = _blobServiceClient.GetBlobContainerClient(DocumentContainerName);
        }

        public async Task<Stream> Get(Uri address, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(address, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading blob from {Uri}", address);
                throw;
            }
        }

        public async Task<Uri> Put(Stream stream, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            try
            {
                await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

                var blobClient = _containerClient.GetBlobClient(Guid.NewGuid().ToString());
                await blobClient.UploadAsync(stream, cancellationToken);

                var sasUri = GetBlobSasUri(blobClient, timeToLive);
                return sasUri;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading blob to storage");
                throw;
            }
        }

        private Uri GetBlobSasUri(BlobClient blobClient, TimeSpan? timeToLive = null)
        {
            try
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = DocumentContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b", // b for blob
                    StartsOn = DateTimeOffset.UtcNow,
                    ExpiresOn = DateTimeOffset.UtcNow.Add(timeToLive ?? TimeSpan.FromMinutes(60))
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                return sasUri;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SAS token");
                throw;
            }
        }
    }
}
// using MassTransit.MessageData;
// using Microsoft.Extensions.Logging;
// using Microsoft.WindowsAzure.Storage;
// using Microsoft.WindowsAzure.Storage.Blob;
// using Microsoft.WindowsAzure.Storage.RetryPolicies;
// using System;
// using System.IO;
// using System.Net;
// using System.Threading;
// using System.Threading.Tasks;

// namespace CTM.EnterpriseBus.Common.Infrastructure
// {
//     public class BlobMessageDataRepository : IMessageDataRepository
//     {
//         private const string DocumentContainerName = "servicebus-oversized-messages";
//         private readonly CloudStorageAccount _storageAccount;
//         private readonly ILogger<BlobMessageDataRepository> _logger;

//         public BlobMessageDataRepository(CloudStorageAccount storageAccount, ILogger<BlobMessageDataRepository> logger)
//         {
//             _storageAccount = storageAccount;
//             _logger = logger;
//         }

//         public async Task<Stream> Get(Uri address, CancellationToken cancellationToken = default)
//         {
//             using (var client = new WebClient())
//             {
//                 return new MemoryStream(await client.DownloadDataTaskAsync(address));
//             }
//         }

//         public async Task<Uri> Put(Stream stream, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
//         {
//             try
//             {
//                 var client = GetCloudBlobClient();
//                 var container = client.GetContainerReference(DocumentContainerName);

//                 await container.CreateIfNotExistsAsync();

//                 var blob = container.GetBlockBlobReference(Guid.NewGuid().ToString());
//                 await blob.UploadFromStreamAsync(stream).ConfigureAwait(false);

//                 return new Uri(blob.Uri + GetSasForBlobUsingAccessPolicy(blob));
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Put error");
//                 throw;
//             }
//         }

//         private CloudBlobClient GetCloudBlobClient()
//         {
//             try
//             {
//                 var blobClient = _storageAccount.CreateCloudBlobClient();

//                 IRetryPolicy linearRetryPolicy = new LinearRetry(TimeSpan.FromSeconds(2), 3);
//                 blobClient.DefaultRequestOptions.RetryPolicy = linearRetryPolicy;

//                 return blobClient;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "GetCloudBlobClient error");
//                 throw;
//             }
            
//         }
//         public string GetSasForBlobUsingAccessPolicy(CloudBlockBlob cloudBlockBlob, TimeSpan? timeToLive = null)
//         {
//             try
//             {
//                 SharedAccessBlobPolicy sharedPolicy = new SharedAccessBlobPolicy()
//                 {
//                     SharedAccessExpiryTime = timeToLive != null ? DateTime.Now.Add(timeToLive.Value) : DateTime.Now.AddMinutes(60),
//                     Permissions = SharedAccessBlobPermissions.Read
//                 };

//                 //using that shared access policy, get the sas token and set the url
//                 return cloudBlockBlob.GetSharedAccessSignature(sharedPolicy);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "GetSasForBlobUsingAccessPolicy error");
//                 throw;
//             }
//         }
//     }
// }
