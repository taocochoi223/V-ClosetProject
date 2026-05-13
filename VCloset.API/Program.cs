// Trigger production deployment test
using dotenv.net;
using Microsoft.EntityFrameworkCore;
using VCloset.Infrastructure.Data;
using VCloset.Domain.Enums;
using Npgsql;
using VCloset.Application.Interfaces;
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

builder.Services.AddHttpContextAccessor();

// Register Application Services
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<IWardrobeService, WardrobeService>();
builder.Services.AddHttpClient<IBackgroundRemovalService, PhotoroomService>();



builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

