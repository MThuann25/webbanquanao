# Website Bán Quần Áo - TrendVibe (Tách Biệt FE & BE)

Dự án Website thương mại điện tử bán quần áo thời trang cao cấp hoàn chỉnh, sẵn sàng triển khai (Production-ready). Dự án được tái cấu trúc thành 2 phần độc lập: **Backend (REST Web API)** và **Frontend (Static Client HTML/CSS/JS)** để người dùng dễ dàng chạy, quản lý và triển khai riêng biệt.

---

## 1. Công Nghệ Sử Dụng

- **Backend (`/BE`):** ASP.NET Core 8.0 Web API (REST API).
- **ORM:** Entity Framework Core (SQL Server).
- **Cơ sở dữ liệu:** Microsoft SQL Server 2014 (đảm bảo tương thích hoàn toàn, không sử dụng các cú pháp T-SQL 2016+).
- **Bảo mật & Auth:** ASP.NET Core Identity (Cookie-based auth, hỗ trợ CORS Credentials) + Google OAuth 2.0.
- **Real-time Chat:** SignalR Hub (`/chatHub`).
- **Frontend (`/FE`):** Static HTML5, Tailwind CSS via CDN, FontAwesome, Chart.js (Dashboard), và SignalR JS Client.
- **Giỏ hàng:** LocalStorage (dành cho khách vãng lai) và Database Sync (khi đăng nhập).

---

## 2. Tài Khoản Thử Nghiệm

Hệ thống tự động khởi tạo dữ liệu mẫu (seed data) bao gồm **20 sản phẩm, 5 danh mục, 5 thương hiệu, các biến thể size/màu, voucher mẫu**, cùng với 2 tài khoản thử nghiệm sau:

| Loại tài khoản | Email đăng nhập | Mật khẩu | Quyền truy cập |
|---|---|---|---|
| **Admin** | `admin@clothingshop.com` | `Admin@123` | Storefront & Khu vực Quản trị (Admin Panel) |
| **User** | `user@clothingshop.com` | `Admin@123` | Chỉ Storefront (Khách hàng) |

---

## 3. Cấu Trúc Mã Nguồn

```
/BE                          → Dự án Backend C# Web API duy nhất chứa toàn bộ logic
  /Controllers               → REST API Endpoints (Auth, Products, Cart, Checkout, Admin, Chat)
  /Data                      → DbContext (EF Core) và Seeder tự động
  /Models                    → Lớp Domain chứa Entities, Enums và Interfaces
  /Services                  → Logic nghiệp vụ (Product, Cart, Order, Chatbot...)
  /Hubs                      → SignalR Hub cho Chatbox
  BE.csproj                  → File project Backend
  Program.cs                 → Cấu hình ứng dụng, CORS, Identity, DI và Hub Routing

/FE                          → Dự án Frontend tĩnh chứa các trang giao diện HTML/CSS/JS
  /js
    /app.js                  → Cấu hình chung, API Fetch layer (Credentials), Header/Footer injection, SignalR Chatbox
  index.html                 → Trang chủ storefront
  shop.html                  → Bộ lọc và tìm kiếm sản phẩm
  detail.html                → Chi tiết sản phẩm, chọn size/màu, bình luận đánh giá
  cart.html                  → Giỏ hàng khách hàng
  checkout.html              → Điền thông tin giao hàng & áp voucher
  vnpay-gateway.html         → Trang thanh toán VNPay mô phỏng
  confirmation.html          → Hóa đơn đặt hàng thành công
  login.html & register.html → Màn hình đăng nhập & đăng ký
  profile.html               → Hồ sơ cá nhân và theo dõi đơn hàng
  admin.html                 → Bảng quản trị của Admin (Dashboard, CRUD sản phẩm, duyệt đơn hàng, kiểm duyệt comment)
```

---

## 4. Hướng Dẫn Khởi Chạy Nhanh

### Bước 1: Chạy Backend API (`/BE`)

1. Mở file `BE/appsettings.json` và cập nhật Connection String phù hợp với SQL Server của bạn tại khóa `"DefaultConnection"`.
   *(Mặc định cấu hình sẵn LocalDB: `"Server=(localdb)\\mssqllocaldb;Database=ClothingShopDB;..."`)*
2. Mở terminal tại thư mục `/BE` và thực hiện lệnh:
   ```bash
   dotnet run
   ```
3. Backend sẽ chạy tại cổng mặc định: `https://localhost:7057` và `http://localhost:5000`. Hệ thống sẽ tự động khởi tạo database và seed dữ liệu mẫu khi chạy lần đầu.

---

### Bước 2: Chạy Frontend Client (`/FE`)

Vì Frontend hoàn toàn là các tệp tĩnh, bạn có thể khởi chạy bằng nhiều cách:

- **Cách 1 (Đơn giản nhất):** Click đúp trực tiếp vào tệp `FE/index.html` để mở trong trình duyệt.
- **Cách 2 (Khuyên dùng):** Chạy một máy chủ tĩnh nội bộ để cookies hoạt động mượt mà nhất.
  - Sử dụng extension **Live Server** trong VS Code (mở thư mục `FE` và click nút "Go Live" ở góc phải màn hình - chạy tại `http://127.0.0.1:5500`).
  - Hoặc chạy lệnh qua Node.js:
    ```bash
    cd FE
    npx http-server -p 5500
    ```

*Lưu ý: Mặc định Frontend được cấu hình kết nối tới Backend tại cổng `https://localhost:7057`. Nếu Backend của bạn chạy ở cổng khác, hãy cập nhật biến `API_BASE_URL` ở đầu file `FE/js/app.js`.*

---

## 5. Cấu hình Google OAuth 2.0 (Tùy chọn)

Để kích hoạt tính năng đăng nhập bằng Google:
1. Truy cập vào [Google Cloud Console](https://console.cloud.google.com/) tạo Client ID OAuth 2.0.
2. Thêm Authorized redirect URIs: `https://localhost:7057/signin-google` (hoặc URI Backend của bạn).
3. Mở file `BE/appsettings.json` và điền thông tin Client ID & Secret tương ứng.
