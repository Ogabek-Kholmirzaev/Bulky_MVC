using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Admin.Controllers;

[Area("Admin")]
//[Authorize(Roles = SD.Role_Admin)]
public class CompanyController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CompanyController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var companyList = _unitOfWork.CompanyRepository.GetAll();

        return View(companyList);
    }

    public IActionResult Upsert(int? id)
    {
        var company = _unitOfWork.CompanyRepository.Get(company => company.Id == id) ?? new Company();

        return View(company);
    }

    [HttpPost]
    public IActionResult Upsert(Company company)
    {
        if (!ModelState.IsValid)
            return View(company);

        if (company.Id == 0)
        {
            _unitOfWork.CompanyRepository.Add(company);
            TempData["success"] = "Company created successfully";
        }
        else
        {
            _unitOfWork.CompanyRepository.Update(company);
            TempData["success"] = "Company updated successfully";
        }

        _unitOfWork.Save();
        

        return RedirectToAction("Index");
    }

    #region API CALLS

    [HttpGet]
    public IActionResult GetAll()
    {
        var companyList = _unitOfWork.CompanyRepository.GetAll();

        return Json(new { data = companyList });
    }

    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        var company = _unitOfWork.CompanyRepository.Get(company => company.Id == id);

        if (company == null)
            return Json(new { success = false, message = "Error while deleting" });

        _unitOfWork.CompanyRepository.Remove(company);
        _unitOfWork.Save();

        return Json(new { success = true, message = "Delete successful" });
    }

    #endregion
}