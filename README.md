# Personal Log Manager

Personal Log Manager là ứng dụng desktop dành cho Windows, được thiết kế theo mô hình **offline-first**, dùng để quản lý nhật ký công việc, nhật ký cá nhân, thư từ, ghi chú và các danh mục tùy chỉnh.

Dữ liệu được lưu trữ cục bộ bằng SQLite. Người dùng có thể tìm kiếm, gắn thẻ, tạo báo cáo và xuất dữ liệu mà không cần tài khoản, máy chủ hoặc kết nối mạng.

## Tính năng

* **Danh mục Log:** Công việc (Work), Cá nhân (Personal), Thư (Letter), Ghi chú (Note) và các danh mục tùy chỉnh do người dùng tạo, mỗi danh mục có thể thiết lập icon và màu sắc.
* **Quản lý công việc:** Dự án, công việc, trạng thái, kết quả, vấn đề/blocker, ghi chú, tag và chức năng nhắc nhở công việc hằng ngày.
* **Nhật ký cá nhân:** Người/chủ đề liên quan, tâm trạng, nội dung, điều muốn ghi nhớ, ghi chú và tag.
* **Thư:** Người nhận, tiêu đề, lời mở đầu, nội dung, lời kết và chữ ký; hỗ trợ xuất nội dung sạch sang TXT UTF-8 và Markdown.
* **Tag:** Tag được nhập bằng dấu phẩy và tự động tạo trong quá trình lưu Log. Trang Tags cho phép đổi tên, đổi màu và xóa Tag.
* **Lịch sử:** Tìm kiếm toàn bộ nội dung theo tiêu đề, nội dung, dự án, kết quả, blocker, ghi chú, người nhận và tag; hỗ trợ lọc theo danh mục, tag, trạng thái và khoảng thời gian; cho phép chỉnh sửa, nhân bản và xóa có xác nhận.
* **Báo cáo Excel hằng tuần:** Bao gồm các phần công việc đã hoàn thành, chưa hoàn thành và kế hoạch tuần tiếp theo, sử dụng các cột báo cáo tiếng Việt và một file Excel template đi kèm có thể thay thế.
* **Các định dạng xuất khác:** CSV, TXT, Markdown và sao lưu cơ sở dữ liệu SQLite cục bộ.
* **Dashboard và thống kê:** Hiển thị các Log gần đây, tiến độ hoàn thành công việc, chuỗi ngày làm việc (streak), tổng số Log theo tháng/năm và số lượng Log theo danh mục.
* **Cài đặt:** Nhắc nhở công việc mặc định lúc 17:00, tính năng snooze, bỏ qua reminder trong ngày, hành vi System Tray, khởi động cùng Windows, giao diện Light/Dark/System và màu chủ đạo.
* **Single Instance:** Chỉ cho phép chạy một instance của ứng dụng.
* **System Tray:** Có menu thao tác nhanh tại khu vực Notification Area của Windows.
* **Logging:** Sử dụng Serilog và lưu log ứng dụng theo từng ngày.
* **Migration:** Hỗ trợ chuyển dữ liệu từ cơ sở dữ liệu của phiên bản Daily Log cũ.

> **Lưu ý:** Reminder hằng ngày chỉ kiểm tra sự tồn tại của **Work Log**. Log Personal, Letter hoặc Note sẽ không được tính là Work Log và không làm hoàn thành hoặc kích hoạt Work Reminder.

## Yêu cầu hệ thống

* Windows 10/11, 64-bit.
* .NET 8 SDK để build và test.
* Inno Setup 6 để biên dịch Windows Installer nếu muốn tạo bộ cài.

Phiên bản publish dạng **self-contained** không yêu cầu cài .NET trên máy tính đích.

## Build và chạy

Thực hiện các lệnh sau từ thư mục gốc của repository bằng PowerShell:

```powershell
dotnet restore .\DailyLogAssistant.sln
dotnet build .\DailyLogAssistant.sln
dotnet run --project .\DailyLogAssistant\DailyLogAssistant.csproj
```

## Chạy Unit Test và Integration Test

Chạy các bài test về Unit Test, SQLite và Excel:

```powershell
dotnet test .\DailyLogAssistant.sln
```

## Publish và tạo Installer

Publish ứng dụng Windows x64 dạng self-contained và single-file. File Excel template sẽ được copy cùng ứng dụng:

```powershell
dotnet publish .\DailyLogAssistant\DailyLogAssistant.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:PublishTrimmed=false `
  -o .\publish\win-x64
```

Có thể chạy trực tiếp:

```text
.\publish\win-x64\PersonalLogManager.exe
```

Hoặc biên dịch bộ cài dành cho người dùng sau khi đã cài Inno Setup 6:

```powershell
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" .\installer\DailyLogAssistant.iss
```

Installer sẽ được tạo tại:

```text
.\publish\PersonalLogManager-Setup-2.0.0.exe
```

Installer được cài đặt cho user hiện tại, không yêu cầu quyền Administrator, tạo shortcut trong Start Menu, cho phép tạo Desktop shortcut và đăng ký Uninstaller.

Sao chép file installer sang một máy Windows 64-bit khác và chạy để cài đặt.

Database và Settings của người dùng không được đóng gói bên trong installer.

## Sử dụng lần đầu

Chọn **Work**, **Personal**, **Letters** hoặc **Notes** trên thanh sidebar để tạo Log mới.

Log có thể được chỉnh sửa bằng cách mở từ trang History. Khi xóa Log, ứng dụng sẽ yêu cầu xác nhận.

Nhập một hoặc nhiều Tag được phân cách bằng dấu phẩy trong Log. Ngoài ra có thể quản lý tên và màu Tag trong trang **Tags**.

Tùy chọn **+ Add custom category** cho phép tạo danh mục tùy chỉnh bằng cách nhập:

* Tên
* Icon
* Màu sắc

Trang **History** hỗ trợ:

* Tìm kiếm toàn bộ nội dung.
* Lọc theo Category.
* Lọc theo Tag.
* Lọc theo Status.
* Lọc theo khoảng thời gian.

Trang **Export** cho phép xuất dữ liệu theo khoảng thời gian, Category và Tag được lựa chọn.

Tùy chọn Excel sẽ tạo báo cáo Work theo tuần. Khoảng thời gian của báo cáo cũng bao gồm các Work Log có trạng thái Planned trong 7 ngày tiếp theo sau ngày kết thúc báo cáo.

Letter Editor hỗ trợ:

* Preview
* Xuất TXT
* Xuất Markdown

Tên file Letter sử dụng định dạng:

```text
Letter-{slug}-{date}
```

## Nhắc nhở công việc hằng ngày

Thời gian mặc định của Work Reminder là:

```text
17:00
```

Có thể thay đổi thời gian trong **Settings**.

Sau khi lưu cài đặt, sử dụng **Test reminder now** để kiểm tra giao diện reminder.

Reminder kiểm tra xem trong ngày hiện tại đã tồn tại **Work Log** hay chưa.

Reminder **không yêu cầu một trạng thái Work cụ thể**.

Các lựa chọn Snooze:

* 15 phút
* 30 phút
* 1 giờ

Tùy chọn **Dismiss** sẽ tắt reminder cho ngày hiện tại.

Nếu ứng dụng được khởi động sau thời gian reminder, máy tính resume từ sleep hoặc Windows được unlock, ứng dụng sẽ thực hiện kiểm tra reminder lại.

Bật:

```text
Start application with Windows
```

để ứng dụng tự động chạy sau khi đăng nhập Windows và tiếp tục nhận reminder.

Ứng dụng có thể được thu nhỏ xuống Notification Area.

Tray Menu hỗ trợ:

* Mở ứng dụng.
* Mở Work Log hôm nay.
* Tạo Note.
* Mở History.
* Mở Export.
* Mở Settings.
* Thoát ứng dụng.

## Template Excel báo cáo tuần

Template Excel mặc định nằm tại:

```text
DailyLogAssistant\Resources\WorkReportTemplate.xlsx
```

Template chứa ba phần báo cáo và các tiêu đề cột tiếng Việt được mô tả trong yêu cầu của project.

Có thể thay đổi template tại:

```text
Settings → Work report template
```

và chọn một file `.xlsx` khác.

Template cần có các tiêu đề có thể nhận diện được cho:

* Công việc đã hoàn thành.
* Công việc chưa hoàn thành.
* Kế hoạch tuần tiếp theo.

Excel exporter sẽ tự động:

* Tìm các section tương ứng.
* Tìm hàng header.
* Mapping các cột dựa trên tên header.
* Thêm hàng khi cần.
* Cố gắng giữ nguyên style của workbook hiện tại.

Nếu workbook được chọn không thể mapping chính xác, hệ thống sẽ sử dụng layout báo cáo mặc định.

File Excel tham khảo:

```text
Vu-Bao Cao Tuan - 21-9-2026.xlsx
```

không được đóng gói trực tiếp trong project tại thời điểm triển khai phiên bản này.

Template đi kèm được xây dựng theo layout đã được mô tả trong yêu cầu.

Nếu muốn sử dụng chính xác format của file Excel gốc, hãy chọn file đó trong:

```text
Settings → Work report template
```

## Database, Settings và Logs

### Database

```text
%LOCALAPPDATA%\PersonalLogManager\PersonalLogManager.db
```

### Application Logs

```text
%LOCALAPPDATA%\PersonalLogManager\Logs\application-YYYYMMDD.log
```

### Excel Template mặc định

```text
%LOCALAPPDATA%\PersonalLogManager\Templates\WorkReportTemplate.xlsx
```

Template mặc định được copy vào thư mục trên trong lần chạy đầu tiên.

Nếu phát hiện database của phiên bản Daily Log cũ:

```text
%LOCALAPPDATA%\DailyLogAssistant\DailyLogAssistant.db
```

ứng dụng sẽ copy và migrate database trong lần chạy đầu tiên.

Dữ liệu Daily Log cũ và Reminder Settings sẽ được giữ lại.

Database cũ không bị xóa hoặc thay đổi.

## Cấu trúc Database

Database bao gồm các bảng:

```text
Logs
Tags
LogTags
Categories
AppSettings
```

Có các index cho:

```text
Date
Category
Status
Title
```

Hệ thống Tag sử dụng quan hệ nhiều-nhiều.

Các trường phụ thuộc Category được phép để trống nếu không áp dụng.

Nội dung người dùng nhập được lưu hoàn toàn trên máy tính cục bộ, trừ khi người dùng chủ động xuất hoặc sao lưu dữ liệu.

## Xử lý sự cố

### Reminder không xuất hiện

Kiểm tra:

* Ứng dụng đang chạy.
* Đã bật `Start application with Windows` nếu muốn nhận reminder sau khi đăng nhập.
* Reminder đang được bật.
* Thời gian reminder được cấu hình chính xác.
* Hôm nay chưa có Work Log.
* Ngày hiện tại chưa được Dismiss.

Reminder sử dụng giờ địa phương của Windows.

### Không nhìn thấy ứng dụng

Ứng dụng có thể đang nằm trong Notification Area.

Mở lại bằng Tray Menu hoặc chọn **Exit** từ Tray Menu rồi khởi động lại ứng dụng.

### Excel Template không được áp dụng

Đảm bảo:

* File có định dạng `.xlsx`.
* File chứa các section heading có thể nhận diện.
* File có các column heading cần thiết.

Nếu mapping thất bại, ứng dụng vẫn có thể xuất báo cáo bằng layout mặc định.

### Database Migration

Migration sẽ giữ nguyên database Daily Log cũ.

Database nguồn không bị xóa.

Database mới được tạo tại:

```text
%LOCALAPPDATA%\PersonalLogManager\
```

### Windows Notification

Notification sử dụng cơ chế thông báo của Windows.

Windows có thể chặn notification tùy theo cài đặt hệ thống.

Kiểm tra:

```text
Windows Settings
→ Notifications
```

### Custom Category

Hiện tại Custom Category chưa hỗ trợ xóa trực tiếp trên giao diện.

Các Category mặc định được bảo vệ và không thể xóa.

### Reminder khi máy tính tắt

Máy tính đang tắt không thể hiển thị reminder.

Khi Windows khởi động lại và ứng dụng được chạy, hệ thống sẽ kiểm tra lại trạng thái reminder của ngày hiện tại.

## Kiến trúc và Dependencies

Ứng dụng WPF sử dụng:

* CommunityToolkit.Mvvm
* Dependency Injection
* Hosted Services
* Entity Framework Core
* SQLite
* Serilog
* ClosedXML

Kiến trúc sử dụng mô hình **MVVM**.

UI State được quản lý bởi ViewModel.

Các chức năng chính được tách thành các Service riêng:

* Data Access
* Category Management
* Tag Management
* Reminder
* Theme
* Letter Export
* Excel Reporting
* Statistics

Các Service được quản lý thông qua Dependency Injection.

## Test

Project:

```text
DailyLogAssistant.Tests
```

bao gồm các bài kiểm thử cho:

* Reminder logic.
* Date logic.
* SQLite CRUD.
* SQLite Search.
* Database Migration.
* Tag Filter.
* Date Filter.
* Letter Export.
* Excel Report Generation.
* Excel Template Generation.

## Mục tiêu

Personal Log Manager được xây dựng để trở thành một công cụ quản lý Log cá nhân và công việc hoàn toàn offline.

Người dùng chỉ cần nhập dữ liệu một lần, sau đó có thể:

```text
Daily Work Log
      ↓
Weekly Excel Report
      ↓
Monthly Statistics
      ↓
Historical Search
```

Hoặc:

```text
Letter
  ↓
TXT / Markdown
```

Hoặc:

```text
Personal Log
  ↓
Tag
  ↓
Search
  ↓
History
```

Mục tiêu của ứng dụng là cung cấp một công cụ:

* Đơn giản.
* Nhanh.
* Hoạt động offline.
* Bảo vệ dữ liệu cục bộ.
* Dễ tìm kiếm.
* Dễ xuất báo cáo.
* Có thể mở rộng trong tương lai.
