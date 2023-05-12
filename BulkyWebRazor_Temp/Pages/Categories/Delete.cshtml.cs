using BulkyWebRazor_Temp.Data;
using BulkyWebRazor_Temp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyWebRazor_Temp.Pages.Categories;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _db;
    [BindProperty]
    public Category? Category { get; set; }
    
    public DeleteModel(ApplicationDbContext db)
    {
        _db = db;
    }
    
    public void OnGet(int? id)
    {
        if (id is null or 0) return;

        var categoryFromDb = _db.Categories.Find(id);
        if (categoryFromDb == null) return;

        Category = categoryFromDb;
    }

    public IActionResult OnPost()
    {
        if (Category == null) return NotFound();
        
        _db.Categories.Remove(Category);
        _db.SaveChanges();
        TempData["success"] = "Category deleted successfully.";
        return RedirectToPage("Index");
    }
}