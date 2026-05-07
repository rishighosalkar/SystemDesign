using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlyweightDesignPattern
{
    public class Character : ICharacterFlyweight
    {
        private char symbol;

        public Character(char symbol)
        {
            this.symbol = symbol;
            Console.WriteLine("Creating Character: " + symbol);
        }

        public void Display(int x, int y)
        {
            Console.WriteLine(symbol + " at (" + x + "," + y + ")");
        }
    }
}
