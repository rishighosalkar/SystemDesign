// See https://aka.ms/new-console-template for more information

using DecoratorPattern.Pizza;
using DecoratorPattern.Toppings;

BasePizza basePizza = new ExtraCheese(new Margherita());

BasePizza basePizza1 = new Mushroom(basePizza);

Console.WriteLine("Final pizza prize is: {0}", basePizza.Cost());

Console.WriteLine("Final pizza prize with mushroom toppings: {0}", basePizza1.Cost());
