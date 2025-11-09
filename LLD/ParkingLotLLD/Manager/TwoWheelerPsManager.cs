using System;
using ParkingLotLLD.Model;

namespace ParkingLotLLD.Manager;

public class TwoWheelerPsManager : ParkingSpotManager
{
    public static List<ParkingSpot> twoWheelerParkingSpots = new List<ParkingSpot>() {
        new ParkingSpot(),
        new ParkingSpot(),
        new ParkingSpot(),
        new ParkingSpot(),
        new ParkingSpot(),
        new ParkingSpot(),
    };

    public TwoWheelerPsManager() : base(twoWheelerParkingSpots)
    {
        
    }
}
