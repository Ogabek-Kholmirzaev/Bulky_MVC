using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
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
            var productList = _unitOfWork.ProductRepository.GetAll();
            
            return View(productList);
        }

        public IActionResult Details(int productId)
        {
            var product = _unitOfWork.ProductRepository.Get(p => p.Id == productId);

            if (product == null)
                return RedirectToAction("Index", "Home");

            var cart = new ShoppingCart
            {
                ProductId = productId,
                Product = product,
                Count = 1
            };

            return View(cart);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart cart)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var cartFromDb = _unitOfWork.ShoppingCartRepository
                .Get(c => c.ProductId == cart.ProductId && c.ApplicationUserId == userId);

            cart.ApplicationUserId = userId;

            if (cartFromDb == null)
                _unitOfWork.ShoppingCartRepository.Add(cart);
            else
                cartFromDb.Count += cart.Count;

            _unitOfWork.Save();
            TempData["success"] = "Cart updated successfully";

            var shoppingCartCount =
                _unitOfWork.ShoppingCartRepository.GetAll().Count(c => c.ApplicationUserId == userId);

            HttpContext.Session.SetInt32(SD.SessionCart, shoppingCartCount);

            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}