using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bulky.Models.ViewModels;

public class RoleViewModel
{
    public ApplicationUser ApplicationUser { get; set; }
    [ValidateNever] public IEnumerable<SelectListItem> RoleList { get; set; }
    [ValidateNever] public IEnumerable<SelectListItem> CompanyList { get; set; }
}