using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class CartController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    public ShoppingCartViewModel ShoppingCartViewModel { get; set; }

    public CartController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var claimsIdentity = (ClaimsIdentity) User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        ShoppingCartViewModel = new ShoppingCartViewModel
        {
            ShoppingCartList =
                _unitOfWork.ShoppingCartRepo.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product")
        };

        foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartViewModel.OrderTotal += cart.Count * cart.Price;
        }

        return View(ShoppingCartViewModel);
    }

    public IActionResult Summary()
    {
        return View();
    }

    private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
    {
        return shoppingCart.Count switch
        {
            <= 50 => shoppingCart.Product.Price,
            <= 100 => shoppingCart.Product.Price50,
            _ => shoppingCart.Product.Price100
        };
    }

    public IActionResult Plus(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingCartRepo.Get(u => u.Id == cartId);
        cartFromDb.Count++;
        _unitOfWork.ShoppingCartRepo.Update(cartFromDb);
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Minus(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingCartRepo.Get(u => u.Id == cartId);
        if (cartFromDb.Count <= 1)
        {
            _unitOfWork.ShoppingCartRepo.Remove(cartFromDb);
        }
        else
        {
            cartFromDb.Count--;
            _unitOfWork.ShoppingCartRepo.Update(cartFromDb);
        }

        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Remove(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingCartRepo.Get(u => u.Id == cartId);
        _unitOfWork.ShoppingCartRepo.Remove(cartFromDb);
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }
}