# Food Cart - Low Level Design Notes

## 1. Problem Statement
Design a food cart system where users can browse a menu, add items to a cart, place orders, and track order status.

## 2. Requirements
**Functional:**
- Admin can add menu items and toggle availability
- Users can browse available menu items
- Users can add/remove items to/from their cart
- Users can checkout (cart → order)
- Order status can be updated through a defined lifecycle

**Non-Functional:**
- Thread-safe in-memory storage (ConcurrentDictionary)
- Clean separation of concerns (Controller → Service → Model)

## 3. Entities

| Entity   | Key Fields                                      |
|----------|------------------------------------------------|
| MenuItem | Id, Name, Price, Category, IsAvailable          |
| Cart     | Id, UserId, Items[], TotalAmount (computed)     |
| CartItem | MenuItemId, ItemName, Price, Quantity, Total     |
| Order    | Id, UserId, Items[], TotalAmount, Status, CreatedAt |

## 4. Order Status State Machine
```
Placed ──→ Preparing ──→ Ready ──→ Delivered
  │
  └──→ Cancelled
```
- Only valid transitions are allowed (enforced in OrderService)
- Once Cancelled or Delivered, no further transitions

## 5. Class Diagram (Simplified)
```
MenuController ──→ MenuService ──→ ConcurrentDictionary<Guid, MenuItem>
CartController ──→ CartService ──→ ConcurrentDictionary<Guid, Cart>
                        │
                        └──→ MenuService (validates item exists & available)
OrderController ──→ OrderService ──→ ConcurrentDictionary<Guid, Order>
                        │
                        └──→ CartService (reads cart, clears after checkout)
```

## 6. API Endpoints

| Method | Endpoint                          | Description              |
|--------|-----------------------------------|--------------------------|
| GET    | /api/menu                         | List available items     |
| POST   | /api/menu                         | Add a menu item          |
| PATCH  | /api/menu/{id}/toggle             | Toggle item availability |
| GET    | /api/cart/{userId}                | View user's cart         |
| POST   | /api/cart/{userId}/items          | Add item to cart         |
| DELETE | /api/cart/{userId}/items/{itemId} | Remove item from cart    |
| POST   | /api/order/{userId}/checkout      | Place order from cart    |
| GET    | /api/order/{orderId}              | Get order details        |
| GET    | /api/order/user/{userId}          | Get user's orders        |
| PATCH  | /api/order/{orderId}/status       | Update order status      |

## 7. Key Design Patterns Used

### Service Layer Pattern
- Controllers are thin — they only handle HTTP concerns
- Business logic lives in services (MenuService, CartService, OrderService)

### State Machine (Order Status)
- Transitions defined as a dictionary: `Dictionary<OrderStatus, OrderStatus[]>`
- Prevents invalid transitions (e.g., Ready → Placed)

### Dependency Injection
- Services registered as Singletons (in-memory store acts as DB)
- CartService depends on MenuService, OrderService depends on CartService

### Computed Properties
- `Cart.TotalAmount` and `CartItem.Total` are computed (`=>`) — no stale data

## 8. What Could Be Extended
- **Payment:** Add a PaymentService with strategy pattern (Cash, Card, UPI)
- **Observer Pattern:** Notify kitchen when order is placed, notify user on status change
- **Database:** Replace ConcurrentDictionary with EF Core + SQL
- **Authentication:** Add JWT-based user auth
- **Rate Limiting:** Prevent abuse on checkout endpoint
- **Inventory:** Track stock per menu item, decrement on order

## 9. Interview Tips
1. Start by clarifying requirements (who are the actors? what are the core flows?)
2. Draw the class diagram first, then code
3. Show awareness of thread safety (ConcurrentDictionary)
4. Demonstrate state machine thinking for status transitions
5. Keep controllers thin, services fat
6. Use enums for fixed sets of values (OrderStatus, Category)
7. Mention what you'd add with more time (payments, notifications, DB)
