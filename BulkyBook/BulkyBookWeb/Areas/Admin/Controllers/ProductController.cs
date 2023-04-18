using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBookWeb.Areas.Admin.Controllers;

public class ProductController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var productList = _unitOfWork.ProductRepository.GetAll();

        return View(productList);
    }

    public IActionResult Upsert(int? id)
    {
        var productVM = new ProductVM
        {
            Product = new Product(),
            CategoryList = _unitOfWork.CategoryRepository.GetAll()
                .Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                })
        };

        if(id == null || id <= 0)
            return View(productVM);

        var product = _unitOfWork.ProductRepository.Get(p => p.Id == id);

        if (product == null)
            return NotFound();

        productVM.Product = product;

        return View(productVM);
    }

    [HttpPost]
    public IActionResult Upsert(ProductVM productVM, IFormFile? file)
    {
        if (!ModelState.IsValid)
        {
            productVM.CategoryList = _unitOfWork.CategoryRepository.GetAll()
                .Select(p => new SelectListItem()
                {
                    Text = p.Name,
                    Value = p.Id.ToString()
                });

            return View(productVM);
        }

        TempData["success"] = "Product created successfully";

        _unitOfWork.ProductRepository.Add(productVM.Product);
        _unitOfWork.Save();

        return RedirectToAction("Index", "Product");
    }

    public IActionResult Delete(int? id)
    {
        if (id == null || id <= 0)
            return NotFound();

        var product = _unitOfWork.ProductRepository.Get(p => p.Id == id);

        if (product == null)
            return NotFound();

        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    public IActionResult DeleteCategory(int? id)
    {
        var product = _unitOfWork.ProductRepository.Get(p => p.Id == id);

        if (product == null)
            return NotFound();

        TempData["success"] = "Product deleted successfully";

        _unitOfWork.ProductRepository.Remove(product);
        _unitOfWork.Save();

        return RedirectToAction("Index", "Product");
    }
}