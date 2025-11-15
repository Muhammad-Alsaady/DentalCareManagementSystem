using Microsoft.AspNetCore.Mvc;

namespace DentalManagementSystem.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}