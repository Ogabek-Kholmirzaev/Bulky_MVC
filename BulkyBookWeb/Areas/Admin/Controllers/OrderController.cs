using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Cache;
using System.Security.Claims;
using BulkyBook.Utility;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Stripe;
using Stripe.Checkout;

namespace BulkyBookWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class OrderController : Controller
{
	private readonly IUnitOfWork _unitOfWork;

    [BindProperty]
    public OrderVM OrderVM { get; set; }

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
        var orderHeader = _unitOfWork.OrderHeaderRepository.Get(o => o.Id == orderId);

        if(orderHeader == null)
            return NotFound();

        OrderVM = new OrderVM()
        {
            OrderHeader = orderHeader,
            OrderDetails = orderHeader.OrderDetails
        };

        return View(OrderVM);
    }

    [HttpPost]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public IActionResult UpdateOrderDetail()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeaderRepository.Get(u => u.Id == OrderVM.OrderHeader.Id);

        if( orderHeaderFromDb == null)
            return NotFound();

        orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
        orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
        orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
        orderHeaderFromDb.City = OrderVM.OrderHeader.City;
        orderHeaderFromDb.State = OrderVM.OrderHeader.State;
        orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;

        if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;

        if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            orderHeaderFromDb.Carrier = OrderVM.OrderHeader.TrackingNumber;

        _unitOfWork.OrderHeaderRepository.Update(orderHeaderFromDb);
        _unitOfWork.Save();

        TempData["Success"] = "Order Details Updated Successfully.";

        return RedirectToAction("Details", new { orderId = orderHeaderFromDb.Id });
    }

    [HttpPost]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public IActionResult StartProcessing()
    {
        _unitOfWork.OrderHeaderRepository.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
        _unitOfWork.Save();

        TempData["Success"] = "Order Details Updated Successfully.";

        return RedirectToAction("Details", new { orderId = OrderVM.OrderHeader.Id });
    }

    [HttpPost]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public IActionResult ShipOrder()
    {
        var orderHeader = _unitOfWork.OrderHeaderRepository.Get(o => o.Id == OrderVM.OrderHeader.Id);

        if(orderHeader == null)
            return NotFound();

        orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
        orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
        orderHeader.OrderStatus = SD.StatusShipped;
        orderHeader.ShippingDate = DateTime.Now;

        if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);

        _unitOfWork.Save();

        TempData["Success"] = "Order Details Updated Successfully.";

        return RedirectToAction("Details", new { orderId = OrderVM.OrderHeader.Id });
    }

    [HttpPost]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public IActionResult CancelOrder()
    {
        var orderHeader = _unitOfWork.OrderHeaderRepository.Get(o => o.Id == OrderVM.OrderHeader.Id);

        if(orderHeader == null)
            return NotFound();

        if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
        {
            var options = new RefundCreateOptions
            {
                Reason = RefundReasons.RequestedByCustomer,
                PaymentIntent = orderHeader.PaymentIntentId
            };

            var service = new RefundService();

            Refund refund = service.Create(options);

            _unitOfWork.OrderHeaderRepository.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
        }
        else
        {
            _unitOfWork.OrderHeaderRepository.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
        }

        _unitOfWork.Save();

        TempData["Success"] = "Order Cancelled Successfully.";

        return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
    }

    [ActionName("Details")]
    [HttpPost]
    public IActionResult Details_PAY_NOW()
    {
        var orderHeader = _unitOfWork.OrderHeaderRepository.Get(u => u.Id == OrderVM.OrderHeader.Id);

        if(orderHeader == null)
            return NotFound();

        //stripe logic
        var domain = $"{Request.Scheme}://{Request.Host.Value}/";
        var options = new SessionCreateOptions
        {
            SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={orderHeader.Id}",
            CancelUrl = domain + $"admin/order/details?orderId={orderHeader.Id}",
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "payment",
        };

        foreach (var item in orderHeader.OrderDetails)
        {
            var sessionLineItem = new SessionLineItemOptions 
            {
                PriceData = new SessionLineItemPriceDataOptions 
                {
                    UnitAmount = (long)(item.Price * 100), // $20.50 => 2050
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

        _unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
        _unitOfWork.Save();

        Response.Headers.Add("Location", session.Url);

        return new StatusCodeResult(303);
    }

    public IActionResult PaymentConfirmation(int orderHeaderId)
    {
        var orderHeader = _unitOfWork.OrderHeaderRepository.Get(u => u.Id == orderHeaderId);

        if(orderHeader == null)
            return NotFound();

        if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment) {
            //this is an order by company
            var service = new SessionService();
            var session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus.ToLower() == "paid")
            {
                _unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                _unitOfWork.OrderHeaderRepository.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                _unitOfWork.Save();
            }
        }

        return View(orderHeaderId);
    }

    #region API CALLS

    [HttpGet]
	public IActionResult GetAll(string status)
	{
		var orderHeaders = _unitOfWork.OrderHeaderRepository.GetAll();

        if (!User.IsInRole(SD.Role_Admin) && !User.IsInRole(SD.Role_Employee))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            orderHeaders = orderHeaders.Where(o => o.ApplicationUserId == userId);
        }

        orderHeaders = status switch
        {
            "inprocess" => orderHeaders.Where(o => o.PaymentStatus == SD.StatusInProcess),
            "pending" => orderHeaders.Where(o => o.PaymentStatus == SD.PaymentStatusPending),
            "completed" => orderHeaders.Where(o => o.PaymentStatus == SD.StatusShipped),
            "approved" => orderHeaders.Where(o => o.PaymentStatus == SD.StatusApproved),
            _ => orderHeaders
        };

        return Json(new { data = orderHeaders });
	}

	#endregion
}