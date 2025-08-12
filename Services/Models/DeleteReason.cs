
namespace TSysWatch;

public class DeleteReason
{
    public bool CanDelete { get; set; }
    public string Reason { get; set; } = string.Empty;
    public FileInfo FileInfo { get;   set; }
}