namespace RHToolkit.Models;

public class AccountData
{
    public int AccountID { get; set; }
    public string? AccountName { get; set; }
    public string? Email { get; set; }
    public Guid AuthID { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime LastLogin { get; set; }
    public string? IsConnect { get; set; }
    public string? LastLoginIP { get; set; }
    public bool IsLocked { get; set; }
    public long Zen { get; set; }
    public int CashMileage { get; set; }
}
