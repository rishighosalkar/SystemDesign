using System;
using ParkingLotLLD.Model;

namespace ParkingLotLLD.Manager;

public class ParkingSpotManager
{
    private List<ParkingSpot> parkingSpots;

    public ParkingSpotManager(List<ParkingSpot> parkingSpots)
    {
        this.parkingSpots = parkingSpots;
    }

    public ParkingSpot FindParkingSpace() {
        return parkingSpots.FirstOrDefault(x => !x.IsEmpty);
    }
}
