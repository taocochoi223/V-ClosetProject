// Trigger production deployment test
using dotenv.net;
using Microsoft.EntityFrameworkCore;
using VCloset.Infrastructure.Data;
using VCloset.Domain.Enums;
using Npgsql;
using VCloset.Application.Interfaces;
using VCloset.Infrastructure.Repositories;
using VCloset.Infrastructure.Services;

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

// Base Application Services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Module 2 - Smart Inventory Services
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<IWardrobeService, WardrobeService>();
builder.Services.AddHttpClient<IBackgroundRemovalService, PhotoroomService>();

// Đăng ký Module 4
builder.Services.AddScoped<ICanvasService, CanvasService>();

// Đăng ký Module 10 - Notifications
builder.Services.AddScoped<INotificationService, NotificationService>();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Auto-create database tables on startup if they don't exist
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<VClosetVersion30Context>();

        // [HACK]: Fake apply the InitialCreate migration so it doesn't crash on existing Supabase tables
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(); // Allow serving images from wwwroot

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

