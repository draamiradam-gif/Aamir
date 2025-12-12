using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using QuestPDF.Infrastructure;
using StudentManagementSystem.Controllers;
using StudentManagementSystem.Data;
using StudentManagementSystem.Hubs;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;

var builder = WebApplication.CreateBuilder(args);
ExcelPackage.License.SetNonCommercialOrganization("Student Management System");

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity Configuration
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddRoles<IdentityRole>();

// Add Identity UI for Razor Pages
builder.Services.AddRazorPages();

// Session configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "StudentManagement.Session";
});

builder.Services.AddControllersWithViews();

// Register services
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IQRCodeService, QRCodeService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IGradeService, GradeService>();
builder.Services.AddScoped<ISemesterService, SemesterService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IGradingService, GradingService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IEmailService, EmailService>();


// Remove duplicate IStudentService registration
// builder.Services.AddScoped<IStudentService, StudentService>();

// Email configuration
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Remove unconditional EmailService registration
// builder.Services.AddTransient<IEmailService, EmailService>();

// Conditional Email Service registration
if (builder.Environment.IsDevelopment())
{
    // Use MockEmailService in development (logs to file instead of sending real emails)
    builder.Services.AddTransient<IEmailService, MockEmailService>();
}
else
{
    // Use real EmailService in production
    builder.Services.AddTransient<IEmailService, EmailService>();
}

builder.Services.AddMemoryCache();
builder.Services.AddLogging();
builder.Services.AddSignalR();

// Set QuestPDF license
QuestPDF.Settings.License = LicenseType.Community;

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Simple role-based policies
    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireRole("SuperAdmin"));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin", "SuperAdmin"));

    options.AddPolicy("UniversityAdminOnly", policy =>
        policy.RequireRole("UniversityAdmin", "SuperAdmin"));

    options.AddPolicy("FacultyAdminOnly", policy =>
        policy.RequireRole("FacultyAdmin", "UniversityAdmin", "SuperAdmin"));

    options.AddPolicy("DepartmentAdminOnly", policy =>
        policy.RequireRole("DepartmentAdmin", "FacultyAdmin", "UniversityAdmin", "SuperAdmin"));

    options.AddPolicy("AdminAccess", policy =>
        policy.RequireRole("Admin", "SuperAdmin", "UniversityAdmin", "FacultyAdmin", "DepartmentAdmin", "FinanceAdmin", "StudentAdmin"));
});

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.LoginPath = "/Home/AdminLogin";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.SlidingExpiration = true;
    options.Cookie.Name = "StudentManagement.Auth";
});

var app = builder.Build();

// Error handling
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Database seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // Ensure database exists
        await context.Database.EnsureCreatedAsync();
        logger.LogInformation("✅ Database ensured/created");

        // 1. Seed roles
        string[] roleNames = { "SuperAdmin", "Admin", "UniversityAdmin", "FacultyAdmin", "DepartmentAdmin", "FinanceAdmin", "StudentAdmin", "Faculty", "Student" };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                logger.LogInformation($"✅ Created role: {roleName}");
            }
            else
            {
                logger.LogInformation($"ℹ️ Role already exists: {roleName}");
            }
        }

        // 2. Seed admin users
        // SuperAdmin
        var superAdminEmail = "superadmin@localhost";
        var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);

        if (superAdmin == null)
        {
            superAdmin = new IdentityUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(superAdmin, "SuperAdmin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                logger.LogInformation("✅ SuperAdmin user created");
            }
            else
            {
                logger.LogError($"❌ Failed to create SuperAdmin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            logger.LogInformation("ℹ️ SuperAdmin user already exists");
        }

        // Regular Admin
        var adminEmail = "admin@localhost";
        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin == null)
        {
            admin = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                logger.LogInformation("✅ Admin user created");
            }
            else
            {
                logger.LogError($"❌ Failed to create Admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            logger.LogInformation("ℹ️ Admin user already exists");
        }

        // 3. Seed permissions (if you have a permission service)
        try
        {
            await DataSeeder.SeedPermissionsAsync(services);
            logger.LogInformation("✅ Permissions seeded");
        }
        catch (Exception ex)
        {
            logger.LogWarning($"⚠️ Permission seeding skipped or failed: {ex.Message}");
        }

        // 4. Seed templates
        try
        {
            await DataSeeder.SeedDefaultTemplates(context);
            logger.LogInformation("✅ Templates seeded");

            // Verify templates were created
            var templateCount = await context.AdminPrivilegeTemplates.CountAsync();
            logger.LogInformation($"📊 Total templates in database: {templateCount}");

            if (templateCount > 0)
            {
                var templates = await context.AdminPrivilegeTemplates
                    .Take(5)
                    .Select(t => t.TemplateName)
                    .ToListAsync();
                logger.LogInformation($"📝 Sample templates: {string.Join(", ", templates)}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error seeding templates");
        }

        logger.LogInformation("🎉 All database seeding completed successfully!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Database initialization failed");
    }
}

// ======= MIGRATION CHECK SECTION (ADDED) ======= 
//using (var scope = app.Services.CreateScope())
//{
//    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
//    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

//    try
//    {
//        logger.LogInformation("🔍 Checking migration status...");

//        // Check for pending migrations
//        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

//        if (pendingMigrations.Any())
//        {
//            logger.LogWarning($"⚠️ FOUND {pendingMigrations.Count()} PENDING MIGRATIONS:");
//            foreach (var migration in pendingMigrations)
//            {
//                logger.LogWarning($"  ⚠️ {migration}");
//            }
//            logger.LogInformation("To apply migrations, run: dotnet ef database update");
//        }
//        else
//        {
//            logger.LogInformation("✅ CONFIRMED: NO PENDING MIGRATIONS");
//        }

//        // Show applied migrations
//        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
//        logger.LogInformation($"📊 Total applied migrations: {appliedMigrations.Count()}");

//        if (appliedMigrations.Any())
//        {
//            logger.LogInformation("📋 Applied migrations list:");
//            foreach (var migration in appliedMigrations)
//            {
//                logger.LogInformation($"  📌 {migration}");
//            }
//        }
//        else
//        {
//            logger.LogWarning("⚠️ NO MIGRATIONS HAVE BEEN APPLIED YET");
//        }

//        logger.LogInformation("🔍 Migration check completed");
//    }
//    catch (Exception ex)
//    {
//        logger.LogError(ex, "❌ Error checking migrations");
//    }
//}

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        logger.LogInformation("🔧 Checking for unapplied migrations...");

        // Get all migration files from assembly
        var migrationFiles = context.Database.GetMigrations();
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

        var pending = migrationFiles.Except(appliedMigrations).ToList();

        if (pending.Any())
        {
            logger.LogWarning($"🔧 Found {pending.Count()} unapplied migration(s):");
            foreach (var migration in pending)
            {
                logger.LogWarning($"  🔧 {migration}");
            }

            logger.LogInformation("Applying migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("✅ Migrations applied successfully");
        }
        else
        {
            logger.LogInformation("✅ All migrations are already applied");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error applying migrations");
    }
}
// ======= END OF MIGRATION CHECK SECTION =======

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=PortalAccess}/{id?}");

app.MapRazorPages();

// Force logout endpoint
app.MapGet("/force-logout", () =>
{
    return Results.Redirect("/logout/force-logout");
});

app.Run();