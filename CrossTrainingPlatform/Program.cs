using CrossTrainingPlatform.Services.DatabaseServices;

var builder = WebApplication.CreateBuilder(args);

// necesar pentru Session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// MVC
builder.Services.AddControllersWithViews();

// Acces date SQL Server
builder.Services.AddSingleton<SqlConnectionFactory>();
builder.Services.AddScoped<AppUserService>();
builder.Services.AddScoped<AppRoleService>();
builder.Services.AddScoped<AppClaimService>();
builder.Services.AddScoped<DomainService>();
builder.Services.AddScoped<GameService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// activeaza sesiunea 
app.UseSession();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=UserAuth}/{action=Register}/{id?}");

app.Run();
