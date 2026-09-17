using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ServiceBooking.Api.Data;
using ServiceBooking.Api.Services;
using System.Text;
using System.Text.Json.Serialization;
using ServiceBooking.Api.Converters;
using ServiceBooking.Api.Hubs;
using Hangfire;
using Hangfire.PostgreSql;
using ServiceBooking.Api.Services.Jobs;

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

builder.Services.AddHangfire(config =>
{
  config.UsePostgreSqlStorage(options =>
  {
    options.UseNpgsqlConnection(
      builder.Configuration.GetConnectionString("DefaultConnection"));
  });
});
builder.Services.AddHangfireServer();

builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
    var jwtKey = builder.Configuration["Jwt:Key"]
      ?? throw new InvalidOperationException(
        "JWT key is not configured.");

    options.TokenValidationParameters =
      new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],

        IssuerSigningKey =
          new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
      };

    options.Events = new JwtBearerEvents
    {
      OnMessageReceived = context =>
      {
        var accessToken = context.Request.Query["access_token"];

        var path = context.HttpContext.Request.Path;

        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/bookings"))
        {
          context.Token = accessToken;
        }

        return Task.CompletedTask;
      }
    };
  });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowFrontend", policy =>
  {
    policy
      .SetIsOriginAllowed(origin =>
      {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
          return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp
          && (uri.Host == "localhost" || uri.Host == "127.0.0.1")
          && uri.Port == 3000;
      })
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();
  });
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ServiceManagementService>();
builder.Services.AddScoped<StaffManagementService>();
builder.Services.AddScoped<ScheduleManagementService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<BookingExpirationJob>();

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

builder.Services
  .AddSignalR()
  .AddJsonProtocol(options =>
  {
    options.PayloadSerializerOptions.Converters.Add(
      new JsonStringEnumConverter()
    );

    options.PayloadSerializerOptions.Converters.Add(
      new TimeOnlyJsonConverter()
    );
  });

var app = builder.Build();

if (app.Environment.IsDevelopment()
    || builder.Configuration.GetValue<bool>("Swagger:Enabled"))
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard();

app.MapControllers();

app.MapHub<BookingHub>("/hubs/bookings");

using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider
      .GetRequiredService<AppDbContext>();

  // await DbSeeder.SeedAsync(db);
}

RecurringJob.AddOrUpdate<BookingExpirationJob>(
  "complete-expired-bookings",
  job => job.ExecuteAsync(),
  "*/10 * * * *"
);

app.Run();
