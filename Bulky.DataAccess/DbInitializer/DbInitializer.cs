using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.DbInitializer;

public class DbInitializer : IDbInitializer
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _db;

    public DbInitializer(
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        ApplicationDbContext db)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _db = db;
    }

    public void Initialize()
    {
        //migrations if they are not applied
        try
        {
            if (_db.Database.GetPendingMigrations().Any())
            {
                _db.Database.Migrate();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        //create roles if not already created
        if (!_roleManager.RoleExistsAsync(SD.RoleCustomer).GetAwaiter().GetResult())
        {
            _roleManager.CreateAsync(new IdentityRole(SD.RoleCustomer)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.RoleEmployee)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.RoleAdmin)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.RoleCompany)).GetAwaiter().GetResult();
            
            //if roles are not created, then create admin user as well
            _userManager.CreateAsync(new ApplicationUser {
                UserName = "Alper",
                Email = "admin@bulkyweb.com",
                Name = "Alper Arıçay",
                PhoneNumber = "1112223333",
                StreetAddress = "test 123 Ave",
                State = "IL",
                PostalCode = "23422",
                City = "Chicago"
            }, "Admin123*").GetAwaiter().GetResult();


            var user = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "admin@bulkyweb.com");
            _userManager.AddToRoleAsync(user, SD.RoleAdmin).GetAwaiter().GetResult();
        }
    }
}