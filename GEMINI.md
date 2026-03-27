# GEMINI.md - LightInsight Project Mandates

File này định nghĩa các quy chuẩn và nguyên tắc bắt buộc khi làm việc với dự án LightInsight. Các hướng dẫn ở đây có ưu tiên cao hơn các quy trình mặc định.

## 1. Công nghệ & Framework
- **Ngôn ngữ:** C# (Hỗ trợ cả .NET Framework 4.8 và .NET 8.0).
- **UI Framework:** WPF (XAML).
- **SDK:** Milestone XProtect SDK (`VideoOS.Platform`).
- **Kiến trúc:** Khuyến khích sử dụng MVVM (Model-View-ViewModel). Tách biệt logic UI và Business logic.

## 2. Quy chuẩn Code (C# & XAML)
- **Naming Conventions:**
  - **Private fields:** Sử dụng `_camelCase` (ví dụ: `_isDirty`, `_allCameras`).
  - **Public properties/methods:** Sử dụng `PascalCase`.
  - **Local variables:** Sử dụng `camelCase`.
  - **Constants:** Sử dụng `PascalCase` hoặc `UPPER_SNAKE_CASE`.
- **XAML:**
  - Ưu tiên sử dụng `Style` và `ResourceDictionary` để quản lý giao diện tập trung (Themes/Dark.xaml, Themes/Light.xaml).
  - Sử dụng `Binding` thay vì can thiệp trực tiếp vào UI từ code-behind khi có thể.
- **Tổ chức file:**
  - Logic nghiệp vụ phức tạp nên được đặt trong `LightInsight.BUS`.
  - Truy xuất dữ liệu đặt trong `LightInsight.DAL`.

## 3. Tích hợp Milestone XProtect
- Luôn kiểm tra trạng thái kết nối thông qua `VideoOS.Platform.SDK.Environment`.
- Đảm bảo giải phóng tài nguyên (Dispose) khi các Widget hoặc View bị đóng.
- Sử dụng `MessageCommunication` để giao tiếp giữa các thành phần nếu cần.

## 4. Quy trình làm việc
- **Nghiên cứu:** Phải hiểu rõ cấu trúc `plugin.def` và cách Milestone nạp Plugin trước khi thay đổi cấu trúc khởi tạo.
- **Thử nghiệm:** Luôn kiểm tra giao diện trên cả hai Theme (Light/Dark) nếu có thay đổi về UI.
- **Bảo mật:** Tuyệt đối không lưu trữ thông tin đăng nhập (Username/Password/Token) trực tiếp trong code hoặc file cấu hình không được mã hóa.
- **Sửa code:** Phải hỏi người dùng kiểm tra lại code cần sửa đổi trong session nếu phần nội dung sửa đổi có liên quan đến các hàm không thuộc nội dung vấn đề đang được xử lý.

## 5. Lưu ý đặc biệt
- Khi tạo Widget mới, phải kế thừa từ `IDashboardWidget` hoặc `IResizableWidget` nếu cần hỗ trợ thay đổi kích thước trên Dashboard.
- Cập nhật file `Language/*.xaml` khi thêm các chuỗi văn bản mới để hỗ trợ đa ngôn ngữ (Tiếng Anh/Tiếng Việt).
