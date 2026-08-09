using UCPFoodCorner.Models;
using Microsoft.AspNetCore.Mvc;

namespace UCPFoodCorner.Controllers;

public class LoginController : Controller
{
    private readonly FirstDBContext db;

    public LoginController(FirstDBContext context)
    {
        db = context;
    }

    [HttpGet]
    public IActionResult SignIn()
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SignIn(string email, string password)
    {
        var user = db.Users.FirstOrDefault(x => x.Email == email && x.Password == password);

        if (user == null)
        {
            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("UserName", user.Name);
        HttpContext.Session.SetString("UserRole", user.Role);

        if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Dashboard", "Admin");

        TempData["Welcome"] = $"Welcome {user.Name}! Login successful.";
        return RedirectToAction("Index", "Cafe");
    }

    [HttpGet]
    public IActionResult SignUp()
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SignUp(string name, string email, string password)
    {
        if (db.Users.Any(x => x.Email == email))
        {
            ViewBag.Error = "An account with this email already exists.";
            return View();
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || password.Length < 4)
        {
            ViewBag.Error = "Please enter valid details. Password must contain at least 4 characters.";
            return View();
        }

        var user = new User
        {
            Name = name.Trim(),
            Email = email.Trim(),
            Password = password,
            Role = "User",
            CreatedAt = DateTime.Now
        };

        db.Users.Add(user);
        db.SaveChanges();

        TempData["Success"] = "Account created successfully. Please sign in.";
        return RedirectToAction(nameof(SignIn));
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}