using SMBLibrary;
using SMBLibrary.Client;
using Testable.Abstractions.SmbClient.Models;

namespace Testable.Abstractions.SmbClient;

public class SmbClientWrapper : ISmbClientWrapper
{
    public async Task<List<string>> GetFileListFromShareAsync(SmbOptions options)
    {
        var client = new SMB2Client();
        var isConnected = client.Connect(options.Host, SMBTransportType.DirectTCPTransport);
        if (!isConnected)
        {
            throw new ApplicationException("SMB Connect error");
        }

        var status = client.Login(string.Empty, options.Username, options.Password);
        if (status != NTStatus.STATUS_SUCCESS)
        {
            client.Disconnect();
            throw new ApplicationException("SMB Login error");
        }

        var fileShare = client.TreeConnect(options.ShareName, out _)
            ?? throw new ApplicationException("SMB TreeConnect error");

        status = fileShare.CreateFile(
            out var directoryHandle,
            out var fileStatus,
            options.FolderPath,
            AccessMask.GENERIC_READ,
            SMBLibrary.FileAttributes.Directory,
            ShareAccess.Read | ShareAccess.Write,
            CreateDisposition.FILE_OPEN,
            CreateOptions.FILE_DIRECTORY_FILE,
            null);

        if (status == NTStatus.STATUS_SUCCESS)
        {
            status = fileShare.QueryDirectory(out var fileList,
                directoryHandle,
                "*",
                FileInformationClass.FileDirectoryInformation);

            status = fileShare.CloseFile(directoryHandle);

            fileShare.Disconnect();
            client.Logoff();
            client.Disconnect();

            return await Task.Run(() =>
                fileList.Select(x => ((FileDirectoryInformation)x).FileName).ToList()).ConfigureAwait(false);
        }
        else
        {
            throw new ApplicationException("SMB get files error");
        }
    }

    public async Task<MemoryStream> DownloadFileFromShare(SmbOptions options, string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

        var client = new SMB2Client();
        var isConnected = client.Connect(options.Host, SMBTransportType.DirectTCPTransport);
        if (!isConnected)
        {
            throw new ApplicationException("SMB Connect error");
        }

        var status = client.Login(string.Empty, options.Username, options.Password);
        if (status != NTStatus.STATUS_SUCCESS)
        {
            client.Disconnect();
            throw new ApplicationException("SMB Login error");
        }

        var fileShare = client.TreeConnect(options.ShareName, out _)
            ?? throw new ApplicationException("SMB TreeConnect error");

        var remoteFilePath = $"{options.FolderPath}{fileName}";

        status = fileShare.CreateFile(out var fileHandle,
            out _,
            remoteFilePath,
            AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE,
            SMBLibrary.FileAttributes.Normal,
            ShareAccess.Read,
            CreateDisposition.FILE_OPEN,
            CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT,
            null);
        if (status != NTStatus.STATUS_SUCCESS)
        {
            client?.Logoff();
            client?.Disconnect();
            throw new ApplicationException("SMB CreateFile error");
        }

        var stream = new MemoryStream();
        long bytesRead = 0;
        while (true)
        {
            status = fileShare.ReadFile(out var data, fileHandle, bytesRead, (int)client.MaxReadSize);

            if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_END_OF_FILE)
            {
                fileShare.CloseFile(fileHandle);
                fileShare.Disconnect();
                client?.Logoff();
                client?.Disconnect();

                throw new ApplicationException("SMB Failed to read from file");
            }

            if(status == NTStatus.STATUS_END_OF_FILE || data.Length == 0) break;
            bytesRead += data.Length;

            await stream.WriteAsync(data).ConfigureAwait(false);
        }

        fileShare.CloseFile(fileHandle);
        fileShare.Disconnect();
        client?.Logoff();
        client?.Disconnect();

        return stream;
    }
}
