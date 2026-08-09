using UCPFoodCorner.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UCPFoodCorner.Controllers;

public class AdminController : Controller
{
    private readonly FirstDBContext db;
    private readonly IWebHostEnvironment env;

    public AdminController(FirstDBContext context, IWebHostEnvironment environment)
    {
        db = context;
        env = environment;
    }

    private bool IsAdmin()
    {
        return string.Equals(HttpContext.Session.GetString("UserRole"), "Admin", StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult Denied()
    {
        return RedirectToAction("SignIn", "Login");
    }

    public IActionResult Dashboard()
    {
        if (!IsAdmin()) return Denied();

        ViewBag.Users = db.Users.Count();
        ViewBag.Items = db.CafeItems.Count();
        ViewBag.Orders = db.Orders.Count();
        ViewBag.PendingOrders = db.Orders.Count(x => x.Status == "Pending");
        ViewBag.Sales = db.Orders.Where(x => x.Status != "Cancelled").Select(x => (decimal?)x.TotalAmount).Sum() ?? 0;
        return View();
    }

    public IActionResult Users()
    {
        if (!IsAdmin()) return Denied();
        return View(db.Users.OrderByDescending(x => x.Id).ToList());
    }

    [HttpGet]
    public IActionResult EditUser(int id)
    {
        if (!IsAdmin()) return Denied();
        var user = db.Users.Find(id);
        return user == null ? NotFound() : View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditUser(User model)
    {
        if (!IsAdmin()) return Denied();

        var user = db.Users.Find(model.Id);
        if (user == null) return NotFound();

        user.Name = model.Name;
        user.Email = model.Email;
        user.Role = model.Role == "Admin" ? "Admin" : "User";

        if (!string.IsNullOrWhiteSpace(model.Password))
            user.Password = model.Password;

        db.SaveChanges();
        TempData["Success"] = "User details updated.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteUser(int id)
    {
        if (!IsAdmin()) return Denied();

        var currentId = HttpContext.Session.GetInt32("UserId");
        if (currentId == id)
        {
            TempData["Error"] = "You cannot delete the account you are currently using.";
            return RedirectToAction(nameof(Users));
        }

        var user = db.Users.Find(id);
        if (user != null)
        {
            db.Users.Remove(user);
            db.SaveChanges();
        }

        TempData["Success"] = "User deleted.";
        return RedirectToAction(nameof(Users));
    }

    public IActionResult Items()
    {
        if (!IsAdmin()) return Denied();
        return View(db.CafeItems.OrderByDescending(x => x.Id).ToList());
    }

    [HttpGet]
    public IActionResult CreateItem()
    {
        if (!IsAdmin()) return Denied();
        return View(new CafeItem());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateItem(CafeItem model, IFormFile? image)
    {
        if (!IsAdmin()) return Denied();

        if (image != null)
            model.ImagePath = await SaveImage(image);

        model.CreatedAt = DateTime.Now;
        db.CafeItems.Add(model);
        db.SaveChanges();

        TempData["Success"] = "New cafe item added.";
        return RedirectToAction(nameof(Items));
    }

    [HttpGet]
    public IActionResult EditItem(int id)
    {
        if (!IsAdmin()) return Denied();
        var item = db.CafeItems.Find(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditItem(CafeItem model, IFormFile? image)
    {
        if (!IsAdmin()) return Denied();

        var item = db.CafeItems.Find(model.Id);
        if (item == null) return NotFound();

        item.Name = model.Name;
        item.Description = model.Description;
        item.Category = model.Category;
        item.Price = model.Price;
        item.IsActive = model.IsActive;

        if (image != null)
            item.ImagePath = await SaveImage(image);

        db.SaveChanges();
        TempData["Success"] = "Cafe item updated.";
        return RedirectToAction(nameof(Items));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteItem(int id)
    {
        if (!IsAdmin()) return Denied();

        var item = db.CafeItems.Find(id);
        if (item != null)
        {
            db.DealItems.RemoveRange(db.DealItems.Where(x => x.CafeItemId == id));
            db.CafeItems.Remove(item);
            db.SaveChanges();
        }

        TempData["Success"] = "Cafe item deleted.";
        return RedirectToAction(nameof(Items));
    }

    [HttpGet]
    public IActionResult Availability(int id, DateTime? date)
    {
        if (!IsAdmin()) return Denied();

        var item = db.CafeItems.Find(id);
        if (item == null) return NotFound();

        var selected = date?.Date ?? DateTime.Today;
        var record = db.ItemAvailabilities.FirstOrDefault(x =>
            x.CafeItemId == id && x.AvailableDate == selected);

        ViewBag.Item = item;
        ViewBag.Date = selected;
        ViewBag.Current = record?.IsAvailable ?? true;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Availability(int id, DateTime date, bool isAvailable)
    {
        if (!IsAdmin()) return Denied();

        var record = db.ItemAvailabilities.FirstOrDefault(x =>
            x.CafeItemId == id && x.AvailableDate == date.Date);

        if (record == null)
        {
            db.ItemAvailabilities.Add(new ItemAvailability
            {
                CafeItemId = id,
                AvailableDate = date.Date,
                IsAvailable = isAvailable
            });
        }
        else
        {
            record.IsAvailable = isAvailable;
        }

        db.SaveChanges();
        TempData["Success"] = $"Availability updated for {date:dd MMM yyyy}.";
        return RedirectToAction(nameof(Items));
    }

    public IActionResult Deals()
    {
        if (!IsAdmin()) return Denied();

        var deals = db.Deals.OrderByDescending(x => x.Id).ToList();
        ViewBag.DealItems = db.DealItems.ToList();
        ViewBag.CafeItems = db.CafeItems.ToDictionary(x => x.Id, x => x);
        return View(deals);
    }

    [HttpGet]
    public IActionResult CreateDeal()
    {
        if (!IsAdmin()) return Denied();
        ViewBag.Items = db.CafeItems.Where(x => x.IsActive).OrderBy(x => x.Category).ThenBy(x => x.Name).ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDeal(string name, string description, decimal dealPrice, bool isActive, List<int>? itemIds, Dictionary<int, int>? quantities, IFormFile? image)
    {
        if (!IsAdmin()) return Denied();

        if (string.IsNullOrWhiteSpace(name) || dealPrice <= 0 || itemIds == null || !itemIds.Any())
        {
            ViewBag.Error = "Enter a deal name, a valid deal price, and select at least one menu item.";
            ViewBag.Items = db.CafeItems.Where(x => x.IsActive).OrderBy(x => x.Category).ThenBy(x => x.Name).ToList();
            return View();
        }

        var deal = new Deal
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? "",
            DealPrice = dealPrice,
            IsActive = isActive,
            CreatedAt = DateTime.Now
        };

        if (image != null)
            deal.ImagePath = await SaveImage(image);

        db.Deals.Add(deal);
        db.SaveChanges();

        foreach (var itemId in itemIds.Distinct())
        {
            var item = db.CafeItems.Find(itemId);
            if (item == null) continue;

            var qty = quantities != null && quantities.TryGetValue(itemId, out var requested)
                ? Math.Max(1, requested)
                : 1;

            db.DealItems.Add(new DealItem
            {
                DealId = deal.Id,
                CafeItemId = itemId,
                Quantity = qty
            });
        }

        db.SaveChanges();
        TempData["Success"] = "Deal created successfully.";
        return RedirectToAction(nameof(Deals));
    }

    [HttpGet]
    public IActionResult EditDeal(int id)
    {
        if (!IsAdmin()) return Denied();
        var deal = db.Deals.Find(id);
        if (deal == null) return NotFound();
        ViewBag.Items = db.CafeItems.Where(x => x.IsActive).OrderBy(x => x.Category).ThenBy(x => x.Name).ToList();
        ViewBag.Selected = db.DealItems.Where(x => x.DealId == id).ToDictionary(x => x.CafeItemId, x => x.Quantity);
        return View(deal);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDeal(int id, string name, string description, decimal dealPrice, bool isActive, List<int>? itemIds, Dictionary<int, int>? quantities, IFormFile? image)
    {
        if (!IsAdmin()) return Denied();
        var deal = db.Deals.Find(id);
        if (deal == null) return NotFound();
        if (string.IsNullOrWhiteSpace(name) || dealPrice <= 0 || itemIds == null || !itemIds.Any())
        {
            ViewBag.Error = "Enter a deal name, valid price and select at least one item.";
            ViewBag.Items = db.CafeItems.Where(x => x.IsActive).OrderBy(x => x.Category).ThenBy(x => x.Name).ToList();
            ViewBag.Selected = itemIds?.Distinct().ToDictionary(x => x, x => quantities != null && quantities.TryGetValue(x, out var q) ? Math.Max(1, q) : 1) ?? new Dictionary<int, int>();
            return View(deal);
        }
        deal.Name = name.Trim(); deal.Description = description?.Trim() ?? ""; deal.DealPrice = dealPrice; deal.IsActive = isActive;
        if (image != null) deal.ImagePath = await SaveImage(image);
        db.DealItems.RemoveRange(db.DealItems.Where(x => x.DealId == id));
        foreach (var itemId in itemIds.Distinct())
        {
            if (db.CafeItems.Find(itemId) == null) continue;
            var qty = quantities != null && quantities.TryGetValue(itemId, out var requested) ? Math.Max(1, requested) : 1;
            db.DealItems.Add(new DealItem { DealId = id, CafeItemId = itemId, Quantity = qty });
        }
        db.SaveChanges(); TempData["Success"] = "Deal updated successfully."; return RedirectToAction(nameof(Deals));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteDeal(int id)
    {
        if (!IsAdmin()) return Denied();

        var deal = db.Deals.Find(id);
        if (deal != null)
        {
            db.Deals.Remove(deal);
            db.SaveChanges();
        }

        TempData["Success"] = "Deal deleted.";
        return RedirectToAction(nameof(Deals));
    }

    public IActionResult Reviews()
    {
        if (!IsAdmin()) return Denied();

        var reviews = db.Reviews.OrderByDescending(x => x.CreatedAt).ToList();
        ViewBag.Items = db.CafeItems.ToDictionary(x => x.Id, x => x.Name);
        ViewBag.Users = db.Users.ToDictionary(x => x.Id, x => x.Name);
        return View(reviews);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteReview(int id)
    {
        if (!IsAdmin()) return Denied();

        var review = db.Reviews.Find(id);
        if (review != null)
        {
            db.Reviews.Remove(review);
            db.SaveChanges();
        }

        return RedirectToAction(nameof(Reviews));
    }

    public IActionResult Orders()
    {
        if (!IsAdmin()) return Denied();

        var orders = db.Orders.OrderByDescending(x => x.OrderDate).ToList();
        ViewBag.Users = db.Users.ToDictionary(x => x.Id, x => x.Name);
        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateOrderStatus(int id, string status)
    {
        if (!IsAdmin()) return Denied();

        var allowed = new[] { "Pending", "Preparing", "Ready", "Completed", "Cancelled" };
        if (!allowed.Contains(status)) return BadRequest();

        var order = db.Orders.Find(id);
        if (order == null) return NotFound();

        order.Status = status;
        db.SaveChanges();

        return RedirectToAction(nameof(Orders));
    }

    private async Task<string> SaveImage(IFormFile image)
    {
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".jfif" };
        var ext = Path.GetExtension(image.FileName).ToLowerInvariant();

        if (!allowed.Contains(ext))
            throw new InvalidOperationException("Only JPG, JPEG, PNG, WEBP and JFIF images are allowed.");

        if (image.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Image must be 5 MB or smaller.");

        var folder = Path.Combine(env.WebRootPath, "uploads", "items");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(folder, fileName);

        await using var stream = new FileStream(path, FileMode.Create);
        await image.CopyToAsync(stream);

        return $"/uploads/items/{fileName}";
    }
}