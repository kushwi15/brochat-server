namespace BroChat.Application.Interfaces;

public interface IFileService
{
    Task<string?> UploadFileAsync(Stream stream, string fileName, string contentType);
}
