namespace VendingIot.Helpers;

public interface IFileService
{
    Task<string> SaveFileAsync(IFormFile file, string folderName);
    void DeleteFile(string? fileName, string folderName);
}

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;

    public FileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    private string GetUploadsFolder(string folderName)
    {
        var root = !string.IsNullOrEmpty(_environment.WebRootPath)
            ? _environment.WebRootPath
            : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        var uploadsFolder = Path.Combine(root, "uploads", folderName);
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        return uploadsFolder;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string folderName)
    {
        var uploadsFolder = GetUploadsFolder(folderName);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return fileName;
    }

    public void DeleteFile(string? fileName, string folderName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return;

        var uploadsFolder = GetUploadsFolder(folderName);
        var filePath = Path.Combine(uploadsFolder, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}