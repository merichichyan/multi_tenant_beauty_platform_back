using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Application.Services;
using multi_tenant_beauty_platform_back.Domain.Repositories;
using multi_tenant_beauty_platform_back.Infrastructure.Data;
using multi_tenant_beauty_platform_back.Infrastructure.Repositories;
using multi_tenant_beauty_platform_back.Presentation.Endpoints;
using multi_tenant_beauty_platform_back.Presentation.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Presentation & OpenAPI / Swagger Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Multi-Tenant Beauty Platform API - Clean Architecture", 
        Version = "v1",
        Description = "Enterprise-grade Clean Architecture API supporting multi-tenant beauty platform operations."
    });
});

// 2. Add Global Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 3. Add Infrastructure Services (Database & Repositories)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("BeautyPlatformCleanDb"));

builder.Services.AddScoped<IOnboardingRepository, OnboardingRepository>();

// 4. Add Application Services
builder.Services.AddScoped<IOnboardingService, OnboardingService>();

// 5. Configure CORS
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

// Configure the HTTP request pipeline.
app.UseExceptionHandler(); // Uses GlobalExceptionHandler registered above

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Multi-Tenant Beauty Platform API v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Map Endpoints cleanly via Presentation Layer extension method
app.MapOnboardingEndpoints();

app.Run();
