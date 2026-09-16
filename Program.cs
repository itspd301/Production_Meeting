using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProductionMeeting.Data;
using ProductionMeeting.Helpers;
using ProductionMeeting.Middleware;
using ProductionMeeting.Models;
using ProductionMeeting.Services.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Every unsafe HTTP verb (POST/PUT/DELETE), including AJAX/fetch, is anti-forgery validated by default.
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// IdentityCore only (not the full AddIdentity): we need the Users/Roles store and
// UserManager/RoleManager for administering accounts, but authentication itself is
// handled by Windows Authentication (Negotiate) below, not passwords or cookies.
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = false;
        // Windows usernames are "DOMAIN\user" - Identity's default allowed-character set has
        // no backslash and silently rejects account creation otherwise.
        options.User.AllowedUserNameCharacters += "\\";
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services
    .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

// Negotiate proves the Windows identity; this maps it to the app's users/roles on every request.
builder.Services.AddTransient<IClaimsTransformation, WindowsUserClaimsTransformation>();

builder.Services.AddScoped<ProductionMeeting.Services.IDashboardService, ProductionMeeting.Services.DashboardService>();
builder.Services.AddScoped<ProductionMeeting.Services.IUserAccessService, ProductionMeeting.Services.UserAccessService>();
builder.Services.AddScoped<ProductionMeeting.Services.IProductionMeetingService, ProductionMeeting.Services.ProductionMeetingService>();
builder.Services.AddScoped<ProductionMeeting.Services.IUserManagementService, ProductionMeeting.Services.UserManagementService>();
builder.Services.AddScoped<ProductionMeeting.Services.IKpiMasterService, ProductionMeeting.Services.KpiMasterService>();
builder.Services.AddScoped<ProductionMeeting.Services.IMasterDataService, ProductionMeeting.Services.MasterDataService>();
builder.Services.AddScoped<ProductionMeeting.Services.IReportService, ProductionMeeting.Services.ReportService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.RequireAdmin, p => p.RequireRole(Roles.Admin));
    options.AddPolicy(PolicyNames.RequireManagerOrAbove, p => p.RequireRole(Roles.Admin, Roles.Manager));
    options.AddPolicy(PolicyNames.RequireProductionUserOrAbove, p => p.RequireRole(Roles.Admin, Roles.Manager, Roles.ProductionUser));
    options.AddPolicy(PolicyNames.RequireViewerOrAbove, p => p.RequireRole(Roles.Admin, Roles.Manager, Roles.ProductionUser, Roles.Viewer));

    // Every controller/action requires a signed-in (Windows-authenticated) user unless
    // explicitly marked [AllowAnonymous]. Being authenticated does not by itself grant any
    // role - see WindowsUserClaimsTransformation for how roles get attached.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    await RoleSeeder.SeedAsync(scope.ServiceProvider);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Windows Authentication has no login page: an unauthenticated request gets a 401 challenge
// that the browser answers transparently with the user's Windows credentials (often a second
// 401 round-trip for NTLM/Kerberos) - that handshake must reach the client untouched, so only
// 403 (wrong role) and 404 get redirected to a friendly page. UseStatusCodePagesWithReExecute
// has no such filter and would swallow the WWW-Authenticate challenge header, breaking auth.
app.UseStatusCodePages(context =>
{
    var response = context.HttpContext.Response;
    if (response.StatusCode is StatusCodes.Status403Forbidden or StatusCodes.Status404NotFound)
    {
        response.Redirect($"/Account/AccessDenied?statusCode={response.StatusCode}");
    }
    return Task.CompletedTask;
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
