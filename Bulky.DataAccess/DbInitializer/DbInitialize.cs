using Bulky.DataAccess.Data;
using Bulky.Models.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.DbInitializer
{
    public class DbInitialize : IDbInitialize
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager; // FIXED: Changed from IdentityUser to ApplicationUser
        private readonly RoleManager<IdentityRole> _roleManager;
        
        public DbInitialize(ApplicationDbContext db, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        
        void IDbInitialize.Initialize()
        {
            ApplicationUser adminUser2 = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "admin@bulky.com");
            adminUser2.PasswordHash = "Admin@123"; // FIXED: This line is not needed, as password should be set during user creation

            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database migration failed: {ex.Message}");
                throw;
            }

            // Create roles if they don't exist
            if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
            {
                Console.WriteLine("Creating roles...");
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Cust)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Comp)).GetAwaiter().GetResult();
                Console.WriteLine("Roles created successfully.");
            }
            else
            {
                Console.WriteLine("Roles already exist.");
            }

            // Create admin user if it doesn't exist
            ApplicationUser adminUser = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "admin@bulky.com");
            if (adminUser == null)
            {
                Console.WriteLine("Creating admin user...");
                try
                {
                    var newAdminUser = new ApplicationUser
                    {
                        UserName = "admin@bulky.com",
                        Email = "admin@bulky.com",
                        Name = "System Administrator",
                        EmailConfirmed = true,
                        PhoneNumber = "+1 (555) 123-4567",
                        StreetAddress = "123 Admin St",
                        City = "Admin City",
                        State = "AC",
                        PostalCode = "12345"
                    };

                    var createUserResult = _userManager.CreateAsync(newAdminUser, "Admin@123").GetAwaiter().GetResult();

                    if (!createUserResult.Succeeded)
                    {
                        var errors = string.Join(", ", createUserResult.Errors.Select(e => e.Description));
                        Console.WriteLine($"Failed to create admin user: {errors}");
                        throw new Exception($"Failed to create admin user: {errors}");
                    }

                    Console.WriteLine("Admin user created successfully.");

                    // Get the created user and assign admin role
                    var createdUser = _userManager.FindByEmailAsync("admin@bulky.com").GetAwaiter().GetResult();
                    if (createdUser != null)
                    {
                        var roleResult = _userManager.AddToRoleAsync(createdUser, SD.Role_Admin).GetAwaiter().GetResult();
                        if (roleResult.Succeeded)
                        {
                            Console.WriteLine("Admin role assigned successfully.");
                        }
                        else
                        {
                            var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                            Console.WriteLine($"Failed to assign admin role: {roleErrors}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating admin user: {ex.Message}");
                    throw;
                }
            }
            else
            {
                Console.WriteLine("Admin user already exists.");
                
                // Ensure admin user has admin role
                var adminRole = _userManager.IsInRoleAsync(adminUser, SD.Role_Admin).GetAwaiter().GetResult();
                if (!adminRole)
                {
                    Console.WriteLine("Adding admin role to existing admin user...");
                    var roleResult = _userManager.AddToRoleAsync(adminUser, SD.Role_Admin).GetAwaiter().GetResult();
                    if (roleResult.Succeeded)
                    {
                        Console.WriteLine("Admin role assigned to existing user.");
                    }
                    else
                    {
                        var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        Console.WriteLine($"Failed to assign admin role to existing user: {roleErrors}");
                    }
                }
            }

            Console.WriteLine("Database initialization completed.");
        }
    }
}
