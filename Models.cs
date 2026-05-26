namespace BankQueueWinForms;

public enum ServiceType { Cash = 1, Account, Loan }
public enum Status      { Waiting, Called, Done, Skipped }

public static class Labels
{
    public static string Service(ServiceType t) => t switch {
        ServiceType.Account => "Account Services",
        ServiceType.Loan    => "Loan Consultation",
        _                   => "Cash Transaction"
    };
    public static string Counter(ServiceType t) => t switch {
        ServiceType.Account => "Counter B",
        ServiceType.Loan    => "Counter C",
        _                   => "Counter A"
    };
}

public class Customer
{
    public int         Id          { get; set; }
    public string      Name        { get; set; } = "";
    public string      ArrivalTime { get; set; } = "";
    public ServiceType Service     { get; set; }
    public Status      Status      { get; set; } = Status.Waiting;
    public long        CalledAt    { get; set; }
    public int         ServedMins  { get; set; }
}
