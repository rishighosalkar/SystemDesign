//Flyweight pattern saves memory by sharing common object data and passing unique data externally.

using FlyweightDesignPattern;

CharacterFactory characterFactory = new CharacterFactory();

ICharacterFlyweight char1 = characterFactory.GetCharacter('A');
ICharacterFlyweight char2 = characterFactory.GetCharacter('B');
ICharacterFlyweight char3 = characterFactory.GetCharacter('A');

char1.Display(10, 20);
char2.Display(11, 21);
char3.Display(12, 22);