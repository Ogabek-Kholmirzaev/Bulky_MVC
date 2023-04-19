using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBookWeb.Areas.Admin.Controllers;

public class ProductController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
    {
        _unitOfWork = unitOfWork;
        _webHostEnvironment = webHostEnvironment;
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

        if (file != null)
        {
            var wwwRootPath = _webHostEnvironment.WebRootPath;
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var productPath = Path.Combine(wwwRootPath, @"images\product");

            using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
            {
                var oldImagePath = Path.Combine(wwwRootPath, productVM.Product.ImageUrl.Trim('\\'));

                if(System.IO.File.Exists(oldImagePath)))
                    System.IO.File.Delete(oldImagePath);
            }

            productVM.Product.ImageUrl = @"\images\product\" + fileName;
        }

        if (productVM.Product.Id == 0)
        {
            _unitOfWork.ProductRepository.Add(productVM.Product);
            TempData["success"] = "Product created successfully";
        }
        else
        {
            _unitOfWork.ProductRepository.Update(productVM.Product);
            TempData["success"] = "Product created successfully";
        }

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