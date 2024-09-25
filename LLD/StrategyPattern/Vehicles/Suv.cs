using System;
using StrategyPattern.DriveStrategy;

namespace StrategyPattern.Vehicles;
class Suv : Vehicle{
    public Suv() : base(new RearWheelDrive()){
        
    }
}