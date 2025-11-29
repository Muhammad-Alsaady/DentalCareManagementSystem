using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Application.DTOs;

namespace DentalManagementSystem.Controllers;

[Authorize]
public class PriceListController : Controller
{
    private readonly IPriceListService _priceListService;

    public PriceListController(IPriceListService priceListService)
    {
        _priceListService = priceListService;
    }

    /// <summary>
    /// Price List Index - Main page with grid
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        var priceListItems = _priceListService.GetAll().ToList();
        return View(priceListItems);
    }

    /// <summary>
    /// Get Price List Grid - AJAX partial view
    /// </summary>
    [HttpGet]
    public IActionResult GetPriceListGrid(string searchString = "")
    {
        var query = _priceListService.GetAll();

        // Apply search filter
        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(p => p.Name != null && 
                p.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase));
        }

        var items = query.OrderBy(p => p.Name).ToList();
        return PartialView("_PriceListGrid", items);
    }

    /// <summary>
    /// Create Price List Item - GET (returns partial for modal)
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        var model = new PriceListItemDto
        {
            Id = Guid.Empty,
            IsActive = true,
            DefaultPrice = 0
        };
        return PartialView("_CreateEdit", model);
    }

    /// <summary>
    /// Create Price List Item - POST (AJAX)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(PriceListItemDto model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                model.Id = Guid.NewGuid();
                model.IsActive = true;
                _priceListService.Create(model);
                
                return Json(new
                {
                    success = true,
                    message = "Service added successfully!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Return partial view with validation errors
        return PartialView("_CreateEdit", model);
    }

    /// <summary>
    /// Edit Price List Item - GET (returns partial for modal)
    /// </summary>
    [HttpGet]
    public IActionResult Edit(Guid id)
    {
        var item = _priceListService.GetById(id);
        if (item == null)
        {
            return NotFound();
        }
        return PartialView("_CreateEdit", item);
    }

    /// <summary>
    /// Edit Price List Item - POST (AJAX)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(PriceListItemDto model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                _priceListService.Update(model);
                
                return Json(new
                {
                    success = true,
                    message = "Service updated successfully!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Return partial view with validation errors
        return PartialView("_CreateEdit", model);
    }

    /// <summary>
    /// Delete Price List Item - GET (returns partial for modal)
    /// </summary>
    [HttpGet]
    public IActionResult Delete(Guid id)
    {
        var item = _priceListService.GetById(id);
        if (item == null)
        {
            return NotFound();
        }
        return PartialView("_Delete", item);
    }

    /// <summary>
    /// Delete Price List Item - POST (AJAX)
    /// </summary>
    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(Guid id)
    {
        try
        {
            _priceListService.Delete(id);
            
            return Json(new
            {
                success = true,
                message = "Service deleted successfully!"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get All Price List Items - GET (returns JSON for dropdowns)
    /// </summary>
    [HttpGet]
    public IActionResult GetAllItems()
    {
        var items = _priceListService.GetAll()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                id = p.Id,
                name = p.Name,
                price = p.DefaultPrice
            })
            .ToList();
        
        return Json(items);
    }
}
