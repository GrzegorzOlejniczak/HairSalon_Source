using HairSalonApp.Data;
using HairSalonApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedRolesAsync(services); // Wywo³aj funkcjê seeduj¹c¹ role
        await SeedDefaultHairdresserAsync(services); // Dodaj fryzjera
        await SeedDefaultServicesAsync(services); // Dodaj us³ugi
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during seeding.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();


async Task SeedRolesAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Lista ról do utworzenia
    var roles = new[] { "User", "Hairdresser" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}
async Task SeedDefaultHairdresserAsync(IServiceProvider serviceProvider)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Dane pierwszego fryzjera
    var hairdresser1Email = "jan.kowalski@gmail.com";
    var hairdresser1Password = "zaq1@WSX";

    if (await userManager.FindByEmailAsync(hairdresser1Email) == null)
    {
        var user1 = new ApplicationUser
        {
            UserName = hairdresser1Email,
            Email = hairdresser1Email,
            Firstname = "Jan",
            Lastname = "Kowalski",
            EmailConfirmed = true,
            ProfilePicturePath = "/images/profiles/a107b862-9767-402c-a418-e0d9e14abdbf.png"
        };

        var result1 = await userManager.CreateAsync(user1, hairdresser1Password);
        if (result1.Succeeded)
        {
            await userManager.AddToRoleAsync(user1, "Hairdresser");
        }
    }

    // Dane drugiego fryzjera
    var hairdresser2Email = "anna.kowalska@gmail.com";
    var hairdresser2Password = "zaq1@WSX";

    if (await userManager.FindByEmailAsync(hairdresser2Email) == null)
    {
        var user2 = new ApplicationUser
        {
            UserName = hairdresser2Email,
            Email = hairdresser2Email,
            Firstname = "Anna",
            Lastname = "Kowalska",
            EmailConfirmed = true,
            ProfilePicturePath = "/images/profiles/06215bec-f34d-49d3-91be-245731599539.png"
        };

        var result2 = await userManager.CreateAsync(user2, hairdresser2Password);
        if (result2.Succeeded)
        {
            await userManager.AddToRoleAsync(user2, "Hairdresser");
        }
    }
}


// Seedowanie us³ug
async Task SeedDefaultServicesAsync(IServiceProvider serviceProvider)
{
    var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

    // Lista us³ug do dodania
    var services = new[]
    {
        new Service { Name = "Strzy¿enie mêskie", Duration = 60, Price = 50 },
        new Service { Name = "Strzy¿enie damskie", Duration = 120, Price = 100 },
        new Service { Name = "Koloryzacja w³osów", Duration = 180, Price = 200 },
        new Service { Name = "Pielêgnacja w³osów", Duration = 60, Price = 80 }
    };

    foreach (var service in services)
    {
        if (!await context.Service.AnyAsync(s => s.Name == service.Name))
        {
            context.Service.Add(service);
        }
    }

    await context.SaveChangesAsync();
}
