# Protocol – Sudoku Đối Kháng Qua Server

## 1. Mục đích

Quy định cách Client và Server trao đổi dữ liệu, xử lý yêu cầu và đồng bộ trạng thái trong game Sudoku đối kháng.

## 2. Quy tắc giao tiếp

- Giao thức: TCP, cổng mặc định `8888`.
- Định dạng dữ liệu: JSON, mã hóa UTF-8.
- Mỗi message kết thúc bằng ký tự xuống dòng `\n`.
- Server quản lý và xác thực trạng thái chính thức của trận đấu.

**Trạng thái:** Đã triển khai kết nối TCP và trao đổi JSON; cần hoàn thiện xử lý dữ liệu trong các trường hợp đặc biệt.

## 3. Cấu trúc message

**Client gửi lên – `RequestMessage`:**
- `Type`: Tên yêu cầu.
- `Payload`: Chuỗi JSON chứa dữ liệu theo từng loại yêu cầu.

**Server phản hồi – `ResponseMessage`:**
- `Status`: Trạng thái xử lý.
- `Message`: Nội dung thông báo.
- `Data`: Dữ liệu bổ sung, có thể là `null`.

Ví dụ:

```json
{
  "Type": "JOIN_QUEUE",
  "Payload": ""
}
```

Cấu trúc chi tiết của `Payload` và `Data` phải được thống nhất theo từng loại message.

## 4. Danh sách message

**Đã triển khai:**

| Message | Hướng | Chức năng |
|---|---|---|
| `JOIN_QUEUE` | Client → Server | Yêu cầu ghép trận |
| `WAITING` | Server → Client | Thông báo đang chờ |
| `MATCH_FOUND` | Server → Hai Client | Thông báo tìm được đối thủ |

**Cần triển khai:**

| Message | Hướng | Chức năng |
|---|---|---|
| `GAME_STARTED` | Server → Người chơi | Thông báo bắt đầu trận |
| `SUBMIT_MOVE` | Client → Server | Gửi nước đi |
| `MOVE_RESULT` | Server → Client gửi yêu cầu | Thông báo kết quả xử lý nước đi |
| `BOARD_UPDATE` | Server → Các Client liên quan | Đồng bộ bàn cờ |
| `TIME_UPDATE` | Server → Người chơi | Cập nhật thời gian |
| `GAME_OVER` | Server → Người chơi | Thông báo kết quả trận |
| `SPECTATE_MATCH` | Client → Server | Yêu cầu xem trận |
| `SPECTATOR_UPDATE` | Server → Khán giả | Cập nhật trạng thái trận đấu |
| `ERROR` | Server → Client | Thông báo lỗi |

## 5. Quy tắc xử lý

- **Nước đi:** Server xác thực nước đi. Nếu không hợp lệ, không ghi nhận và gửi lỗi; nếu hợp lệ, cập nhật trạng thái và gửi thông tin đồng bộ.
- **Đồng bộ:** Server là nơi quản lý trạng thái bàn cờ chính thức.
- **Khán giả:** Chỉ được xem trận đấu, không được gửi nước đi.
- **Ngắt kết nối:**  khi server phát hiện có người chơi bị mất kết nối, server sẽ tạm dừng trận đấu trong 1 phút
- **Thắng/thua:** Người hoàn thành bàn Sudoku hợp lệ trước sẽ thắng. Hết 10 phút, so sánh số ô điền đúng; bằng nhau thì hòa.
- **Lỗi:** Server cần xử lý JSON sai, yêu cầu không hợp lệ và message không được hỗ trợ.

**Trạng thái:** Các quy tắc nghiệp vụ trên đã được thống nhất, nhưng phần lớn vẫn cần triển khai trong code.

## 6. Trạng thái hiện tại

Đã có kết nối TCP, truyền JSON, cấu trúc message cơ bản và ghép hai Client qua hàng chờ. Việc khởi tạo trận, xử lý nước đi, đồng bộ bàn cờ, tính giờ, xác định kết quả, xử lý thua do ngắt kết nối và chế độ khán giả vẫn cần phát triển.

**Mọi thay đổi về cấu trúc message phải được cập nhật trong tài liệu để Client và Server luôn sử dụng cùng một giao thức.**
