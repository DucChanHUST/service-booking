# Service Booking

Hệ thống đặt lịch dịch vụ với backend ASP.NET Core, frontend Next.js và PostgreSQL.

## Yêu cầu

- Docker + Docker Compose
- Node.js 22+
- .NET 8 SDK
- pnpm

## 1. Cài đặt nhanh bằng Docker

Tạo file `.env` ở thư mục gốc:

```env
POSTGRES_DB=service_booking
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
JWT_KEY=service-booking-demo-super-secret-key-2026-minimum-32-characters
```

Sau đó chạy:

```bash
docker compose up --build -d
```

Kiểm tra:

- Frontend: http://localhost
- Swagger API: http://localhost/swagger
- Database Postgres: localhost:5432

Dừng hệ thống:

```bash
docker compose down
```

## 2. Chạy local (không dùng Docker)

### Backend

```bash
cd backend/ServiceBooking.Api
dotnet restore
dotnet run
```

API chạy ở: http://localhost:5172 hoặc theo cổng cấu hình trong launchSettings.

### Frontend

```bash
cd frontend
pnpm install
pnpm dev
```

Frontend chạy ở: http://localhost:3000

## 3. Test database

Dùng file `docker-compose.test.yml` nếu cần database test riêng:

```bash
docker compose -f docker-compose.test.yml up -d
```

## 4. Tài khoản demo

Ứng dụng được thiết lập sẵn các tài khoản demo sau:

| Role     | Email                   | Password       |
| -------- | ----------------------- | -------------- |
| Admin    | `admin@example.com`     | `Admin@123`    |
| Customer | `customer1@example.com` | `Customer@123` |
| Customer | `customer2@example.com` | `Customer@123` |
