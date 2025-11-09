using System;

namespace ParkingLotLLD.Model;

public class ParkingSpot
{
    public Guid Id;
    public bool IsEmpty;
    Vehicle? vehicle;
    public int Price;
}
