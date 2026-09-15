using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ServiceBooking.Api.Data;
using ServiceBooking.Api.Services;
using System.Text;
using System.Text.Json.Serialization;
using ServiceBooking.Api.Converters;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
      options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter());

      options.JsonSerializerOptions.Converters.Add(
        new TimeOnlyJsonConverter());
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
      var jwtKey = builder.Configuration["Jwt:Key"]
          ?? throw new InvalidOperationException(
              "JWT key is not configured.");

      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],

        IssuerSigningKey = new SymmetricSecurityKey(
              Encoding.UTF8.GetBytes(jwtKey)
          )
      };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowFrontend", policy =>
  {
    policy
      .WithOrigins("http://localhost:3000")
      .AllowAnyHeader()
      .AllowAnyMethod();
  });
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ServiceManagementService>();
builder.Services.AddScoped<StaffManagementService>();
builder.Services.AddScoped<ScheduleManagementService>();
builder.Services.AddScoped<BookingService>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
  options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    Description = "JWT Authorization header using the Bearer scheme."
  });

  options.AddSecurityRequirement(document =>
      new OpenApiSecurityRequirement
      {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
      });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider
      .GetRequiredService<AppDbContext>();

  // await DbSeeder.SeedAsync(db);
}

app.Run();