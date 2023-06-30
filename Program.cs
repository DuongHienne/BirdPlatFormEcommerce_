
using BirdPlatFormEcommerce.DEntity;
using BirdPlatFormEcommerce.Helper;
using BirdPlatFormEcommerce.Helper.Mail;
using BirdPlatFormEcommerce.Order;
using BirdPlatFormEcommerce.Payment;
using BirdPlatFormEcommerce.Product;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

namespace BirdPlatFormEcommerce
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<DataswpContext>(op =>
            {
                op.UseSqlServer(builder.Configuration.GetConnectionString("BirdForm"));
            });

            builder.Services.AddCors(option => option.AddPolicy("BirdPlatform", build =>
            {
                build.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
            }));
            builder.Services.AddControllers()
                .AddJsonOptions(opt => opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddScoped<IHomeViewProductService, HomeViewProductService>();
            builder.Services.AddScoped<IManageProductService, ManageProductService>();
            builder.Services.AddAutoMapper(typeof(Mappings).Assembly);
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IMailService, MailService>();

            // HttpContext
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // VnPay method
            builder.Services.AddScoped<IVnPayService, VnPayService>();


            builder.Services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                
            }).AddJwtBearer(options => {
                options.SaveToken = true;
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JWT:Audience"],
                    ValidIssuer = builder.Configuration["JWT:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]))
                };
            });

            // Automapper


            var app = builder.Build();
            app.UseStaticFiles();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            
            app.UseCors("BirdPlatform");
            app.MapControllers();

            app.Run();
        }
    }
}