using EntityArchitect.CRUD.Enumerations;

namespace Paris2025.Enumerations;

public class FulfillmentStatus : Enumeration
{
    public FulfillmentStatus(int id) : base(id)
    {
    }

    public FulfillmentStatus(int id, string name) : base(id, name)
    {
    }
    
    //FULFILLED
    // IN_PROGRESS
    // PARTIALLY_FULFILLED
    // UNFULFILLED
    // 
    
    public static FulfillmentStatus Fulfilled = new FulfillmentStatus(1, "FULFILLED");
    public static FulfillmentStatus InProgress = new FulfillmentStatus(2, "IN_PROGRESS");
    public static FulfillmentStatus PartiallyFulfilled = new FulfillmentStatus(3, "PARTIALLY_FULFILLED");
    public static FulfillmentStatus Unfulfilled = new FulfillmentStatus(4, "UNFULFILLED");
}