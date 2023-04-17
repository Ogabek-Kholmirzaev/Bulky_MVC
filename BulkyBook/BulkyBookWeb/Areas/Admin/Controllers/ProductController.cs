using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
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

    public IActionResult Create()
    {
        var categoryList = _unitOfWork.CategoryRepository.GetAll()
            .Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            });

        ViewData["CategoryList"] = categoryList;

        return View();
    }

    [HttpPost]
    public IActionResult Create(Product product)
    {
        if (!ModelState.IsValid)
            return View();

        TempData["success"] = "Product created successfully";

        _unitOfWork.ProductRepository.Add(product);
        _unitOfWork.Save();

        return RedirectToAction("Index", "Product");
    }

    public IActionResult Edit(int? id)
    {
        if (id == null || id <= 0)
            return NotFound();

        var product = _unitOfWork.ProductRepository.Get(p => p.Id == id);

        if (product == null)
            return NotFound();

        return View(product);
    }

    [HttpPost]
    public IActionResult Edit(Product product)
    {
        if (!ModelState.IsValid)
            return View();

        TempData["success"] = "Product updated successfully";

        _unitOfWork.ProductRepository.Update(product);
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