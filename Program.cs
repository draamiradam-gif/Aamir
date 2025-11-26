using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QuestPDF.Infrastructure;
using StudentManagementSystem.Data;
using StudentManagementSystem.Services;
using Microsoft.AspNetCore.SignalR;
using StudentManagementSystem.Hubs;

// using StudentManagementSystem.Hubs; // ✅ COMMENTED OUT TEMPORARILY

var builder = WebApplication.CreateBuilder(args);
ExcelPackage.License.SetNonCommercialOrganization("Student Management System");

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Simple password requirements for development
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 3;

    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

// ADD THIS - Session configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register your custom services - ALL SERVICE REGISTRATIONS MUST BE HERE
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IQRCodeService, QRCodeService>();

// ✅ ENHANCED: Grade Management Services
builder.Services.AddScoped<IGradeService, GradeService>();
builder.Services.AddScoped<IEmailService, EmailService>(); // For grade notifications
builder.Services.AddScoped<IReportService, ReportService>(); // For grade exports

// ✅ NEW: SignalR for real-time grade updates (TEMPORARILY COMMENTED)
builder.Services.AddSignalR();

// ✅ NEW: HTTP Client for external integrations
builder.Services.AddHttpClient();

// ✅ NEW: Additional academic services
builder.Services.AddScoped<IAcademicWarningService, AcademicWarningService>();
builder.Services.AddScoped<ITranscriptService, TranscriptService>();

// Your existing services continue...
builder.Services.AddScoped<ISemesterService, SemesterService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<EnrollmentService>();

builder.Services.AddScoped<IComprehensiveGradeService, ComprehensiveGradeService>();

builder.Services.AddLogging();

// Kestrel configuration
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(configureOptions =>
    {
        configureOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
    });
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
});

// Set QuestPDF license
QuestPDF.Settings.License = LicenseType.Community;

// NOW build the app
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// IMPORTANT: Add Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// ✅ NEW: Add SignalR hub mapping (TEMPORARILY COMMENTED)
app.MapHub<GradeHub>("/gradeHub");

app.UseExceptionHandler("/error");
app.UseStatusCodePagesWithReExecute("/error/{0}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Add this before app.Run()
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Add detailed logging
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        // Log the exception
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An unhandled exception occurred during the request.");

        // Re-throw to let the exception handler middleware handle it
        throw;
    }
});

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        EnrollmentDataSeeder.SeedEnrollmentData(context);
        Console.WriteLine("Enrollment data seeding completed successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Enrollment data seeding failed: {ex.Message}");
    }
}

app.Run();