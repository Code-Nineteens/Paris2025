using EntityArchitect.CRUD.Enumerations;

namespace Paris2025.Enumerations;

public class FinancialStatus : Enumeration
{
    public FinancialStatus(int id) : base(id)
    {
    }

    public FinancialStatus(int id, string name) : base(id, name)
    {
    }
    
    public static FinancialStatus Expired = new FinancialStatus(1, "EXPIRED");
    public static FinancialStatus Paid = new FinancialStatus(2, "PAID");
    public static FinancialStatus PartiallyPaid = new FinancialStatus(3, "PARTIALLY_PAID");
    public static FinancialStatus PartiallyRefunded = new FinancialStatus(4, "PARTIALLY_REFUNDED");
    public static FinancialStatus Pending = new FinancialStatus(5, "PENDING");
    public static FinancialStatus Refunded = new FinancialStatus(6, "REFUNDED");
    public static FinancialStatus Voided = new FinancialStatus(7, "VOIDED");
}