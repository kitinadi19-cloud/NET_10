# Game Rules – UDM_17

## 1. Luật chơi sơ bộ
* Hai người chơi cùng giải một đề Sudoku nhưng có tiến độ riêng.
* Người chơi gửi nước đi lên Server; Server kiểm tra và cập nhật trạng thái trận đấu.
* Server quản lý thời gian, tiến độ và quyết định kết quả.
* Khán giả được theo dõi trận đấu theo thời gian thực nhưng không được sửa trạng thái hoặc thực hiện nước đi.

## 2. Các điểm cần thống nhất
* **Thời gian mỗi trận:** 10 phút mỗi trận.
* **Cách tính điểm:** Tính theo ô đúng (Mỗi ô điền đúng được cộng 1 điểm; ô đánh bị sai -1 điểm. Hết giờ, người có nhiều ô đúng hơn thắng).
* **Điều kiện thắng, thua, hòa:** 
  * Người hoàn thành bảng hợp lệ trước sẽ **thắng**.
  * Nếu hết giờ mà chưa ai hoàn thành, người có số điểm cao hơn sẽ **thắng**.
  * Nếu số điểm bằng nhau, kết quả **hòa**.
  * Nếu một người chủ động bỏ cuộc, người còn lại **thắng**.
* **Xử lý khi người chơi mất kết nối:** khi server phát hiện có người chơi bị mất kết nối
  * server sẽ tạm dừng trận đấu trong 1 phút
  * trong vòng 1 phút nếu chưa quay lại -> xử thua
  * nếu quay lại trong khoảng thời gian đó -> tiếp tục trận đấu

## 3. Tổng quan luật

| Nội dung | Quy định |
| :--- | :--- |
| **Đề Sudoku** | Server tạo hoặc chọn một đề hợp lệ, hai người nhận cùng đề |
| **Tiến độ** | Mỗi người có bảng riêng, không ảnh hưởng bảng của đối thủ |
| **Kiểm tra nước đi** | Server xác thực dữ liệu và kiểm tra đáp án; Client không tự quyết định kết quả |
| **Ô đề bài** | Không được sửa các ô có sẵn |
| **Nước đi sai** | Không ghi nhận vào bảng; có thể thông báo cho người chơi "-1 điểm ô sai" |
| **Khán giả** | Chỉ xem tiến độ, điểm và thời gian; không gửi nước đi |
| **Hết giờ** | Server khóa nước đi và công bố kết quả |
| **Nhiều trận** | Mỗi trận có mã riêng, dữ liệu người chơi được tách biệt |
| **Lịch sử** | Lưu người chơi, thời gian, điểm và kết quả trận đấu |
