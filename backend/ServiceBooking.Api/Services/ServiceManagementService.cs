using Microsoft.EntityFrameworkCore;
using ServiceBooking.Api.Data;
using ServiceBooking.Api.DTOs.Services;
using ServiceBooking.Api.Entities;

namespace ServiceBooking.Api.Services;

public class ServiceManagementService
{
  private readonly AppDbContext _dbContext;

  public ServiceManagementService(AppDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<(List<ServiceResponse> Items, int TotalCount)> GetServicesAsync(
      string? search,
      int page,
      int pageSize)
  {
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var query = _dbContext.Services
        .AsNoTracking()
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
    {
      search = search.Trim();

      query = query.Where(x =>
          x.Name.Contains(search) ||
          (x.Description != null && x.Description.Contains(search)));
    }

    var totalCount = await query.CountAsync();

    var items = await query
        .OrderBy(x => x.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(x => new ServiceResponse
        {
          Id = x.Id,
          Name = x.Name,
          Description = x.Description,
          DurationMinutes = x.DurationMinutes,
          Price = x.Price,
          IsActive = x.IsActive
        })
        .ToListAsync();

    return (items, totalCount);
  }

  public async Task<ServiceResponse> CreateAsync(
      CreateServiceRequest request)
  {
    ValidateService(request.Name, request.DurationMinutes, request.Price);

    var service = new Service
    {
      Id = Guid.NewGuid(),
      Name = request.Name.Trim(),
      Description = request.Description?.Trim(),
      DurationMinutes = request.DurationMinutes,
      Price = request.Price,
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _dbContext.Services.Add(service);

    await _dbContext.SaveChangesAsync();

    return MapToResponse(service);
  }

  public async Task<ServiceResponse?> UpdateAsync(
      Guid id,
      UpdateServiceRequest request)
  {
    ValidateService(request.Name, request.DurationMinutes, request.Price);

    var service = await _dbContext.Services
        .FirstOrDefaultAsync(x => x.Id == id);

    if (service is null)
      return null;

    service.Name = request.Name.Trim();
    service.Description = request.Description?.Trim();
    service.DurationMinutes = request.DurationMinutes;
    service.Price = request.Price;
    service.IsActive = request.IsActive;
    service.UpdatedAt = DateTime.UtcNow;

    await _dbContext.SaveChangesAsync();

    return MapToResponse(service);
  }

  private static void ValidateService(
      string name,
      int durationMinutes,
      decimal price)
  {
    if (string.IsNullOrWhiteSpace(name))
      throw new ArgumentException("Service name is required.");

    if (durationMinutes <= 0)
      throw new ArgumentException(
          "Duration must be greater than 0.");

    if (price < 0)
      throw new ArgumentException(
          "Price cannot be negative.");
  }

  private static ServiceResponse MapToResponse(Service service)
  {
    return new ServiceResponse
    {
      Id = service.Id,
      Name = service.Name,
      Description = service.Description,
      DurationMinutes = service.DurationMinutes,
      Price = service.Price,
      IsActive = service.IsActive
    };
  }

  public async Task<ServiceResponse?> GetByIdAsync(
    Guid id)
  {
    return await _dbContext.Services
      .AsNoTracking()
      .Where(x => x.Id == id)
      .Select(x => new ServiceResponse
      {
        Id = x.Id,
        Name = x.Name,
        Description = x.Description,
        DurationMinutes = x.DurationMinutes,
        Price = x.Price,
        IsActive = x.IsActive
      })
      .FirstOrDefaultAsync();
  }
}