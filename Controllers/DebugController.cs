using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Services;

namespace StudentManagementSystem.Controllers
{
    public class DebugController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;

        public DebugController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IPermissionService permissionService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _permissionService = permissionService;
        }

        // GET: /Debug/CheckUsers
        public async Task<IActionResult> CheckUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userInfo = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userInfo.Add(new
                {
                    UserId = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = string.Join(", ", roles)
                });
            }

            return Json(new
            {
                TotalUsers = users.Count,
                Users = userInfo
            });
        }

        // GET: /Debug/CheckRoles
        public async Task<IActionResult> CheckRoles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return Json(roles.Select(r => new { r.Id, r.Name }));
        }

        // GET: /Debug/CreateAdmin
        public async Task<IActionResult> CreateAdmin(string email = "admin@localhost", string password = "Admin123!")
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return Json(new
                {
                    success = false,
                    message = $"User {email} already exists",
                    existingUser = new { existingUser.Id, existingUser.Email }
                });
            }

            // Create new admin user
            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // Ensure Admin role exists
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                }

                // Ensure SuperAdmin role exists
                if (!await _roleManager.RoleExistsAsync("SuperAdmin"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
                }

                // Add to roles
                await _userManager.AddToRoleAsync(user, "Admin");
                await _userManager.AddToRoleAsync(user, "SuperAdmin");

                return Json(new
                {
                    success = true,
                    message = $"Admin user created successfully!",
                    credentials = new { email, password },
                    loginUrl = "/Identity/Account/Login"
                });
            }

            return Json(new
            {
                success = false,
                message = "Failed to create admin user",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        // GET: /Debug/MakeUserAdmin?email=user@example.com
        public async Task<IActionResult> MakeUserAdmin(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Json(new { success = false, message = $"User {email} not found" });
            }

            // Ensure Admin role exists
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            await _userManager.AddToRoleAsync(user, "Admin");

            var roles = await _userManager.GetRolesAsync(user);
            return Json(new
            {
                success = true,
                message = $"User {email} is now an admin",
                currentRoles = roles
            });
        }

        // GET: /Debug/ResetAdmin
        public async Task<IActionResult> ResetAdmin()
        {
            var email = "admin@localhost";

            // Delete existing admin if exists
            var existingAdmin = await _userManager.FindByEmailAsync(email);
            if (existingAdmin != null)
            {
                await _userManager.DeleteAsync(existingAdmin);
            }

            // Create new admin
            var adminUser = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                // Ensure roles exist
                if (!await _roleManager.RoleExistsAsync("Admin"))
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                if (!await _roleManager.RoleExistsAsync("SuperAdmin"))
                    await _roleManager.CreateAsync(new IdentityRole("SuperAdmin"));

                await _userManager.AddToRoleAsync(adminUser, "Admin");
                await _userManager.AddToRoleAsync(adminUser, "SuperAdmin");

                return Json(new
                {
                    success = true,
                    message = "Admin user reset successfully!",
                    credentials = new { email, password = "Admin123!" }
                });
            }

            return Json(new
            {
                success = false,
                message = "Failed to reset admin user",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        // GET: /Debug/CheckPermissions
        public async Task<IActionResult> CheckPermissions()
        {
            var permissions = await _permissionService.GetAllPermissionsAsync();
            var roles = await _roleManager.Roles.ToListAsync();

            var rolePermissionsInfo = new List<object>();
            foreach (var role in roles)
            {
                // Get users in this role
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

                rolePermissionsInfo.Add(new
                {
                    Role = role.Name,
                    UserCount = usersInRole.Count,
                    Users = usersInRole.Select(u => u.Email)
                });
            }

            return Json(new
            {
                TotalPermissions = permissions.Count,
                Permissions = permissions.Select(p => new { p.Id, p.Name, p.Category, p.Description }),
                Roles = rolePermissionsInfo
            });
        }

        // GET: /Debug/SeedPermissions
        public async Task<IActionResult> SeedPermissions()
        {
            try
            {
                await _permissionService.SeedDefaultPermissionsAsync();
                return Json(new
                {
                    success = true,
                    message = "Permissions seeded successfully!"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Failed to seed permissions: {ex.Message}"
                });
            }
        }

        // GET: /Debug/TestAdminAccess
        public IActionResult TestAdminAccess()
        {
            return Json(new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                UserName = User.Identity?.Name,
                IsInRoleAdmin = User.IsInRole("Admin"),
                IsInRoleSuperAdmin = User.IsInRole("SuperAdmin"),
                Claims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }

        // GET: /Debug/CreateTestUsers
        public async Task<IActionResult> CreateTestUsers()
        {
            var testUsers = new[]
            {
                new { Email = "admin@localhost", Password = "Admin123!", Roles = new[] { "Admin", "SuperAdmin" } },
                new { Email = "faculty@localhost", Password = "Faculty123!", Roles = new[] { "Faculty" } },
                new { Email = "student@localhost", Password = "Student123!", Roles = new[] { "Student" } },
                new { Email = "user@localhost", Password = "User123!", Roles = new string[0] }
            };

            var results = new List<object>();

            foreach (var testUser in testUsers)
            {
                var existingUser = await _userManager.FindByEmailAsync(testUser.Email);
                if (existingUser != null)
                {
                    results.Add(new
                    {
                        email = testUser.Email,
                        status = "Already exists",
                        roles = await _userManager.GetRolesAsync(existingUser)
                    });
                    continue;
                }

                var user = new IdentityUser
                {
                    UserName = testUser.Email,
                    Email = testUser.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, testUser.Password);
                if (result.Succeeded)
                {
                    // Ensure roles exist
                    foreach (var role in testUser.Roles)
                    {
                        if (!await _roleManager.RoleExistsAsync(role))
                        {
                            await _roleManager.CreateAsync(new IdentityRole(role));
                        }
                        await _userManager.AddToRoleAsync(user, role);
                    }

                    results.Add(new
                    {
                        email = testUser.Email,
                        status = "Created",
                        password = testUser.Password,
                        roles = testUser.Roles
                    });
                }
                else
                {
                    results.Add(new
                    {
                        email = testUser.Email,
                        status = "Failed",
                        errors = result.Errors.Select(e => e.Description)
                    });
                }
            }

            return Json(new { TestUsers = results });
        }
    }
}