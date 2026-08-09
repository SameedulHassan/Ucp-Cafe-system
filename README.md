# UCP Food Corner - Cafe Management System

ASP.NET Core MVC (.NET 10) + EF Core 10 + SQL Server LocalDB cafe management system.

## Connection

```text
Add your connection string here
```

The app initializes missing cafe tables on startup. It preserves an existing `Users` table and adds `Role` and `CreatedAt` when needed.

## Run

1. Open `UCPFoodCorner.sln` in Visual Studio Community.
2. Ensure SQL Server LocalDB is installed and `FirstDB` is available.
3. Build with Ctrl+Shift+B.
4. Run with Ctrl+F5.

## Main flow

- **Home:** today's deals only.
- **Menu:** complete menu with search, category filtering, availability, reviews, item details and cart actions.
- **Logged-in user:** sees their name in the navigation and a welcome hero instead of registration prompts.
- **Login/Sign-up:** includes a cancel/explore option; already logged-in users are redirected home.
- **Admin:** users, menu, daily availability, reviews, orders and multi-item deals.

## Admin

Set an existing user to admin in SQL Server:

```sql
USE FirstDB;
UPDATE Users SET Role = 'Admin' WHERE Email = 'sameed@gmail.com';
```

The admin can create a deal by selecting multiple menu items, choosing quantities, setting a deal price, uploading a deal image and publishing it to the customer home page.

## Uploaded images

Menu and deal images are stored under:

`wwwroot/uploads/items`

The supplied UCP Food Corner image is used as the main site background under:

`wwwroot/images/ucp-food-corner.jfif`

## Note

This project uses the simple username/password structure from the original educational project. For production use, replace plain-text passwords with ASP.NET Core Identity/password hashing.
