In the Strategy Design Pattern, the goal is to encapsulate varying behavior (functionality) into separate classes so that they can be interchanged dynamically without modifying the main class.

Here’s a quick breakdown

Concept

. You have a main class (Context) that performs some operation.

2. The part of the operation that varies based on the situation is extracted into a separate interface (Strategy).

3. Different implementations of this interface represent different strategies (behaviors).

4. The main class “has a” Strategy and delegates the variable behavior to it.

Read more [here](LLD\StrategyPattern\Strategy_Design_Pattern_Explained.pdf)


1. we move out the functionality to another class (the strategy).
2. The main class depends on an interface, not on concrete implementations.