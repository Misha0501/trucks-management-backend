using TruckManagement.Helpers;

// Where ApiResponseFactory is defined

namespace TruckManagement.Endpoints
{
    public static class FileUploadsEndpoints
    {
        public static void MapFileUploadsEndpoints(this WebApplication app)
        {
            app.MapPost("/temporary-uploads", async (HttpRequest request, IWebHostEnvironment env, IConfiguration config) =>
            {
                if (!request.HasFormContentType)
                    return ApiResponseFactory.Error("Invalid content type. Expected multipart/form-data.");

                var form = await request.ReadFormAsync();
                var files = form.Files;

                if (files.Count == 0)
                    return ApiResponseFactory.Error("No files uploaded.");

                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".heic", ".pdf" };
                const long maxFileSize = 10 * 1024 * 1024; // 10 MB

                var temporaryBasePath = config["Storage:BasePath"] ?? "Storage";
                var tmpPath = Path.Combine(temporaryBasePath, "Tmp");
                var uploadDirectory = Path.Combine(env.ContentRootPath, tmpPath);
                Directory.CreateDirectory(uploadDirectory);

                var uploaded = new List<object>();

                foreach (var file in files)
                {
                    var fileExt = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExt))
                        return ApiResponseFactory.Error($"Unsupported file type: {fileExt}");

                    if (file.Length > maxFileSize)
                        return ApiResponseFactory.Error($"File '{file.FileName}' exceeds maximum size of 10 MB.");

                    var fileId = Guid.NewGuid();
                    var savedFileName = fileId + fileExt;
                    var savedPath = Path.Combine(uploadDirectory, savedFileName);

                    await using var stream = new FileStream(savedPath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    uploaded.Add(new
                    {
                        FileName = file.FileName,
                        FileId = fileId
                    });
                }

                return ApiResponseFactory.Success(uploaded);
            });

        }
    }
}