using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class CartController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CartController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var shoppingCartVM = new ShoppingCartVM()
        {
            ShoppingCartList = _unitOfWork.ShoppingCartRepository.GetAll().Where(c => c.ApplicationUserId == userId)
        };

        foreach (var cart in shoppingCartVM.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            shoppingCartVM.OrderTotal += cart.Price * cart.Count;
        }

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
        return View();
    }
}