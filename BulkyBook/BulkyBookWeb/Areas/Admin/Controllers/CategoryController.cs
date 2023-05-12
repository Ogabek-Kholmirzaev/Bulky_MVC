using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Admin.Controllers;

[Area("Admin")]
//[Authorize(Roles = SD.Role_Admin)]
public class CategoryController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var categoryList = _unitOfWork.CategoryRepository.GetAll();

        return View(categoryList);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Category category)
    {
        //if (obj.Name == obj.DisplayOrder.ToString())
        //    ModelState.AddModelError("Name", "The Display Order cannot exactly match the Name");

        if (!ModelState.IsValid)
            return View();

        TempData["success"] = "Category created successfully";

        _unitOfWork.CategoryRepository.Add(category);
        _unitOfWork.Save();

        return RedirectToAction("Index", "Category");
    }

    public IActionResult Edit(int? id)
    {
        if (id == null || id <= 0)
            return NotFound();

        var category = _unitOfWork.CategoryRepository.Get(c => c.Id == id);

        if (category == null)
            return NotFound();

        return View(category);
    }

    [HttpPost]
    public IActionResult Edit(Category category)
    {
        if (!ModelState.IsValid)
            return View();

        TempData["success"] = "Category updated successfully";

        _unitOfWork.CategoryRepository.Update(category);
        _unitOfWork.Save();

        return RedirectToAction("Index", "Category");
    }

    public IActionResult Delete(int? id)
    {
        if (id == null || id <= 0)
            return NotFound();

        var category = _unitOfWork.CategoryRepository.Get(c => c.Id == id);

        if (category == null)
            return NotFound();

        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    public IActionResult DeleteCategory(int? id)
    {
        var category = _unitOfWork.CategoryRepository.Get(c => c.Id == id);

        if (category == null)
            return NotFound();

        TempData["success"] = "Category deleted successfully";

        _unitOfWork.CategoryRepository.Remove(category);
        _unitOfWork.Save();

        return RedirectToAction("Index", "Category");
    }
}