// Trigger production deployment test
using dotenv.net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VCloset.Application.Interfaces;
using VCloset.Domain.Enums;
using VCloset.Infrastructure.Data;
using VCloset.Infrastructure.Repositories;
using VCloset.Infrastructure.Security;
using VCloset.Infrastructure.Services;
using VCloset.API.Hubs;
using VCloset.API.Services;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Load env vars from the root .env file
DotEnv.Load(options: new DotEnvOptions(probeForEnv: true));

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("MyCnn");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<UserRole>("user_role")
                 .MapEnum<AuthProvider>("auth_provider")
                 .MapEnum<BodyShapeType>("body_shape_type")
                 .MapEnum<ClothingCategory>("clothing_category")
                 .MapEnum<AiJobStatus>("ai_job_status")
                 .MapEnum<CommissionStatus>("commission_status")
                 .MapEnum<PremiumPlan>("premium_plan")
                 .MapEnum<BrandStatus>("brand_status")
                 .MapEnum<ChatRoomType>("chat_room_type")
                 .MapEnum<MessageType>("message_type");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<VClosetVersion30Context>(options =>
    options.UseNpgsql(dataSource));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// Health Check — dùng cho Blue-Green deploy script
builder.Services.AddHealthChecks();

// Kích hoạt dịch vụ SignalR
builder.Services.AddSignalR();

// Base Application Services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();


var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["JWT_SECRET"];
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret ?? "VClosetSuperSecretSecurityKeyThatIsAtLeast32CharactersLong!")),
        ValidateIssuer = true,
        ValidIssuer = "VCloset",
        ValidateAudience = true,
        ValidAudience = "VClosetUsers",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Module 2 - Smart Inventory Services
var s3AccessKey = Environment.GetEnvironmentVariable("S3_ACCESS_KEY") ?? builder.Configuration["S3_ACCESS_KEY"];
var s3SecretKey = Environment.GetEnvironmentVariable("S3_SECRET_KEY") ?? builder.Configuration["S3_SECRET_KEY"];
var s3ServiceUrl = Environment.GetEnvironmentVariable("S3_SERVICE_URL") ?? builder.Configuration["S3_SERVICE_URL"];
var s3BucketName = Environment.GetEnvironmentVariable("S3_BUCKET_NAME") ?? builder.Configuration["S3_BUCKET_NAME"];

if (!string.IsNullOrEmpty(s3AccessKey) && 
    !string.IsNullOrEmpty(s3SecretKey) && 
    !string.IsNullOrEmpty(s3ServiceUrl) && 
    !string.IsNullOrEmpty(s3BucketName))
{
    builder.Services.AddScoped<IStorageService, S3StorageService>();
}
else
{
    builder.Services.AddScoped<IStorageService, LocalStorageService>();
}

builder.Services.AddScoped<IWardrobeService, WardrobeService>();
builder.Services.AddHttpClient<IBackgroundRemovalService, PhotoroomService>();
builder.Services.AddHttpClient<IVirtualTryOnService, FashnTryOnService>();

// Đăng ký Module 4
builder.Services.AddScoped<ICanvasService, CanvasService>();

// Đăng ký Module 10 - Notifications (Database & SignalR Real-Time)
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationHubService, NotificationHubService>();

// Cấu hình chính sách CORS hỗ trợ SignalR (Cực kỳ quan trọng để kết nối với Web và di động)
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true) // Cho phép tất cả các nguồn (Origins) kết nối
              .AllowCredentials(); // Bắt buộc phải có để SignalR truyền nhận thông tin kết nối
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "VCloset API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<VClosetVersion30Context>();

        try
        {
            var conn = context.Database.GetDbConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (\"MigrationId\" character varying(150) NOT NULL, \"ProductVersion\" character varying(32) NOT NULL, CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY (\"MigrationId\"));";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('20260523062128_InitialCreate', '8.0.4') ON CONFLICT DO NOTHING;";
            cmd.ExecuteNonQuery();
            conn.Close();
        }
        catch { }

        context.Database.Migrate();
        Console.WriteLine("[INFO] Database Checked/Created Successfully via Code-First.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] An error occurred creating the DB: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(); // Allow serving images from wwwroot

// Kích hoạt CORS trước khi định tuyến và phân quyền
app.UseCors("SignalRPolicy");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health Check endpoint — Docker dùng để biết container đã sẵn sàng chưa
app.MapHealthChecks("/health");

// Khai báo Endpoint Route cho Hub SignalR
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
