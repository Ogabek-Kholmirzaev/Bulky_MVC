using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.DataAccess.Data.DBInitializer;


public class DBInitializer : IDBInitializer
{
    private UserManager<IdentityUser> _userManager;
    private RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _db;

    public DBInitializer(
        ApplicationDbContext db, 
        UserManager<IdentityUser> userManager, 
        RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public void Initialize()
    {
        try
        {
            #region Applying Migrations

            _db.Database.MigrateAsync().GetAwaiter().GetResult();

            #endregion

            #region Adding Roles

            if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();

            if (!_roleManager.RoleExistsAsync(SD.Role_Company).GetAwaiter().GetResult())
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();

            if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();

            if (!_roleManager.RoleExistsAsync(SD.Role_Employee).GetAwaiter().GetResult())
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();

            #endregion

            #region Seeding Admin

            var admin = new ApplicationUser
            {
                UserName = "admin@dotnetmastery.com",
                Email = "admin@dotnetmastery.com",
                Name = "John Doe",
                PhoneNumber = "1112223333",
                StreetAddress = "test 123 Ave",
                State = "IL",
                PostalCode = "23422",
                City = "Chicago"
            };

            var result = _userManager.CreateAsync(admin, "Admin123*").GetAwaiter().GetResult();

            if(result.Succeeded)
                _userManager.AddToRoleAsync(admin, SD.Role_Admin).GetAwaiter().GetResult();

            #endregion
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}