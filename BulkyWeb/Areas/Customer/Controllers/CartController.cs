using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace BulkyWeb.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class CartController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    [BindProperty] public ShoppingCartViewModel ShoppingCartViewModel { get; set; }

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
                _unitOfWork.ShoppingCartRepo.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
            OrderHeader = new OrderHeader()
        };

        foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartViewModel.OrderHeader.OrderTotal += cart.Count * cart.Price;
        }

        return View(ShoppingCartViewModel);
    }

    public IActionResult Summary()
    {
        var claimsIdentity = (ClaimsIdentity) User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        ShoppingCartViewModel = new ShoppingCartViewModel
        {
            ShoppingCartList =
                _unitOfWork.ShoppingCartRepo.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
            OrderHeader = new OrderHeader()
        };

        ShoppingCartViewModel.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUserRepo.Get(u => u.Id == userId);
        ShoppingCartViewModel.OrderHeader.Name = ShoppingCartViewModel.OrderHeader.ApplicationUser.Name;
        ShoppingCartViewModel.OrderHeader.PhoneNumber = ShoppingCartViewModel.OrderHeader.ApplicationUser.PhoneNumber;
        ShoppingCartViewModel.OrderHeader.StreetAddress =
            ShoppingCartViewModel.OrderHeader.ApplicationUser.StreetAddress;
        ShoppingCartViewModel.OrderHeader.City = ShoppingCartViewModel.OrderHeader.ApplicationUser.City;
        ShoppingCartViewModel.OrderHeader.State = ShoppingCartViewModel.OrderHeader.ApplicationUser.State;
        ShoppingCartViewModel.OrderHeader.PostalCode = ShoppingCartViewModel.OrderHeader.ApplicationUser.PostalCode;

        foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartViewModel.OrderHeader.OrderTotal += cart.Count * cart.Price;
        }

        return View(ShoppingCartViewModel);
    }

    [HttpPost]
    [ActionName("Summary")]
    public IActionResult SummaryPost()
    {
        var claimsIdentity = (ClaimsIdentity) User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        ShoppingCartViewModel.ShoppingCartList =
            _unitOfWork.ShoppingCartRepo.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");

        ShoppingCartViewModel.OrderHeader.OrderDate = DateTime.Now;
        ShoppingCartViewModel.OrderHeader.ApplicationUserId = userId;

        var user = _unitOfWork.ApplicationUserRepo.Get(u => u.Id == userId);

        foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartViewModel.OrderHeader.OrderTotal += cart.Count * cart.Price;
        }

        if (user.CompanyId.GetValueOrDefault() == 0)
        {
            //regular customer
            ShoppingCartViewModel.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            ShoppingCartViewModel.OrderHeader.OrderStatus = SD.StatusPending;
        }
        else
        {
            //company
            ShoppingCartViewModel.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
            ShoppingCartViewModel.OrderHeader.OrderStatus = SD.StatusApproved;
        }

        _unitOfWork.OrderHeaderRepo.Add(ShoppingCartViewModel.OrderHeader);
        _unitOfWork.Save();

        foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
        {
            var orderDetail = new OrderDetail
            {
                ProductId = cart.ProductId,
                OrderHeaderId = ShoppingCartViewModel.OrderHeader.Id,
                Price = cart.Price,
                Count = cart.Count
            };
            _unitOfWork.OrderDetailRepo.Add(orderDetail);
        }

        _unitOfWork.Save();

        if (user.CompanyId.GetValueOrDefault() == 0)
        {
            //stripe logic for customer payment
            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartViewModel.OrderHeader.Id}",
                CancelUrl = domain + "customer/cart/index",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in ShoppingCartViewModel.ShoppingCartList)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long) (item.Price * 100), // $20.50 => 2050
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                };
                options.LineItems.Add(sessionLineItem);
            }


            var service = new SessionService();
            var session = service.Create(options);
            _unitOfWork.OrderHeaderRepo.UpdateStripePaymentId(ShoppingCartViewModel.OrderHeader.Id, session.Id,
                session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        return RedirectToAction(nameof(OrderConfirmation), new {id = ShoppingCartViewModel.OrderHeader.Id});
    }

    public IActionResult OrderConfirmation(int id)
    {
        var orderHeader = _unitOfWork.OrderHeaderRepo.Get(u => u.Id == id, includeProperties: "ApplicationUser");
        if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
        {
            //customer order
            var service = new SessionService();
            var session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus.ToLower() == "paid")
            {
                _unitOfWork.OrderHeaderRepo.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                _unitOfWork.OrderHeaderRepo.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                _unitOfWork.Save();
            }
        }

        var shoppingCarts = _unitOfWork.ShoppingCartRepo
            .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
        _unitOfWork.ShoppingCartRepo.RemoveRange(shoppingCarts);
        _unitOfWork.Save();

        return View(id);
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
        var cartFromDb = _unitOfWork.ShoppingCartRepo.Get(u => u.Id == cartId, tracked: true);
        if (cartFromDb.Count <= 1)
        {
            HttpContext.Session.SetInt32(SD.SessionCart,
                _unitOfWork.ShoppingCartRepo.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
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
        var cartFromDb = _unitOfWork.ShoppingCartRepo.Get(u => u.Id == cartId, tracked: true);
        HttpContext.Session.SetInt32(SD.SessionCart,
            _unitOfWork.ShoppingCartRepo.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
        _unitOfWork.ShoppingCartRepo.Remove(cartFromDb);
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }
}