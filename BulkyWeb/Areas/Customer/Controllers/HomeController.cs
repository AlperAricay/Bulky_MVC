using System.Diagnostics;
using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Customer.Controllers;

[Area("Customer")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var claimsIdentity = (ClaimsIdentity) User.Identity;
        var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

        if (claim != null)
        {
            HttpContext.Session.SetInt32(SD.SessionCart, 
                _unitOfWork.ShoppingCartRepo.GetAll(u => u.ApplicationUserId == claim.Value).Count());
        }
        
        var productList = _unitOfWork.ProductRepo.GetAll(includeProperties: "Category");
        return View(productList);
    }

    public IActionResult Details(int productId)
    {
        var cart = new ShoppingCart
        {
            Product = _unitOfWork.ProductRepo.Get(u => u.Id == productId, includeProperties: "Category"),
            Count = 1,
            ProductId = productId
        };
        return View(cart);
    }

    [HttpPost]
    [Authorize]
    public IActionResult Details(ShoppingCart shoppingCart)
    {
        var claimsIdentity = (ClaimsIdentity) User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
        shoppingCart.ApplicationUserId = userId;

        var cartFromDb = _unitOfWork.ShoppingCartRepo.Get(u =>
            u.ProductId == shoppingCart.ProductId && u.ApplicationUserId == userId);
        if (cartFromDb != null)
        {
            //Duplicate entry, increase count instead
            cartFromDb.Count += shoppingCart.Count;
            _unitOfWork.ShoppingCartRepo.Update(cartFromDb);
            _unitOfWork.Save();
        }
        else
        {
            //New entry
            _unitOfWork.ShoppingCartRepo.Add(shoppingCart);
            _unitOfWork.Save();
            HttpContext.Session.SetInt32(SD.SessionCart, 
                _unitOfWork.ShoppingCartRepo.GetAll(u => u.ApplicationUserId == userId).Count());
        }

        TempData["success"] = "Cart updated successfully";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}