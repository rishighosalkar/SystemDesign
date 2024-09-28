using System;

namespace DecoratorPattern.Pizza;

public class Margherita : BasePizza
{
    public override int Cost()
    {
        return 150;
    }
}
