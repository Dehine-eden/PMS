using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Helpers;
using ProjectManagementSystem1.Middleware;
using ProjectManagementSystem1.Model.Dto.UserManagementDto;
using ProjectManagementSystem1.Model.Entities;
using ProjectManagementSystem1.Services;
using ProjectManagementSystem1.Services.Background;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using System.Security.Claims;
using System.Text;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using ProjectManagementSystem1.Services.AttachmentService;
using ProjectManagementSystem1.Services.TodoItems;
using ProjectManagementSystem1.Services.TodoItemService;
using Hangfire;
using Hangfire.SqlServer;
using ProjectManagementSystem1.Configuration;
using ProjectManagementSystem1.Services.MilestoneService;
using ProjectManagementSystem1.Services.CommentService;
using ProjectManagementSystem1.Services.ProjectService;
using ProjectManagementSystem1.Services.ProjectTaskService;
using ProjectManagementSystem1.Services.MessageService;
using ProjectManagementSystem1.Services.NotificationService;
using ProjectManagementSystem1.Services.ADService;
using ProjectManagementSystem1.Services.UserService;
using ProjectManagementSystem1.Services.AuthService;
using ProjectManagementSystem1.Services.JwtService;
using ProjectManagementSystem1.Data.Seeders;
using ProjectManagementSystem1.Services.Activators;
using ProjectManagementSystem1.Services.ErpUserService;




var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Trace);
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password Settings customize
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // Lockout Settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IADService, ADService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddHostedService<RefreshTokenCleanupService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddScoped<IProjectAssignmentService, ProjectAssignmentService>();
builder.Services.AddScoped<IMilestoneService, MilestoneService>();
builder.Services.AddScoped<IProjectTaskService, ProjectTaskService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<ITodoItemService, TodoItemService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IIndependentTaskService, IndependentTaskService>();
builder.Services.AddScoped<IPersonalTodoService, PersonalTodoService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient(); // for HttpClient injection
builder.Services.AddScoped<IErpUserService, ErpUserService>();


builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vite dev server
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // if cookies/auth are used
    });
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
//builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Add Hangfire services.
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

GlobalConfiguration.Configuration.UseActivator(new HangfireActivator(builder.Services.BuildServiceProvider()));
// Add the processing server as IHostedService
builder.Services.AddHangfireServer();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Project Management API",
        Version = "v1"
    });

     c.CustomSchemaIds(type => type.FullName);
    c.IgnoreObsoleteProperties();

    // ?? JWT Bearer Auth Setup
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the format: Bearer {your token}"
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
            new string[] { }
        }
    });
    c.UseAllOfToExtendReferenceSchemas();
    c.CustomSchemaIds(type => type.FullName);
    c.IgnoreObsoleteProperties();
    //c.SchemaFilter<EnumSchemaFilter>(); 
});



var jwtSection = builder.Configuration.GetSection("JwtSettings");

if (!jwtSection.Exists() || string.IsNullOrEmpty(jwtSection["SecretKey"]))
{
    throw new ApplicationException("Missing or invalid JWT configuration");
}

if (string.IsNullOrEmpty(jwtSection["SecretKey"]))
    throw new Exception("JWT SecretKey is missing in appsettings.json");

builder.Services.Configure<JwtSettings>(jwtSection);

var jwtSettings = jwtSection.Get<JwtSettings>();
var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuerSigningKey = true
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim(ClaimTypes.Role, "Admin"));
    options.AddPolicy("ManagerOnly", policy => policy.RequireClaim(ClaimTypes.Role, "Manager"));
    options.AddPolicy("SupervisorOnly", policy => policy.RequireClaim(ClaimTypes.Role, "Supervisor"));
    options.AddPolicy("UserOnly", policy => policy.RequireClaim(ClaimTypes.Role, "User"));

    // Composite examples:
    options.AddPolicy("AdminOrManager", policy =>
        policy.RequireAssertion(context =>
        context.User.HasClaim(c => c.Type == ClaimTypes.Role && (c.Value == "Admin" || c.Value == "Manager"))
    ));
});



var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Console.WriteLine($"Database connection test: {db.Database.CanConnect()}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DATABASE CONNECTION FAILED: {ex.Message}");
    }
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await RoleSeeder.SeedRolesAsync(services); //Seed Roles
    await AdminSeeder.SeedAdminAsync(services); //Seed Admin
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.UseHangfireDashboard();

//app.UseMiddleware<JwtErrorHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Project Management System API V1");
    c.RoutePrefix = "Swagger"; // Set Swagger UI at the app's root
});

app.UseHttpsRedirection();

app.UseCors("AllowFrontendDev");

app.UseJwtErrorHandling();
app.UseAuthentication();
app.UseAuthorization();



app.MapControllers();

// Add this before app.Run()
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"UNHANDLED EXCEPTION: {ex}");
        throw;
    }
});

// Test endpoint
app.MapGet("/test", () => "API is running");

app.Run();
