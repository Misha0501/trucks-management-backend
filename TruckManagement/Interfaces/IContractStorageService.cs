namespace TruckManagement.Interfaces
{
    /// <summary>
    /// Service for storing and retrieving driver contract PDF files.
    /// Abstracts file storage operations to allow for different implementations
    /// (local filesystem, Azure Blob Storage, AWS S3, etc.).
    /// </summary>
    public interface IContractStorageService
    {
        /// <summary>
        /// Saves a contract PDF to storage.
        /// </summary>
        /// <param name="pdfBytes">The PDF file content as byte array</param>
        /// <param name="driverId">The driver this contract belongs to</param>
        /// <param name="versionNumber">The version number of this contract</param>
        /// <returns>The full file path where the PDF was saved</returns>
        /// <exception cref="IOException">Thrown when file save operation fails</exception>
        Task<string> SaveContractPdfAsync(byte[] pdfBytes, Guid driverId, int versionNumber);

        /// <summary>
        /// Retrieves a contract PDF from storage.
        /// </summary>
        /// <param name="filePath">The full path to the PDF file</param>
        /// <returns>The PDF file content as byte array</returns>
        /// <exception cref="FileNotFoundException">Thrown when file doesn't exist</exception>
        /// <exception cref="IOException">Thrown when file read operation fails</exception>
        Task<byte[]> GetContractPdfAsync(string filePath);

        /// <summary>
        /// Deletes a contract PDF from storage.
        /// Use with caution - contract PDFs should generally be preserved for audit trail.
        /// </summary>
        /// <param name="filePath">The full path to the PDF file</param>
        /// <returns>True if file was deleted successfully, false if file didn't exist</returns>
        /// <exception cref="IOException">Thrown when file delete operation fails</exception>
        Task<bool> DeleteContractPdfAsync(string filePath);

        /// <summary>
        /// Checks if a contract PDF exists in storage.
        /// </summary>
        /// <param name="filePath">The full path to the PDF file</param>
        /// <returns>True if file exists, false otherwise</returns>
        Task<bool> FileExistsAsync(string filePath);

        /// <summary>
        /// Gets the filename for a contract PDF based on driver ID and version number.
        /// Format: contract_v{versionNumber}_{yyyyMMdd_HHmmss}_{driverId}.pdf
        /// </summary>
        /// <param name="driverId">The driver ID</param>
        /// <param name="versionNumber">The version number</param>
        /// <returns>The generated filename</returns>
        string GetContractFileName(Guid driverId, int versionNumber);

        /// <summary>
        /// Gets the base storage path for contracts.
        /// Default: /storage/contracts/
        /// Can be configured via environment variable CONTRACTS_STORAGE_PATH.
        /// </summary>
        /// <returns>The base storage path</returns>
        string GetStorageBasePath();
    }
}

