using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using multi_tenant_beauty_platform_back.Application.Services;
using multi_tenant_beauty_platform_back.Domain.Repositories;
using multi_tenant_beauty_platform_back.Domain.Services;
using multi_tenant_beauty_platform_back.Infrastructure.Authentication;
using multi_tenant_beauty_platform_back.Infrastructure.Data;
using multi_tenant_beauty_platform_back.Infrastructure.Repositories;
using multi_tenant_beauty_platform_back.Presentation.Endpoints;
using multi_tenant_beauty_platform_back.Presentation.Middlewares;
using multi_tenant_beauty_platform_back.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("api", new() 
    { 
        Title = "Multi-Tenant Beauty Platform API - Clean Architecture", 
        Version = "api",
        Description = "Enterprise-grade Clean Architecture API supporting multi-tenant beauty platform operations with JWT Authentication."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("BeautyPlatformCleanDb"));

builder.Services.AddScoped<IOnboardingRepository, OnboardingRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IServiceCategoryRepository, ServiceCategoryRepository>();
builder.Services.AddScoped<ISpecialistRepository, SpecialistRepository>();
builder.Services.AddScoped<ISalonRepository, SalonRepository>();

builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IServiceCategoryService, ServiceCategoryService>();
builder.Services.AddScoped<ISpecialistService, SpecialistService>();
builder.Services.AddScoped<ISalonService, SalonService>();
builder.Services.AddScoped<IListingService, ListingService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/api/swagger.json", "Multi-Tenant Beauty Platform API"));
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapOnboardingEndpoints();
app.MapAuthEndpoints();
app.MapServiceCategoryEndpoints();
app.MapSpecialistEndpoints();
app.MapSalonEndpoints();
app.MapListingEndpoints();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var usersCount = context.Users.Count();
    Console.WriteLine($"Users Count: {usersCount}");
    if (!context.ServiceCategories.Any())
    {
        context.ServiceCategories.AddRange(
            new ServiceCategory("Hair"),
            new ServiceCategory("Nails"),
            new ServiceCategory("Makeup"),
            new ServiceCategory("Skincare"),
            new ServiceCategory("Massage")
        );
        context.SaveChanges();
        Console.WriteLine("Seeded 5 service categories.");
    }
}

app.Run();
