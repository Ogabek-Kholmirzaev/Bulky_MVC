using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace BulkyBookWeb.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class CartController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    [BindProperty]
    public ShoppingCartVM ShoppingCartVM { get; set; }

    public CartController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var shoppingCartList = _unitOfWork.ShoppingCartRepository.GetAll().Where(c => c.ApplicationUserId == userId);
        var shoppingCartVM = new ShoppingCartVM()
        {
            ShoppingCartList = shoppingCartList,
            OrderHeader = new OrderHeader()
        };

        foreach (var cart in shoppingCartVM.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            shoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
        }

        var shoppingCartCount = shoppingCartList.Count();

        HttpContext.Session.SetInt32(SD.SessionCart, shoppingCartCount);

        return View(shoppingCartVM);
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
        var cart = _unitOfWork.ShoppingCartRepository.Get(c => c.Id == cartId);

        if(cart == null)
            return NotFound();

        cart.Count++;
        _unitOfWork.Save();

        return RedirectToAction("Index");
    }

    public IActionResult Minus(int cartId)
    {
        var cart = _unitOfWork.ShoppingCartRepository.Get(c => c.Id == cartId);

        if (cart == null)
            return NotFound();

        if (cart.Count <= 1)
            _unitOfWork.ShoppingCartRepository.Remove(cart);
        else
            cart.Count--;

        _unitOfWork.Save();

        return RedirectToAction("Index");
    }

    public IActionResult Remove(int cartId)
    {
        var cart = _unitOfWork.ShoppingCartRepository.Get(c => c.Id == cartId);

        if (cart == null)
            return NotFound();

        _unitOfWork.ShoppingCartRepository.Remove(cart);
        _unitOfWork.Save();

        return RedirectToAction("Index");
    }

    public IActionResult Summary()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var shoppingCartVM = new ShoppingCartVM()
        {
            ShoppingCartList = _unitOfWork.ShoppingCartRepository.GetAll().Where(u => u.ApplicationUserId == userId),
            OrderHeader = new OrderHeader
            {
                ApplicationUser = _unitOfWork.ApplicationUserRepository.Get(u=>u.Id == userId)!
            }
        };

        shoppingCartVM.OrderHeader.Name = shoppingCartVM.OrderHeader.ApplicationUser.Name;
        shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber!;
        shoppingCartVM.OrderHeader.StreetAddress = shoppingCartVM.OrderHeader.ApplicationUser.StreetAddress!;
        shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.ApplicationUser.City!;
        shoppingCartVM.OrderHeader.State = shoppingCartVM.OrderHeader.ApplicationUser.State!;
        shoppingCartVM.OrderHeader.PostalCode = shoppingCartVM.OrderHeader.ApplicationUser.PostalCode!;

        foreach (var cart in shoppingCartVM.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            shoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
        }

        return View(shoppingCartVM);
    }

    [HttpPost, ActionName("Summary")]
    public IActionResult SummaryPost()
    {
	    var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

	    ShoppingCartVM.ShoppingCartList =
		    _unitOfWork.ShoppingCartRepository.GetAll().Where(u => u.ApplicationUserId == userId);

	    var applicationUser =
		    _unitOfWork.ApplicationUserRepository.Get(u => u.Id == userId)!;

	    ShoppingCartVM.OrderHeader.ApplicationUserId = userId;
	    ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;

	    foreach (var cart in ShoppingCartVM.ShoppingCartList)
	    {
		    cart.Price = GetPriceBasedOnQuantity(cart);
		    ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
	    }

	    if (applicationUser.CompanyId.GetValueOrDefault() == 0)
	    {
		    ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
		    ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
	    }
	    else
	    {
		    ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
		    ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
	    }

	    _unitOfWork.OrderHeaderRepository.Add(ShoppingCartVM.OrderHeader);
	    _unitOfWork.Save();

	    foreach (var cart in ShoppingCartVM.ShoppingCartList)
	    {
		    var orderDetail = new OrderDetail()
		    {
			    ProductId = cart.ProductId,
			    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
			    Price = cart.Price,
			    Count = cart.Count
		    };

		    _unitOfWork.OrderDetailRepository.Add(orderDetail);
		    _unitOfWork.Save();
	    }

	    if (applicationUser.CompanyId.GetValueOrDefault() == 0)
	    {
		    //regular customer stripe logic
		    const string domain = "https://localhost:7208";
            var options = new SessionCreateOptions
            {
	            LineItems = new List<SessionLineItemOptions>(),
				Mode = "payment",
				SuccessUrl = domain + "/Customer/Cart/OrderConfirmation?id=" + ShoppingCartVM.OrderHeader.Id,
				CancelUrl = domain + "/Customer/Cart/Index"
			};

            foreach (var cart in ShoppingCartVM.ShoppingCartList ?? new List<ShoppingCart>())
            {
	            options.LineItems.Add(new SessionLineItemOptions
	            {
		            PriceData = new SessionLineItemPriceDataOptions
		            {
			            UnitAmount = (long)(cart.Price * 100),
			            Currency = "usd",
			            ProductData = new SessionLineItemPriceDataProductDataOptions
			            {
				            Name = cart.Product.Title
			            }
		            },
		            Quantity = cart.Count
	            });
            }

            var service = new SessionService();
            var session = service.Create(options);

            _unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();

            Response.Headers.Add("Location", session.Url);

            return new StatusCodeResult(303);
	    }


	    return RedirectToAction("OrderConfirmation", new { id = ShoppingCartVM.OrderHeader.Id });
	}

    public IActionResult OrderConfirmation(int id)
    {
        var order = _unitOfWork.OrderHeaderRepository.Get(o => o.Id == id);

        if (order == null)
            return NotFound();

        if (order.OrderStatus != SD.PaymentStatusDelayedPayment)
        {
	        var service = new SessionService();
            var session = service.Get(order.SessionId);

            if(session.PaymentStatus.ToLower() == "paid")
            {
	            _unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                _unitOfWork.OrderHeaderRepository.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
	            _unitOfWork.Save();
            }
        }

	    return View(id);
    }
}