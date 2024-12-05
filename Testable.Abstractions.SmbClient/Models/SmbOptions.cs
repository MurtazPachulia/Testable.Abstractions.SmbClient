namespace Testable.Abstractions.SmbClient.Models;

public class SmbOptions
{
    public string Host { get; set; } = string.Empty;
    public string ShareName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
