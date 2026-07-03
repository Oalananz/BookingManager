namespace BookingManager.Api.Models;

/// <summary>
/// Persisted values are Active and Cancelled only. Completed is derived at read
/// time (an Active booking whose EndDateTime is in the past) and never stored,
/// so no background job is needed and the DB exclusion constraint stays simple.
/// </summary>
public enum BookingStatus
{
    Active,
    Cancelled,
    Completed
}
