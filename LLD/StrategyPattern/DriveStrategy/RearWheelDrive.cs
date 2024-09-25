using System;

namespace StrategyPattern.DriveStrategy;

class RearWheelDrive : IDriveTrain{
    public void Drive(){
        Console.WriteLine("RearWheelDrive");
    }
}