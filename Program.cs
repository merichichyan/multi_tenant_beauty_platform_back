using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using multi_tenant_beauty_platform_back.Application.Services;
using multi_tenant_beauty_platform_back.Domain.Repositories;
using multi_tenant_beauty_platform_back.Domain.Services;
using multi_tenant_beauty_platform_back.Infrastructure.Authentication;
using multi_tenant_beauty_platform_back.Infrastructure.BackgroundServices;
using multi_tenant_beauty_platform_back.Infrastructure.Data;
using multi_tenant_beauty_platform_back.Infrastructure.Repositories;
using multi_tenant_beauty_platform_back.Infrastructure.Services;
using multi_tenant_beauty_platform_back.Presentation.Endpoints;
using multi_tenant_beauty_platform_back.Presentation.Middlewares;
using multi_tenant_beauty_platform_back.Domain.Entities;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
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
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
builder.Services.AddScoped<IUserService, UserService>();

// OneSignal Push Notification Service
builder.Services.AddHttpClient<INotificationService, OneSignalNotificationService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Background service: silently removes bookings older than 3 months every 24 h
builder.Services.AddHostedService<BookingCleanupService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/api/swagger.json", "Multi-Tenant Beauty Platform API"));
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapOnboardingEndpoints();
app.MapAuthEndpoints();
app.MapServiceCategoryEndpoints();
app.MapSpecialistEndpoints();
app.MapSalonEndpoints();
app.MapListingEndpoints();
app.MapUserEndpoints();
app.MapBookingEndpoints();
app.MapLetterEndpoints();
app.MapFavoriteEndpoints();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        Console.WriteLine("Applying pending migrations...");
        context.Database.Migrate();
        Console.WriteLine("Database migrated successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration failed: {ex.Message}");
    }

    try
    {
        try
        {
            context.Database.ExecuteSqlRaw(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE LOWER(table_name) = 'staffmembers' AND LOWER(column_name) = 'status') THEN
                        ALTER TABLE ""StaffMembers"" ADD COLUMN ""Status"" TEXT NOT NULL DEFAULT 'Active';
                    END IF;
                END $$;");
        }
        catch (Exception)
        {
            // Column already exists or fallback safety
        }

        try
        {
            context.Database.ExecuteSqlRaw(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE LOWER(table_name) = 'staffmembers' AND LOWER(column_name) = 'specialistid') THEN
                        ALTER TABLE ""StaffMembers"" ADD COLUMN ""SpecialistId"" uuid NULL;
                    END IF;
                END $$;");
        }
        catch (Exception)
        {
            // Column already exists or fallback safety
        }

        try
        {
            context.Database.ExecuteSqlRaw(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE LOWER(table_name) = 'bookings' AND LOWER(column_name) = 'salonid') THEN
                        ALTER TABLE ""Bookings"" ADD COLUMN ""SalonId"" uuid NULL;
                    END IF;
                END $$;");
        }
        catch (Exception)
        {
            // Column already exists or fallback safety
        }

        try
        {
            context.Database.ExecuteSqlRaw(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE LOWER(table_name) = 'bookings' AND LOWER(column_name) = 'salonname') THEN
                        ALTER TABLE ""Bookings"" ADD COLUMN ""SalonName"" TEXT NULL;
                    END IF;
                END $$;");
        }
        catch (Exception)
        {
            // Column already exists or fallback safety
        }

        context.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""FavoriteSalons"" (
                ""Id"" uuid PRIMARY KEY,
                ""UserId"" uuid NOT NULL,
                ""SalonId"" uuid NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_FavoriteSalons_UserId_SalonId"" ON ""FavoriteSalons"" (""UserId"", ""SalonId"");

            CREATE TABLE IF NOT EXISTS ""FavoriteSpecialists"" (
                ""Id"" uuid PRIMARY KEY,
                ""UserId"" uuid NOT NULL,
                ""SpecialistId"" uuid NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_FavoriteSpecialists_UserId_SpecialistId"" ON ""FavoriteSpecialists"" (""UserId"", ""SpecialistId"");
        ");
        context.Database.ExecuteSqlRaw("UPDATE \"ServiceItems\" SET \"IsActive\" = true;");
        var usersCount = context.Users.Count();
        Console.WriteLine($"Users Count: {usersCount}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database not initialized or needs migration: {ex.Message}");
    }

    try
    {
        if (context.Database.GetPendingMigrations().Any())
        {
            Console.WriteLine("Pending migrations found. Seeding will run after migrations are applied.");
        }
        else
        {
            var existingCategories = context.ServiceCategories.ToList();
            if (!existingCategories.Any())
            {
                context.ServiceCategories.AddRange(
                    new ServiceCategory("Մազեր", "Волосы", "Hair"),
                    new ServiceCategory("Եղունգներ", "Ногти", "Nails"),
                    new ServiceCategory("Դիմահարդարում", "Макияж", "Makeup"),
                    new ServiceCategory("Մաշկի խնամք", "Уход за кожей", "Skincare"),
                    new ServiceCategory("Մերսում", "Массаж", "Massage")
                );
                context.SaveChanges();
                Console.WriteLine("Seeded 5 service categories.");
            }
            else
            {
                var updated = false;
                foreach (var cat in existingCategories)
                {
                    if (string.IsNullOrWhiteSpace(cat.NameHy) || string.IsNullOrWhiteSpace(cat.NameRu) || string.IsNullOrWhiteSpace(cat.NameEn))
                    {
                        var name = (new[] { cat.NameEn, cat.NameRu, cat.NameHy }.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "").Trim();
                        if (name.Equals("Hair", StringComparison.OrdinalIgnoreCase))
                        {
                            cat.Update("Մազեր", "Волосы", "Hair");
                            updated = true;
                        }
                        else if (name.Equals("Nails", StringComparison.OrdinalIgnoreCase))
                        {
                            cat.Update("Եղունգներ", "Ногти", "Nails");
                            updated = true;
                        }
                        else if (name.Equals("Makeup", StringComparison.OrdinalIgnoreCase))
                        {
                            cat.Update("Դիմահարդարում", "Макияж", "Makeup");
                            updated = true;
                        }
                        else if (name.Equals("Skincare", StringComparison.OrdinalIgnoreCase))
                        {
                            cat.Update("Մաշկի խնամք", "Уход за кожей", "Skincare");
                            updated = true;
                        }
                        else if (name.Equals("Massage", StringComparison.OrdinalIgnoreCase))
                        {
                            cat.Update("Մերսում", "Массаж", "Massage");
                            updated = true;
                        }
                    }
                }
                if (updated)
                {
                    context.SaveChanges();
                    Console.WriteLine("Updated existing service categories with localized translations.");
                }
            }
            var existingAdmin = context.Users.FirstOrDefault(u => u.Email == "merichichyan");
            if (existingAdmin == null)
            {
                var passwordHash = BCrypt.Net.BCrypt.HashPassword("Meri.12345");
                var admin = new User(
                    email: "merichichyan",
                    passwordHash: passwordHash,
                    fullName: "merichichyan",
                    role: "admin",
                    phone: "+37499000000"
                );
                context.Users.Add(admin);
                context.SaveChanges();
                Console.WriteLine("Seeded admin user 'merichichyan'.");
            }
            else
            {
                existingAdmin.UpdatePasswordHash(BCrypt.Net.BCrypt.HashPassword("Meri.12345"));
                existingAdmin.UpdateRole("admin");
                context.SaveChanges();
                Console.WriteLine("Updated admin user 'merichichyan' password and role.");
            }

            if (!context.Salons.Any())
            {
                var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123");
                var salon1 = new Salon(
                    email: "maison@noire.com",
                    passwordHash: passwordHash,
                    salonName: "Maison Noire",
                    ownerFullName: "Ani Petrosyan",
                    role: "Salon",
                    phone: "+37499111222",
                    deviceId: "salon_device_1",
                    address: "10 Abovyan St, Yerevan",
                    latitude: 40.1792,
                    longitude: 44.5152,
                    description: "Luxury hair & styling services in the heart of Yerevan.",
                    socialMedias: "instagram.com/maisonnoire",
                    logoUrl: "https://images.unsplash.com/photo-1560066984-138dadb4c035?w=500&auto=format&fit=crop",
                    preferredColors: "#1A1A1A,#D4AF37",
                    operatingHours: "10:00 - 20:00",
                    rating: 4.9,
                    startingPrice: 65,
                    availabilityStatus: "AVAILABLE TODAY"
                );
                
                var salon2 = new Salon(
                    email: "aura@veil.com",
                    passwordHash: passwordHash,
                    salonName: "Aura & Veil",
                    ownerFullName: "Nare Grigoryan",
                    role: "Salon",
                    phone: "+37499333444",
                    deviceId: "salon_device_2",
                    address: "24 Tumanyan St, Yerevan",
                    latitude: 40.1812,
                    longitude: 44.5182,
                    description: "A serene space dedicated to premium facials and skin wellness.",
                    socialMedias: "instagram.com/auraveil",
                    logoUrl: "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=500&auto=format&fit=crop",
                    preferredColors: "#0F172A,#F59E0B",
                    operatingHours: "09:00 - 21:00",
                    rating: 5.0,
                    startingPrice: 90,
                    availabilityStatus: "3 SLOTS LEFT"
                );

                var salon3 = new Salon(
                    email: "sage@studio.com",
                    passwordHash: passwordHash,
                    salonName: "Sage Studio",
                    ownerFullName: "Lilit Mkrtchyan",
                    role: "Salon",
                    phone: "+37499555666",
                    deviceId: "salon_device_3",
                    address: "5 Saryan St, Yerevan",
                    latitude: 40.1752,
                    longitude: 44.5102,
                    description: "Organic nails and holistic self-care studio.",
                    socialMedias: "instagram.com/sagestudio",
                    logoUrl: "https://images.unsplash.com/photo-1607613009820-a29f7bb81c04?w=500&auto=format&fit=crop",
                    preferredColors: "#022C22,#10B981",
                    operatingHours: "11:00 - 19:00",
                    rating: 4.7,
                    startingPrice: 55,
                    availabilityStatus: "BOOK FOR TOMORROW"
                );

                context.Salons.AddRange(salon1, salon2, salon3);
                context.SaveChanges();
                Console.WriteLine("Seeded 3 premium salons.");
            }

            var existingSalons = context.Salons.Include(s => s.StaffMembers).ToList();
            if (existingSalons.Any() && !context.StaffMembers.Any())
            {
                foreach (var salon in existingSalons)
                {
                    if (salon.SalonName.Contains("Maison Noire"))
                    {
                        var staff1 = new StaffMember(salon.Id, "Emily Watson", "Senior Hair Stylist", "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=150");
                        staff1.AddService(new ServiceItem("Balayage & Hair Cut", "Hair", 120, 120, staffMemberId: staff1.Id));
                        staff1.AddService(new ServiceItem("Blow Dry & Styling", "Hair", 40, 45, staffMemberId: staff1.Id));
                        context.StaffMembers.Add(staff1);
                    }
                    else if (salon.SalonName.Contains("Aura & Veil"))
                    {
                        var staff2 = new StaffMember(salon.Id, "Chloe Bennet", "Lead Esthetician", "https://images.unsplash.com/photo-1580489944761-15a19d654956?w=150");
                        staff2.AddService(new ServiceItem("HydraFacial Treatment", "Skincare", 95, 60, staffMemberId: staff2.Id));
                        staff2.AddService(new ServiceItem("Aromatherapy Massage", "Massage", 80, 60, staffMemberId: staff2.Id));
                        context.StaffMembers.Add(staff2);
                    }
                    else if (salon.SalonName.Contains("Sage Studio"))
                    {
                        var staff3 = new StaffMember(salon.Id, "Bella Thorne", "Nail Artist", "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150");
                        staff3.AddService(new ServiceItem("Classic Gel Manicure", "Nails", 45, 45, staffMemberId: staff3.Id));
                        staff3.AddService(new ServiceItem("Luxury Spa Pedicure", "Nails", 60, 60, staffMemberId: staff3.Id));
                        context.StaffMembers.Add(staff3);
                    }
                }
                context.SaveChanges();
                Console.WriteLine("Upgraded existing salons with staff members & services.");
            }

            var existingSpecs = context.Specialists.Include(s => s.Services).ToList();
            if (existingSpecs.Any())
            {
                var updated = false;
                foreach (var spec in existingSpecs)
                {
                    if (!spec.Services.Any())
                    {
                        if (spec.FullName.Contains("Alex Mercer"))
                        {
                            context.ServiceItems.Add(new ServiceItem("Men's Haircut & Styling", "Hair", 45, 45, specialistId: spec.Id));
                            context.ServiceItems.Add(new ServiceItem("Beard Trim & Clean Shave", "Hair", 25, 30, specialistId: spec.Id));
                            updated = true;
                        }
                        else if (spec.FullName.Contains("Maria Gonzalez"))
                        {
                            context.ServiceItems.Add(new ServiceItem("Custom Glow Facial", "Skincare", 80, 60, specialistId: spec.Id));
                            context.ServiceItems.Add(new ServiceItem("Chemical Peel Therapy", "Skincare", 120, 45, specialistId: spec.Id));
                            updated = true;
                        }
                        else if (spec.FullName.Contains("Sophia Loren"))
                        {
                            context.ServiceItems.Add(new ServiceItem("Gel Extension Full Set", "Nails", 70, 90, specialistId: spec.Id));
                            context.ServiceItems.Add(new ServiceItem("Premium Spa Manicure", "Nails", 50, 45, specialistId: spec.Id));
                            updated = true;
                        }
                    }
                }
                if (updated)
                {
                    context.SaveChanges();
                    Console.WriteLine("Upgraded existing specialists with services.");
                }
            }

            if (!context.Specialists.Any())
            {
                var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123");
                var spec1 = new Specialist(
                    email: "alex@mercer.com",
                    passwordHash: passwordHash,
                    fullName: "Alex Mercer",
                    role: "Specialist",
                    phone: "+37499777888",
                    deviceId: "spec_device_1",
                    address: "15 Sayat-Nova Ave, Yerevan",
                    latitude: 40.1800,
                    longitude: 44.5200,
                    description: "Master Barber and Hair Stylist with 10+ years experience.",
                    socialMedias: "instagram.com/alexmercer",
                    logoUrl: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=500&auto=format&fit=crop",
                    preferredColors: "#18181B,#F43F5E",
                    workingHours: "10:00 - 18:00",
                    rating: 4.95,
                    startingPrice: 45,
                    availabilityStatus: "AVAILABLE TODAY"
                );
                spec1.AddService(new ServiceItem("Men's Haircut & Styling", "Hair", 45, 45));
                spec1.AddService(new ServiceItem("Beard Trim & Clean Shave", "Hair", 25, 30));

                var spec2 = new Specialist(
                    email: "maria@gonzalez.com",
                    passwordHash: passwordHash,
                    fullName: "Maria Gonzalez",
                    role: "Specialist",
                    phone: "+37499888999",
                    deviceId: "spec_device_2",
                    address: "3 Nalbandyan St, Yerevan",
                    latitude: 40.1770,
                    longitude: 44.5120,
                    description: "Senior Medical Esthetician specializing in skin health.",
                    socialMedias: "instagram.com/mariagonzalez",
                    logoUrl: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=500&auto=format&fit=crop",
                    preferredColors: "#1C1917,#D97706",
                    workingHours: "09:00 - 17:00",
                    rating: 4.88,
                    startingPrice: 80,
                    availabilityStatus: "2 SLOTS LEFT"
                );
                spec2.AddService(new ServiceItem("Custom Glow Facial", "Skincare", 80, 60));
                spec2.AddService(new ServiceItem("Chemical Peel Therapy", "Skincare", 120, 45));

                var spec3 = new Specialist(
                    email: "sophia@loren.com",
                    passwordHash: passwordHash,
                    fullName: "Sophia Loren",
                    role: "Specialist",
                    phone: "+37499101010",
                    deviceId: "spec_device_3",
                    address: "12 Pushkin St, Yerevan",
                    latitude: 40.1785,
                    longitude: 44.5140,
                    description: "Luxury Nail Artist doing gel extensions and complex nail art.",
                    socialMedias: "instagram.com/sophialoren",
                    logoUrl: "https://images.unsplash.com/photo-1580489944761-15a19d654956?w=500&auto=format&fit=crop",
                    preferredColors: "#27272A,#A855F7",
                    workingHours: "11:00 - 20:00",
                    rating: 4.98,
                    startingPrice: 50,
                    availabilityStatus: "BOOK FOR TOMORROW"
                );
                spec3.AddService(new ServiceItem("Gel Extension Full Set", "Nails", 70, 90));
                spec3.AddService(new ServiceItem("Premium Spa Manicure", "Nails", 50, 45));

                context.Specialists.AddRange(spec1, spec2, spec3);
                context.SaveChanges();
                Console.WriteLine("Seeded 3 featured specialists.");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error occurred during database seeding: {ex.Message}\n{ex.StackTrace}");
    }
}

app.Run();
