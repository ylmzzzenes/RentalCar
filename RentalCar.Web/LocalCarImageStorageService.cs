
using Org.BouncyCastle.Asn1.X509;

namespace RentalCar.Web
{
    public class LocalCarImageStorageService : ICarImageStorageService
    {
        private readonly IWebHostEnvironment _environment;

        public LocalCarImageStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }
        public async Task<IReadOnlyList<string>> SaveAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default)
        {
            var savedFileNames = new List<string>();

            if(files is null)
                 return savedFileNames;

            var uploadFolder = Path.Combine(_environment.WebRootPath, "Images", "Upload");

            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            foreach(var file in files)
            {
                if (file is null || file.Length == 0)
                    continue;

                var extension = Path.GetExtension(file.FileName);

                if (string.IsNullOrWhiteSpace(extension))
                    continue;

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!allowedExtensions.Contains(extension.ToLowerInvariant()))
                    continue;

                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadFolder, fileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream, cancellationToken);

                savedFileNames.Add(fileName);
            }

            return savedFileNames;
        }
    }
}
