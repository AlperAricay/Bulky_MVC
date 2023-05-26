using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class OrderController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    [BindProperty] public OrderViewModel OrderViewModel { get; set; }

    public OrderController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Details(int orderId)
    {
        OrderViewModel = new OrderViewModel
        {
            OrderHeader = _unitOfWork.OrderHeaderRepo.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
            OrderDetails =
                _unitOfWork.OrderDetailRepo.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
        };
        return View(OrderViewModel);
    }

    [HttpPost]
    [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
    public IActionResult UpdateOrderDetails()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeaderRepo.Get(u => u.Id == OrderViewModel.OrderHeader.Id);
        orderHeaderFromDb.Name = OrderViewModel.OrderHeader.Name;
        orderHeaderFromDb.PhoneNumber = OrderViewModel.OrderHeader.PhoneNumber;
        orderHeaderFromDb.StreetAddress = OrderViewModel.OrderHeader.StreetAddress;
        orderHeaderFromDb.City = OrderViewModel.OrderHeader.City;
        orderHeaderFromDb.State = OrderViewModel.OrderHeader.State;
        orderHeaderFromDb.PostalCode = OrderViewModel.OrderHeader.PostalCode;
        if (!string.IsNullOrEmpty(OrderViewModel.OrderHeader.Carrier))
        {
            orderHeaderFromDb.Carrier = OrderViewModel.OrderHeader.Carrier;
        }

        if (!string.IsNullOrEmpty(OrderViewModel.OrderHeader.TrackingNumber))
        {
            orderHeaderFromDb.Carrier = OrderViewModel.OrderHeader.TrackingNumber;
        }

        _unitOfWork.OrderHeaderRepo.Update(orderHeaderFromDb);
        _unitOfWork.Save();

        TempData["Success"] = "Order Details Updated Successfully.";

        return RedirectToAction(nameof(Details), new {orderId = orderHeaderFromDb.Id});
    }

    [HttpPost]
    [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
    public IActionResult StartProcessing()
    {
        _unitOfWork.OrderHeaderRepo.UpdateStatus(OrderViewModel.OrderHeader.Id, SD.StatusInProcess);
        _unitOfWork.Save();
        TempData["Success"] = "Order Details Updated Successfully.";
        return RedirectToAction(nameof(Details), new {orderId = OrderViewModel.OrderHeader.Id});
    }

    [HttpPost]
    [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
    public IActionResult ShipOrder()
    {
        var orderHeader = _unitOfWork.OrderHeaderRepo.Get(u => u.Id == OrderViewModel.OrderHeader.Id);
        orderHeader.TrackingNumber = OrderViewModel.OrderHeader.TrackingNumber;
        orderHeader.Carrier = OrderViewModel.OrderHeader.Carrier;
        orderHeader.OrderStatus = SD.StatusShipped;
        orderHeader.ShippingDate = DateTime.Now;
        if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
        {
            orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
        }

        _unitOfWork.OrderHeaderRepo.Update(orderHeader);
        _unitOfWork.Save();
        TempData["Success"] = "Order Shipped Successfully.";
        return RedirectToAction(nameof(Details), new {orderId = OrderViewModel.OrderHeader.Id});
    }

    [HttpPost]
    [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
    public IActionResult CancelOrder()
    {
        var orderHeader = _unitOfWork.OrderHeaderRepo.Get(u => u.Id == OrderViewModel.OrderHeader.Id);

        if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
        {
            var options = new RefundCreateOptions()
            {
                Reason = RefundReasons.RequestedByCustomer,
                PaymentIntent = orderHeader.PaymentIntentId
            };

            var service = new RefundService();
            var refund = service.Create(options);

            _unitOfWork.OrderHeaderRepo.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
        }
        else
        {
            _unitOfWork.OrderHeaderRepo.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
        }

        _unitOfWork.Save();
        TempData["Success"] = "Order Cancelled Successfully.";
        return RedirectToAction(nameof(Details), new {orderId = OrderViewModel.OrderHeader.Id});
    }

    [ActionName("Details")]
    [HttpPost]
    public IActionResult Details_PAY_NOW()
    {
        OrderViewModel.OrderHeader = _unitOfWork.OrderHeaderRepo
            .Get(u => u.Id == OrderViewModel.OrderHeader.Id, includeProperties: "ApplicationUser");
        OrderViewModel.OrderDetails = _unitOfWork.OrderDetailRepo
            .GetAll(u => u.OrderHeaderId == OrderViewModel.OrderHeader.Id, includeProperties: "Product");

        //stripe logic
        var domain = Request.Scheme + "://" + Request.Host.Value + "/";
        var options = new SessionCreateOptions
        {
            SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderViewModel.OrderHeader.Id}",
            CancelUrl = domain + $"admin/order/details?orderId={OrderViewModel.OrderHeader.Id}",
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "payment",
        };

        foreach (var item in OrderViewModel.OrderDetails)
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
        _unitOfWork.OrderHeaderRepo.UpdateStripePaymentId(OrderViewModel.OrderHeader.Id, session.Id,
            session.PaymentIntentId);
        _unitOfWork.Save();
        Response.Headers.Add("Location", session.Url);
        return new StatusCodeResult(303);
    }

    public IActionResult PaymentConfirmation(int orderHeaderId)
    {
        var orderHeader = _unitOfWork.OrderHeaderRepo.Get(u => u.Id == orderHeaderId);
        if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
        {
            //this is an order by company

            var service = new SessionService();
            var session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus.ToLower() == "paid")
            {
                _unitOfWork.OrderHeaderRepo.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                _unitOfWork.OrderHeaderRepo.UpdateStatus(orderHeaderId, orderHeader.OrderStatus,
                    SD.PaymentStatusApproved);
                _unitOfWork.Save();
            }
        }

        return View(orderHeaderId);
    }

    #region API CALLS

    [HttpGet]
    public IActionResult GetAll(string status)
    {
        IEnumerable<OrderHeader> objOrderHeaders;

        if (User.IsInRole(SD.RoleAdmin) || User.IsInRole(SD.RoleEmployee))
        {
            objOrderHeaders = _unitOfWork.OrderHeaderRepo.GetAll(includeProperties: "ApplicationUser").ToList();
        }
        else
        {
            var claimsIdentity = (ClaimsIdentity) User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            objOrderHeaders = _unitOfWork.OrderHeaderRepo
                .GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser").ToList();
        }

        switch (status)
        {
            case "pending":
                objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                break;
            case "inProcess":
                objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                break;
            case "completed":
                objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                break;
            case "approved":
                objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                break;
            default:
                break;
        }

        return Json(new {data = objOrderHeaders});
    }

    #endregion
}