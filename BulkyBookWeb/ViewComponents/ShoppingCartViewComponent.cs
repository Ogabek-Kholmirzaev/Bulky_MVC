using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.ViewComponents;

public class ShoppingCartViewComponent : ViewComponent
{
    private readonly IUnitOfWork _unitOfWork;

    public ShoppingCartViewComponent(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if(userId == null)
            return View(0);

        var shoppingCartSession = HttpContext.Session.GetInt32(SD.SessionCart);

        var smth = HttpContext.Session.Keys.FirstOrDefault(SD.SessionCart);

        if (shoppingCartSession == null)
        {
            var shoppingCartCount = 
                _unitOfWork.ShoppingCartRepository.GetAll().Count(c => c.ApplicationUserId == userId);

            HttpContext.Session.SetInt32(SD.SessionCart, shoppingCartCount);

            return View(shoppingCartCount);
        }

        return View(shoppingCartSession.Value);
    }
}