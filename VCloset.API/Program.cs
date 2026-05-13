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

builder.Services.AddHttpClient();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// [CHỌN GỬI MAIL]: Bật dòng dưới để gửi qua GMAIL (Không cần mua domain, gửi được cho mọi người khi code local)
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// [CHỌN GỬI MAIL]: Bật dòng dưới để gửi qua RESEND (Sử dụng khi deploy lên Cloud và cấu hình tên miền riêng)
// builder.Services.AddScoped<IEmailService, ResendEmailService>();

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

