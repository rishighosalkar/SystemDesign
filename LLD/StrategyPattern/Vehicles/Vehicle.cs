using System;
using StrategyPattern.DriveStrategy;

namespace StrategyPattern.Vehicles;
class Vehicle{
    private IDriveTrain _driveTrain;

    public Vehicle(IDriveTrain driveTrain)
    {
        _driveTrain = driveTrain;
    }

    public void Drive(){
        _driveTrain.Drive();
    }
}