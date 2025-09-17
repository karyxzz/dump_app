using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace unzip_final
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Function1))]
        public async Task Run(
            [BlobTrigger("dhara-logicapp/zipe-files/{name}", Connection = "StorageConnection")] Stream stream,
            string name)
        {
            _logger.LogInformation($"Blob trigger function processed blob. Name: {name}");

            // Get connection string from environment/application settings
            string connectionString = Environment.GetEnvironmentVariable("StorageConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Storage connection string not configured.");
                return;
            }

            // Test Blob connectivity
            try
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                // Attempt to list containers (quick connection check)
                await foreach (var container in blobServiceClient.GetBlobContainersAsync())
                {
                    _logger.LogInformation("Successfully connected to Azure Blob Storage.");
                    break; // Just a quick check: exit after the first success
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Azure Blob Storage. Stopping further processing.");
                return;
            }

            // Only process .zip files
            if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("The file is not a zip archive. Exiting function.");
                return;
            }

            // Proceed with processing (as before)
            string connectionString2 = Environment.GetEnvironmentVariable("StorageConnection");
            BlobServiceClient blobServiceClient2 = new BlobServiceClient(connectionString2);
            BlobContainerClient outputContainerClient = blobServiceClient2.GetBlobContainerClient("output-container");
            await outputContainerClient.CreateIfNotExistsAsync();
            string outputFolderName = Path.GetFileNameWithoutExtension(name);

            try
            {
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    if (entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        string outputBlobName = $"{outputFolderName}/{entry.Name}";
                        BlobClient outputBlobClient = outputContainerClient.GetBlobClient(outputBlobName);
                        using var entryStream = entry.Open();
                        using var memoryStream = new MemoryStream();
                        await entryStream.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        await outputBlobClient.UploadAsync(memoryStream, overwrite: true);
                        _logger.LogInformation($"Extracted {entry.Name} to {outputBlobName}");
                    }
                    else
                    {
                        _logger.LogInformation($"Skipped non-CSV file {entry.Name} inside zip archive.");
                    }
                }
                _logger.LogInformation("Unzipping and uploading completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unzipping the file or uploading blobs.");
            }
        }
    }
}
