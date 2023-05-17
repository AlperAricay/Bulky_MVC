using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers;
[Area("Admin")]
[Authorize(Roles = SD.RoleAdmin)]
public class CategoryController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var objCategoryList = _unitOfWork.CategoryRepo.GetAll().ToList();
        return View(objCategoryList);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Category obj)
    {
        if (_unitOfWork.CategoryRepo.Get(category => category.DisplayOrder == obj.DisplayOrder) != null)
        {
            ModelState.AddModelError("displayOrder", "The Display Order must be unique.");
        }

        if (ModelState.IsValid)
        {
            _unitOfWork.CategoryRepo.Add(obj);
            _unitOfWork.Save();
            TempData["success"] = "Category created successfully.";
            return RedirectToAction("Index");
        }

        return View();
    }

    public IActionResult Edit(int? productId)
    {
        if (productId is null or 0) return NotFound();

        var categoryFromDb = _unitOfWork.CategoryRepo.Get(u => u.Id == productId);
        if (categoryFromDb == null) return NotFound();

        return View(categoryFromDb);
    }

    [HttpPost]
    public IActionResult Edit(Category obj)
    {
        if (_unitOfWork.CategoryRepo.Get(category =>
                category.DisplayOrder == obj.DisplayOrder && category.Id != obj.Id) != null)
        {
            ModelState.AddModelError("displayOrder", "The Display Order must be unique.");
        }

        if (ModelState.IsValid)
        {
            _unitOfWork.CategoryRepo.Update(obj);
            _unitOfWork.Save();
            TempData["success"] = "Category edited successfully.";
            return RedirectToAction("Index");
        }

        return View();
    }

    public IActionResult Delete(int? productId)
    {
        if (productId is null or 0) return NotFound();

        var categoryFromDb = _unitOfWork.CategoryRepo.Get(u => u.Id == productId);
        if (categoryFromDb == null) return NotFound();

        return View(categoryFromDb);
    }

    [HttpPost, ActionName("Delete")]
    public IActionResult DeletePost(int? productId)
    {
        var obj = _unitOfWork.CategoryRepo.Get(u => u.Id == productId);
        if (obj == null) return NotFound();

        _unitOfWork.CategoryRepo.Remove(obj);
        _unitOfWork.Save();
        TempData["success"] = "Category deleted successfully.";
        return RedirectToAction("Index");
    }
}