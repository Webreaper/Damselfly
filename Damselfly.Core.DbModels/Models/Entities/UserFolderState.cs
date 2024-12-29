namespace Damselfly.Core.Models;

public class UserFolderState
{
    public int FolderId { get; set; }
    public int UserId { get; set; }
    public bool Expanded { get; set; } = true; // Always default folders to 'expanded'
}