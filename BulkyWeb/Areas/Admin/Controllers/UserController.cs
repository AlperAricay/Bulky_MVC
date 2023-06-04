using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.RoleAdmin)]
public class UserController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUnitOfWork _unitOfWork;

    public UserController(UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork,
        RoleManager<IdentityRole> roleManager)
    {
        _unitOfWork = unitOfWork;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult RoleManagement(string id)
    {
        var applicationUser = _unitOfWork.ApplicationUserRepo.Get(u => u.Id == id, includeProperties: "Company");
        
        var roleViewModel = new RoleViewModel
        {
            ApplicationUser = applicationUser,
            RoleList = _roleManager.Roles.Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Name
            }),
            CompanyList = _unitOfWork.CompanyRepo.GetAll().Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Id.ToString()
            })
        };

        roleViewModel.ApplicationUser.Role = _userManager.GetRolesAsync(applicationUser)
            .GetAwaiter().GetResult().FirstOrDefault();
        return View(roleViewModel);
    }

    [HttpPost]
    public IActionResult RoleManagement(RoleViewModel roleManagementViewModel)
    {
        var oldRole = _userManager
            .GetRolesAsync(_unitOfWork.ApplicationUserRepo.Get(u => u.Id == roleManagementViewModel.ApplicationUser.Id))
            .GetAwaiter().GetResult().FirstOrDefault();

        var applicationUser =
            _unitOfWork.ApplicationUserRepo.Get(u => u.Id == roleManagementViewModel.ApplicationUser.Id);


        if (roleManagementViewModel.ApplicationUser.Role != oldRole)
        {
            //a role was updated
            if (roleManagementViewModel.ApplicationUser.Role == SD.RoleCompany)
            {
                applicationUser.CompanyId = roleManagementViewModel.ApplicationUser.CompanyId;
            }

            if (oldRole == SD.RoleCompany)
            {
                applicationUser.CompanyId = null;
            }

            _unitOfWork.ApplicationUserRepo.Update(applicationUser);
            _unitOfWork.Save();

            _userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
            _userManager.AddToRoleAsync(applicationUser, roleManagementViewModel.ApplicationUser.Role).GetAwaiter().GetResult();
        }
        else
        {
            if (oldRole == SD.RoleCompany && applicationUser.CompanyId != roleManagementViewModel.ApplicationUser.CompanyId)
            {
                applicationUser.CompanyId = roleManagementViewModel.ApplicationUser.CompanyId;
                _unitOfWork.ApplicationUserRepo.Update(applicationUser);
                _unitOfWork.Save();
            }
        }

        return RedirectToAction("Index");
    }


    #region API CALLS

    [HttpGet]
    public IActionResult GetAll()
    {
        var objUserList = _unitOfWork.ApplicationUserRepo.GetAll(includeProperties: "Company").ToList();

        foreach (var user in objUserList)
        {
            user.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();

            if (user.Company == null)
            {
                user.Company = new Company()
                {
                    Name = ""
                };
            }
        }

        return Json(new {data = objUserList});
    }


    [HttpPost]
    public IActionResult LockUnlock([FromBody] string id)
    {
        var objFromDb = _unitOfWork.ApplicationUserRepo.Get(u => u.Id == id);
        if (objFromDb == null)
        {
            return Json(new {success = false, message = "Error while Locking/Unlocking"});
        }

        if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
        {
            //user is currently locked and we need to unlock them
            objFromDb.LockoutEnd = DateTime.Now;
        }
        else
        {
            objFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
        }

        _unitOfWork.ApplicationUserRepo.Update(objFromDb);
        _unitOfWork.Save();
        return Json(new {success = true, message = "Operation Successful"});
    }

    #endregion
}