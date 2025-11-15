using EntityArchitect.CRUD.Enumerations;

namespace Paris2025.Enumerations;

public class Currency : Enumeration
{
    public Currency(int id) : base(id)
    {
    }

    public Currency(int id, string name) : base(id, name)
    {
    }
    
    public static Currency USD = new Currency(1, "USD");
    public static Currency EUR = new Currency(2, "EUR");
    public static Currency GBP = new Currency(3, "GBP");
    public static Currency PLN = new Currency(4, "PLN");
}