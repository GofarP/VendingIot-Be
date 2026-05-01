public interface IFileService
{
    Task<string> SaveFileAsync(IFormFile file, string folderName);
    void DeleteFile(string fileName, string folderName);
}


public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;

    public FileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string folderName)
    {
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", folderName);
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return fileName;
    }

    public void DeleteFile(string fileName, string folderName)
    {
        var filePath = Path.Combine(_environment.WebRootPath, "uploads", folderName, fileName);
        if (File.Exists(filePath)) File.Delete(filePath);
    }

}