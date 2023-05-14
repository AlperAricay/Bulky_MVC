using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers;
[Area("Admin")]
public class ProductController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var objProductList = _unitOfWork.ProductRepo.GetAll().ToList();
        return View(objProductList);
    }

    public IActionResult Create()
    {
        var categoryList = _unitOfWork.CategoryRepo.GetAll().Select(u => new SelectListItem
        {
            Text = u.Name,
            Value = u.Id.ToString()
        });
        var productViewModel = new ProductViewModel
        {
            CategoryList = categoryList,
            Product = new Product()
        };
        return View(productViewModel);
    }

    [HttpPost]
    public IActionResult Create(ProductViewModel productViewModel)
    {
        if (ModelState.IsValid)
        {
            _unitOfWork.ProductRepo.Add(productViewModel.Product);
            _unitOfWork.Save();
            TempData["success"] = "Product created successfully.";
            return RedirectToAction("Index");
        }
        
        var categoryList = _unitOfWork.CategoryRepo.GetAll().Select(u => new SelectListItem
        {
            Text = u.Name,
            Value = u.Id.ToString()
        });
        productViewModel.CategoryList = categoryList;
        return View(productViewModel);
    }

    public IActionResult Edit(int? id)
    {
        if (id is null or 0) return NotFound();
        
        var productFromDb = _unitOfWork.ProductRepo.Get(u => u.Id == id);
        if (productFromDb == null) return NotFound();

        return View(productFromDb);
    }

    [HttpPost]
    public IActionResult Edit(Product obj)
    {
        if (ModelState.IsValid)
        {
            _unitOfWork.ProductRepo.Update(obj);
            _unitOfWork.Save();
            TempData["success"] = "Product edited successfully.";
            return RedirectToAction("Index");
        }

        return View();
    }
    
    public IActionResult Delete(int? id)
    {
        if (id is null or 0) return NotFound();
        
        var productFromDb = _unitOfWork.ProductRepo.Get(u => u.Id == id);
        if (productFromDb == null) return NotFound();

        return View(productFromDb);
    }
    
    [HttpPost, ActionName("Delete")]
    public IActionResult DeletePost(int? id)
    {
        var obj = _unitOfWork.ProductRepo.Get(u => u.Id == id);
        if (obj == null) return NotFound();
        
        _unitOfWork.ProductRepo.Remove(obj);
        _unitOfWork.Save();
        TempData["success"] = "Product deleted successfully.";
        return RedirectToAction("Index");
    }
}