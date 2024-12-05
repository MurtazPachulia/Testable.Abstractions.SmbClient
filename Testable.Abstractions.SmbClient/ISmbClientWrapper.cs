using Testable.Abstractions.SmbClient.Models;

namespace Testable.Abstractions.SmbClient;

public interface ISmbClientWrapper
{
    Task<List<string>> GetFileListFromShareAsync(SmbOptions options);
    Task<MemoryStream> DownloadFileFromShare(SmbOptions options, string fileName);
}
