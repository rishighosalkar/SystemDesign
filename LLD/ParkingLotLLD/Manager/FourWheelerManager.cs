using System;
using ParkingLotLLD.Model;

namespace ParkingLotLLD.Manager;

public class FourWheelerManager : ParkingSpotManager
{
    public static List<ParkingSpot> fourWheelerParkingSpots = new List<ParkingSpot>() {
        new ParkingSpot(),
        new ParkingSpot(),
        new ParkingSpot(),
        new ParkingSpot(),
        new ParkingSpot(),
        new ParkingSpot(),
    };
    public FourWheelerManager() : base(fourWheelerParkingSpots)
    {
        
    }
}
