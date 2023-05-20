using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.RoleAdmin)]
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
        var objProductList = _unitOfWork.ProductRepo.GetAll(includeProperties: "Category").ToList();
        return View(objProductList);
    }

    public IActionResult Upsert(int? id)
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
        if (id is null or 0)
        {
            //create
            return View(productViewModel);
        }
        else
        {
            //update
            productViewModel.Product = _unitOfWork.ProductRepo.Get(u => u.Id == id);
            return View(productViewModel);
        }
    }

    [HttpPost]
    public IActionResult Upsert(ProductViewModel productViewModel, IFormFile? file)
    {
        if (ModelState.IsValid)
        {
            var wwwRootPath = _webHostEnvironment.WebRootPath;
            if (file != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var productPath = Path.Combine(wwwRootPath, @"images\product");

                if (!string.IsNullOrEmpty(productViewModel.Product.ImageUrl))
                {
                    //delete the old image
                    var oldImagePath = Path.Combine(wwwRootPath, productViewModel.Product.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                productViewModel.Product.ImageUrl = @"\images\product\" + fileName;
            }

            if (productViewModel.Product.Id == 0)
            {
                _unitOfWork.ProductRepo.Add(productViewModel.Product);
                TempData["success"] = "Product created successfully.";
            }
            else
            {
                _unitOfWork.ProductRepo.Update(productViewModel.Product);
                TempData["success"] = "Product edited successfully.";
            }

            _unitOfWork.Save();
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

    #region API CALLS

    [HttpGet]
    public IActionResult GetAll()
    {
        var objProductList = _unitOfWork.ProductRepo.GetAll(includeProperties: "Category").ToList();
        return Json(new {data = objProductList});
    }
    
    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        var productToDelete = _unitOfWork.ProductRepo.Get(u=> u.Id == id);
        if (productToDelete == null)
        {
            return Json(new { success = false, message = "Error while deleting" });
        }
        
        var oldImagePath = 
            Path.Combine(_webHostEnvironment.WebRootPath,
            productToDelete.ImageUrl.TrimStart('\\'));
        
        if (System.IO.File.Exists(oldImagePath)) System.IO.File.Delete(oldImagePath);
        
        _unitOfWork.ProductRepo.Remove(productToDelete);
        _unitOfWork.Save();
        TempData["success"] = "Product deleted successfully.";
        
        return Json(new {success = true, message = "Deleted successfully"});
    }

    #endregion
}