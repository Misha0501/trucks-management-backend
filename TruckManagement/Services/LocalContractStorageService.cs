using TruckManagement.Interfaces;

namespace TruckManagement.Services
{
    /// <summary>
    /// Local filesystem implementation of contract storage service.
    /// Stores contract PDFs in a structured directory hierarchy:
    /// /storage/contracts/{year}/{month}/{driverId}/contract_v{version}_{timestamp}_{driverId}.pdf
    /// </summary>
    public class LocalContractStorageService : IContractStorageService
    {
        private readonly string _baseStoragePath;
        private readonly ILogger<LocalContractStorageService> _logger;

        public LocalContractStorageService(
            IConfiguration configuration,
            ILogger<LocalContractStorageService> logger)
        {
            _logger = logger;
            
            // Get base path from config or use default
            _baseStoragePath = configuration["ContractStorage:BasePath"] 
                ?? Path.Combine(Directory.GetCurrentDirectory(), "storage", "contracts");

            // Ensure base directory exists
            if (!Directory.Exists(_baseStoragePath))
            {
                Directory.CreateDirectory(_baseStoragePath);
                _logger.LogInformation("Created contract storage directory: {BasePath}", _baseStoragePath);
            }
        }

        /// <inheritdoc />
        public async Task<string> SaveContractPdfAsync(byte[] pdfBytes, Guid driverId, int versionNumber)
        {
            try
            {
                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    throw new ArgumentException("PDF bytes cannot be null or empty", nameof(pdfBytes));
                }

                if (versionNumber <= 0)
                {
                    throw new ArgumentException("Version number must be greater than 0", nameof(versionNumber));
                }

                var now = DateTime.UtcNow;
                
                // Build directory structure: {basePath}/{year}/{month}/{driverId}/
                var directoryPath = Path.Combine(
                    _baseStoragePath,
                    now.Year.ToString(),
                    now.Month.ToString("D2"),
                    driverId.ToString()
                );

                // Ensure directory exists
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    _logger.LogDebug("Created directory: {DirectoryPath}", directoryPath);
                }

                // Generate filename: contract_v{version}_{timestamp}_{driverId}.pdf
                var fileName = GetContractFileName(driverId, versionNumber);
                var filePath = Path.Combine(directoryPath, fileName);

                // Write file to disk
                await File.WriteAllBytesAsync(filePath, pdfBytes);

                _logger.LogInformation(
                    "Saved contract PDF: DriverId={DriverId}, Version={Version}, Size={Size} bytes, Path={Path}",
                    driverId, versionNumber, pdfBytes.Length, filePath);

                return filePath;
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                _logger.LogError(ex, 
                    "Failed to save contract PDF: DriverId={DriverId}, Version={Version}",
                    driverId, versionNumber);
                throw new IOException($"Failed to save contract PDF for driver {driverId}, version {versionNumber}", ex);
            }
        }

        /// <inheritdoc />
        public async Task<byte[]> GetContractPdfAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Contract PDF not found: {FilePath}", filePath);
                    throw new FileNotFoundException($"Contract PDF not found: {filePath}", filePath);
                }

                var pdfBytes = await File.ReadAllBytesAsync(filePath);

                _logger.LogDebug("Retrieved contract PDF: {FilePath}, Size={Size} bytes", filePath, pdfBytes.Length);

                return pdfBytes;
            }
            catch (Exception ex) when (ex is not ArgumentException and not FileNotFoundException)
            {
                _logger.LogError(ex, "Failed to read contract PDF: {FilePath}", filePath);
                throw new IOException($"Failed to read contract PDF: {filePath}", ex);
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteContractPdfAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Cannot delete contract PDF - file not found: {FilePath}", filePath);
                    return false;
                }

                // Use Task.Run to make the synchronous File.Delete async-compatible
                await Task.Run(() => File.Delete(filePath));

                _logger.LogWarning("Deleted contract PDF: {FilePath}", filePath);
                return true;
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                _logger.LogError(ex, "Failed to delete contract PDF: {FilePath}", filePath);
                throw new IOException($"Failed to delete contract PDF: {filePath}", ex);
            }
        }

        /// <inheritdoc />
        public Task<bool> FileExistsAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult(false);
            }

            var exists = File.Exists(filePath);
            return Task.FromResult(exists);
        }

        /// <inheritdoc />
        public string GetContractFileName(Guid driverId, int versionNumber)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            return $"contract_v{versionNumber}_{timestamp}_{driverId}.pdf";
        }

        /// <inheritdoc />
        public string GetStorageBasePath()
        {
            return _baseStoragePath;
        }
    }
}

