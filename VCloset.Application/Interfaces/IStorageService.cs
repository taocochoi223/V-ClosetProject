using System.IO;
using System.Threading.Tasks;

namespace VCloset.Application.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = "uploads");
    Task DeleteFileAsync(string fileUrl);
}
