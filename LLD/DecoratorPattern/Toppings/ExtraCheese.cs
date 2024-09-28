using System;
using DecoratorPattern.Pizza;

namespace DecoratorPattern.Toppings;

public class ExtraCheese : Toppings
{
    private readonly BasePizza _basePizza;
    public ExtraCheese(BasePizza basePizza)
    {
        _basePizza = basePizza;
    }
    public override int Cost()
    {
        return _basePizza.Cost() + 10;
    }
}
