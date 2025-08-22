using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Bulky.DataAccess.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Bulky.Utility;
using Stripe;
using Bulky.DataAccess.DbInitializer;

namespace BulkyWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<Bulky.DataAccess.Data.ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("stripe"));

            builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                // Configure password requirements (for development - adjust for production)
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;
            }).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LogoutPath = $"/Identity/Account/Logout";
                options.LoginPath = $"/Identity/Account/Login";
                options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
            });
            builder.Services.AddAuthentication().AddFacebook(options =>
            {
                options.AppId = "1474590400632884";
                options.AppSecret = "3d7aeedfd7ee1a3f58346c98107c7891";



            });
            builder.Services.AddScoped<IDbInitialize, DbInitialize>();

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IEmailSender, EmailSender>();



            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            using (var scope = app.Services.CreateScope())
            {
                var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitialize>();
                dbInitializer.Initialize();
            }

            StripeConfiguration.ApiKey = builder.Configuration.GetSection("stripe:SecretKey").Get<string>();
            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();
            app.MapRazorPages();
            app.Run();

        }
    }
}
