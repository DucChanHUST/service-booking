using Microsoft.EntityFrameworkCore;
using ServiceBooking.Api.Data;
using ServiceBooking.Api.DTOs.Staffs;
using ServiceBooking.Api.Entities;

namespace ServiceBooking.Api.Services;

public class StaffManagementService
{
  private readonly AppDbContext _dbContext;

  public StaffManagementService(AppDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<List<StaffResponse>> GetStaffsAsync()
  {
    return await _dbContext.Staffs
      .AsNoTracking()
      .OrderBy(x => x.FullName)
      .Select(x => new StaffResponse
      {
        Id = x.Id,
        FullName = x.FullName,
        Email = x.Email,
        IsActive = x.IsActive
      })
      .ToListAsync();
  }

  public async Task<StaffResponse> CreateAsync(
    CreateStaffRequest request)
  {
    Validate(request.FullName, request.Email);

    var email = request.Email.Trim().ToLowerInvariant();

    var emailExists = await _dbContext.Staffs.AnyAsync(x => x.Email.ToLower() == email);

    if (emailExists)
      throw new ArgumentException("Staff email already exists.");

    var staff = new Staff
    {
      Id = Guid.NewGuid(),
      FullName = request.FullName.Trim(),
      Email = email,
      IsActive = true,
      CreatedAt = DateTime.UtcNow
    };

    _dbContext.Staffs.Add(staff);

    await _dbContext.SaveChangesAsync();

    return MapToResponse(staff);
  }

  public async Task<StaffResponse?> UpdateAsync(
    Guid id,
    UpdateStaffRequest request)
  {
    Validate(request.FullName, request.Email);

    var staff = await _dbContext.Staffs.FirstOrDefaultAsync(x => x.Id == id);

    if (staff is null)
      return null;

    var email = request.Email.Trim().ToLowerInvariant();

    var emailExists = await _dbContext.Staffs
      .AnyAsync(x => x.Id != id && x.Email.ToLower() == email);

    if (emailExists)
      throw new ArgumentException("Staff email already exists.");

    staff.FullName = request.FullName.Trim();
    staff.Email = email;
    staff.IsActive = request.IsActive;

    await _dbContext.SaveChangesAsync();

    return MapToResponse(staff);
  }

  private static void Validate(
    string fullName,
    string email)
  {
    if (string.IsNullOrWhiteSpace(fullName))
      throw new ArgumentException("Staff full name is required.");

    if (string.IsNullOrWhiteSpace(email))
      throw new ArgumentException("Staff email is required.");
  }

  private static StaffResponse MapToResponse(Staff staff)
  {
    return new StaffResponse
    {
      Id = staff.Id,
      FullName = staff.FullName,
      Email = staff.Email,
      IsActive = staff.IsActive
    };
  }
}