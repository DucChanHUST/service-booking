# Service Booking - Handover Document

## Chức năng đã hoàn thành

### Authentication & Authorization

- Đăng nhập bằng JWT
- Phân quyền Customer/Admin
- Validate quyền truy cập API
- Password hash bằng BCrypt

### Customer

- Xem danh sách dịch vụ
- Xem khung giờ có thể đặt
- Tạo booking
- Xem booking của bản thân
- Lọc booking theo trạng thái
- Hủy booking với lý do bắt buộc
- Không cho phép hủy booking đã hoàn thành / đã bắt đầu

### Admin

- Quản lý dịch vụ
- Quản lý nhân viên
- Quản lý lịch làm việc
- Xem tất cả booking
- Cập nhật trạng thái booking

### Business rules

- Không cho phép đặt lịch trong quá khứ
- Không đặt ngoài giờ làm việc của staff
- Không overlap với booking khác
- Không cho phép service inactive / staff inactive
- Xử lý timezone theo UTC
- Dùng transaction + advisory lock khi đặt booking

### Real-time & background

- SignalR update trạng thái booking realtime
- Hangfire tự động chuyển booking quá hạn thành Completed

### Testing

- Có automated tests cho các rule nghiệp vụ chính
- Có project kiểm tra race condition
