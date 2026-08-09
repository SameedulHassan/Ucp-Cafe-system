using UCPFoodCorner.Models;
using Microsoft.AspNetCore.Mvc;

namespace UCPFoodCorner.Controllers;

public class HomeController : Controller
{
    private readonly FirstDBContext db;

    public HomeController(FirstDBContext context)
    {
        db = context;
    }

    public IActionResult Index()
    {
        var deals = db.Deals.Where(x => x.IsActive).OrderByDescending(x => x.CreatedAt).ToList();
        ViewBag.DealItems = db.DealItems.ToList();
        ViewBag.CafeItems = db.CafeItems.ToDictionary(x => x.Id, x => x);
        return View(deals);
    }

    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
    }
}
