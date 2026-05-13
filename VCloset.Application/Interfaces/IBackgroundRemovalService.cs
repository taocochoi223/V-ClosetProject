using System.Threading.Tasks;

namespace VCloset.Application.Interfaces;

public interface IBackgroundRemovalService
{
    Task<byte[]> RemoveBackgroundAsync(byte[] imageBytes, string fileName = "image.png");
}
