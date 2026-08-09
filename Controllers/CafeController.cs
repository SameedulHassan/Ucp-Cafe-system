using UCPFoodCorner.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace UCPFoodCorner.Controllers;

public class CafeController : Controller
{
    private readonly FirstDBContext db;

    public CafeController(FirstDBContext context)
    {
        db = context;
    }

    public IActionResult Index(string? search, string? category)
    {
        var items = db.CafeItems.Where(x => x.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(x => x.Name.Contains(search) || x.Description.Contains(search));
        if (!string.IsNullOrWhiteSpace(category))
            items = items.Where(x => x.Category == category);

        ViewBag.Categories = db.CafeItems.Where(x => x.IsActive).Select(x => x.Category).Distinct().OrderBy(x => x).ToList();
        ViewBag.Search = search;
        ViewBag.Category = category;
        ViewBag.UnavailableIds = db.ItemAvailabilities.Where(x => x.AvailableDate == DateTime.Today && !x.IsAvailable).Select(x => x.CafeItemId).ToHashSet();
        ViewBag.Reviews = db.Reviews.ToList();
        return View(items.OrderBy(x => x.Category).ThenBy(x => x.Name).ToList());
    }

    public IActionResult Details(int id)
    {
        var item = db.CafeItems.FirstOrDefault(x => x.Id == id);
        if (item == null) return NotFound();
        ViewBag.Reviews = db.Reviews.Where(x => x.CafeItemId == id).OrderByDescending(x => x.CreatedAt).ToList();
        ViewBag.Users = db.Users.ToDictionary(x => x.Id, x => x.Name);
        return View(item);
    }

    public IActionResult DealDetails(int id)
    {
        var deal = db.Deals.FirstOrDefault(x => x.Id == id && x.IsActive);
        if (deal == null) return NotFound();
        var components = (from di in db.DealItems
                           join ci in db.CafeItems on di.CafeItemId equals ci.Id
                           where di.DealId == id && ci.IsActive
                           select new DealComponent { Item = ci, Quantity = di.Quantity }).ToList();
        ViewBag.Components = components;
        ViewBag.RegularTotal = components.Sum(x => x.Item.Price * x.Quantity);
        ViewBag.Saving = Math.Max(0, (decimal)ViewBag.RegularTotal - deal.DealPrice);
        ViewBag.Unavailable = components.Any(x => db.ItemAvailabilities.Any(a => a.CafeItemId == x.Item.Id && a.AvailableDate == DateTime.Today && !a.IsAvailable));
        return View(deal);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult AddToCart(int id, int quantity = 1)
    {
        if (HttpContext.Session.GetInt32("UserId") == null) return RedirectToAction("SignIn", "Login");
        var item = db.CafeItems.FirstOrDefault(x => x.Id == id && x.IsActive);
        if (item == null) return NotFound();
        if (db.ItemAvailabilities.Any(x => x.CafeItemId == id && x.AvailableDate == DateTime.Today && !x.IsAvailable))
        { TempData["Error"] = "This item is unavailable today."; return RedirectToAction(nameof(Index)); }
        var cart = GetCart();
        var existing = cart.FirstOrDefault(x => !x.IsDeal && x.ItemId == id);
        if (existing == null) cart.Add(new CartItem { ItemId = item.Id, Name = item.Name, Price = item.Price, Quantity = Math.Max(1, quantity), ImagePath = item.ImagePath });
        else existing.Quantity += Math.Max(1, quantity);
        SaveCart(cart);
        TempData["Success"] = $"{item.Name} added to your cart.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult AddDealToCart(int id, int quantity = 1)
    {
        if (HttpContext.Session.GetInt32("UserId") == null) return RedirectToAction("SignIn", "Login");
        var deal = db.Deals.FirstOrDefault(x => x.Id == id && x.IsActive);
        if (deal == null) return NotFound();
        var components = GetDealComponents(id);
        if (!components.Any()) { TempData["Error"] = "This deal has no active menu items."; return RedirectToAction("Index", "Home"); }
        if (DealUnavailable(components)) { TempData["Error"] = "One or more items in this deal are unavailable today."; return RedirectToAction("Index", "Home"); }
        var cart = GetCart();
        var existing = cart.FirstOrDefault(x => x.IsDeal && x.DealId == id);
        if (existing == null) cart.Add(new CartItem { ItemId = -deal.Id, DealId = deal.Id, IsDeal = true, Name = deal.Name, Price = deal.DealPrice, Quantity = Math.Max(1, quantity), ImagePath = deal.ImagePath });
        else existing.Quantity += Math.Max(1, quantity);
        SaveCart(cart);
        TempData["Success"] = $"{deal.Name} added to your cart.";
        return RedirectToAction(nameof(Index), "Home");
    }

    public IActionResult Cart()
    {
        if (HttpContext.Session.GetInt32("UserId") == null) return RedirectToAction("SignIn", "Login");
        return View(GetCart());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult UpdateCart(int id, int quantity)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(x => x.ItemId == id);
        if (item != null) { if (quantity <= 0) cart.Remove(item); else item.Quantity = quantity; SaveCart(cart); }
        return RedirectToAction(nameof(Cart));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult RemoveFromCart(int id)
    {
        var cart = GetCart(); cart.RemoveAll(x => x.ItemId == id); SaveCart(cart); return RedirectToAction(nameof(Cart));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Checkout()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("SignIn", "Login");
        var cart = GetCart();
        if (!cart.Any()) { TempData["Error"] = "Your cart is empty."; return RedirectToAction(nameof(Cart)); }

        foreach (var cartItem in cart)
        {
            if (cartItem.IsDeal)
            {
                var components = GetDealComponents(cartItem.DealId);
                if (!components.Any() || DealUnavailable(components)) { TempData["Error"] = $"The deal '{cartItem.Name}' is no longer available."; return RedirectToAction(nameof(Cart)); }
            }
            else if (db.ItemAvailabilities.Any(a => a.CafeItemId == cartItem.ItemId && a.AvailableDate == DateTime.Today && !a.IsAvailable))
            { TempData["Error"] = $"'{cartItem.Name}' is no longer available today."; return RedirectToAction(nameof(Cart)); }
        }

        var order = new Order { UserId = userId.Value, Status = "Pending", TotalAmount = cart.Sum(x => x.Price * x.Quantity), OrderDate = DateTime.Now };
        db.Orders.Add(order); db.SaveChanges();

        foreach (var cartItem in cart)
        {
            if (!cartItem.IsDeal)
            {
                db.OrderItems.Add(new OrderItem { OrderId = order.Id, CafeItemId = cartItem.ItemId, Quantity = cartItem.Quantity, UnitPrice = cartItem.Price });
            }
            else
            {
                var components = GetDealComponents(cartItem.DealId);
                var regularTotal = components.Sum(x => x.Item.Price * x.Quantity);
                foreach (var component in components)
                {
                    var componentTotal = component.Item.Price * component.Quantity;
                    var allocated = regularTotal == 0 ? 0 : cartItem.Price * (componentTotal / regularTotal);
                    db.OrderItems.Add(new OrderItem { OrderId = order.Id, CafeItemId = component.Item.Id, Quantity = component.Quantity * cartItem.Quantity, UnitPrice = allocated / Math.Max(1, component.Quantity) });
                }
            }
        }
        db.SaveChanges(); SaveCart(new List<CartItem>());
        TempData["Success"] = $"Order #{order.Id} placed successfully.";
        return RedirectToAction(nameof(Orders));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult BuyNow(int id, int quantity = 1)
    {
        if (HttpContext.Session.GetInt32("UserId") == null) return RedirectToAction("SignIn", "Login");
        var item = db.CafeItems.FirstOrDefault(x => x.Id == id && x.IsActive);
        if (item == null) return NotFound();
        if (db.ItemAvailabilities.Any(x => x.CafeItemId == id && x.AvailableDate == DateTime.Today && !x.IsAvailable)) { TempData["Error"] = "This item is unavailable today."; return RedirectToAction(nameof(Index)); }
        quantity = Math.Max(1, quantity);
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var order = new Order { UserId = userId, Status = "Pending", TotalAmount = item.Price * quantity, OrderDate = DateTime.Now };
        db.Orders.Add(order); db.SaveChanges();
        db.OrderItems.Add(new OrderItem { OrderId = order.Id, CafeItemId = item.Id, Quantity = quantity, UnitPrice = item.Price });
        db.SaveChanges();
        TempData["Success"] = $"{item.Name} ordered successfully. Order #{order.Id}.";
        return RedirectToAction(nameof(Orders));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult BuyDealNow(int id, int quantity = 1)
    {
        if (HttpContext.Session.GetInt32("UserId") == null) return RedirectToAction("SignIn", "Login");
        var deal = db.Deals.FirstOrDefault(x => x.Id == id && x.IsActive);
        if (deal == null) return NotFound();
        var components = GetDealComponents(id);
        if (!components.Any()) { TempData["Error"] = "This deal has no active menu items."; return RedirectToAction("Index", "Home"); }
        if (DealUnavailable(components)) { TempData["Error"] = "One or more items in this deal are unavailable today."; return RedirectToAction("Index", "Home"); }
        quantity = Math.Max(1, quantity);
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var order = new Order { UserId = userId, Status = "Pending", TotalAmount = deal.DealPrice * quantity, OrderDate = DateTime.Now };
        db.Orders.Add(order); db.SaveChanges();
        var regularTotal = components.Sum(x => x.Item.Price * x.Quantity);
        foreach (var component in components)
        {
            var componentTotal = component.Item.Price * component.Quantity;
            var allocated = regularTotal == 0 ? 0 : deal.DealPrice * (componentTotal / regularTotal);
            db.OrderItems.Add(new OrderItem { OrderId = order.Id, CafeItemId = component.Item.Id, Quantity = component.Quantity * quantity, UnitPrice = allocated / Math.Max(1, component.Quantity) });
        }
        db.SaveChanges();
        TempData["Success"] = $"{deal.Name} ordered successfully for Rs. {(deal.DealPrice * quantity):N0}. Order #{order.Id}.";
        return RedirectToAction(nameof(Orders));
    }

    public IActionResult Orders()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("SignIn", "Login");
        var orders = db.Orders.Where(x => x.UserId == userId).OrderByDescending(x => x.OrderDate).ToList();
        ViewBag.OrderItems = db.OrderItems.ToList(); ViewBag.Items = db.CafeItems.ToDictionary(x => x.Id, x => x.Name);
        return View(orders);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Review(int itemId, int rating, string comment)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("SignIn", "Login");
        if (rating < 1 || rating > 5) { TempData["Error"] = "Rating must be between 1 and 5."; return RedirectToAction(nameof(Details), new { id = itemId }); }
        db.Reviews.Add(new Review { CafeItemId = itemId, UserId = userId.Value, Rating = rating, Comment = comment ?? "", CreatedAt = DateTime.Now });
        db.SaveChanges(); TempData["Success"] = "Thank you for your review!"; return RedirectToAction(nameof(Details), new { id = itemId });
    }

    private List<DealComponent> GetDealComponents(int dealId)
    {
        return (from di in db.DealItems join ci in db.CafeItems on di.CafeItemId equals ci.Id where di.DealId == dealId && ci.IsActive select new DealComponent { Item = ci, Quantity = di.Quantity }).ToList();
    }

    private bool DealUnavailable(List<DealComponent> components)
    {
        return components.Any(x => db.ItemAvailabilities.Any(a => a.CafeItemId == x.Item.Id && a.AvailableDate == DateTime.Today && !a.IsAvailable));
    }

    private List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString("Cart");
        return string.IsNullOrEmpty(json) ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
    }

    private void SaveCart(List<CartItem> cart) => HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
}

