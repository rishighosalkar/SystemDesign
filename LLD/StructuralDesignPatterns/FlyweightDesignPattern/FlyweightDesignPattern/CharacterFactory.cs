using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlyweightDesignPattern
{
    public class CharacterFactory
    {
        private Dictionary<char, ICharacterFlyweight> _characterFlyweightMap;

        public ICharacterFlyweight GetCharacter(char symbol)
        {
            if(!_characterFlyweightMap.ContainsKey(symbol))
            {
                _characterFlyweightMap.Add(symbol, new Character(symbol));
            }

            return _characterFlyweightMap[symbol];
        }
    }
}
