using System;
using DecoratorPattern.Pizza;

namespace DecoratorPattern.Toppings;

public class Mushroom : Toppings
{
    private readonly BasePizza _basePizza;
    public Mushroom(BasePizza basePizza)
    {
        _basePizza = basePizza;
    }
    public override int Cost()
    {
        return _basePizza.Cost() + 30;
    }
}
