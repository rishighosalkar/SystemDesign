using StateDesignPattern.Context;

Console.WriteLine("=== Happy Path ===");
var order = new OrderProcessing();
order.NextState();  // Created -> Paid
order.NextState();  // Paid -> Shipped
order.NextState();  // Shipped -> Delivered
order.NextState();  // Already delivered

Console.WriteLine("\n=== Cancel Path ===");
var order2 = new OrderProcessing();
order2.NextState();    // Created -> Paid
order2.CancelOrder();  // Paid -> Cancelled
order2.NextState();    // Cannot proceed
