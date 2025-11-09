using System;

namespace ParkingLotLLD.Model;

public class Ticket
{
    public Guid Id;
    public DateTime EntryTime;
    public ParkingSpot? parkingSpot;
}
