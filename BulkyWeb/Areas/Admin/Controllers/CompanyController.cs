using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.RoleAdmin)]
public class CompanyController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CompanyController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var objCompanyList = _unitOfWork.CompanyRepo.GetAll().ToList();
        return View(objCompanyList);
    }

    public IActionResult Upsert(int? id)
    {
        if (id is null or 0)
        {
            //create
            return View(new Company());
        }
        else
        {
            //update
            var companyObj = _unitOfWork.CompanyRepo.Get(u => u.Id == id);
            return View(companyObj);
        }
    }

    [HttpPost]
    public IActionResult Upsert(Company companyObj)
    {
        if (ModelState.IsValid)
        {
            if (companyObj.Id == 0)
            {
                _unitOfWork.CompanyRepo.Add(companyObj);
                TempData["success"] = "Company created successfully.";
            }
            else
            {
                _unitOfWork.CompanyRepo.Update(companyObj);
                TempData["success"] = "Company edited successfully.";
            }

            _unitOfWork.Save();
            return RedirectToAction("Index");
        }
        
        return View(companyObj);
    }

    #region API CALLS

    [HttpGet]
    public IActionResult GetAll()
    {
        var objCompanyList = _unitOfWork.CompanyRepo.GetAll().ToList();
        return Json(new {data = objCompanyList});
    }
    
    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        var companyToDelete = _unitOfWork.CompanyRepo.Get(u=> u.Id == id);
        if (companyToDelete == null)
        {
            return Json(new { success = false, message = "Error while deleting" });
        }

        _unitOfWork.CompanyRepo.Remove(companyToDelete);
        _unitOfWork.Save();
        TempData["success"] = "Company deleted successfully.";
        
        return Json(new {success = true, message = "Deleted successfully"});
    }

    #endregion
}