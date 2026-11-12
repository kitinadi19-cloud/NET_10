# Protocol – Sudoku Đối Kháng Qua Server

## 1. Mục đích
Quy định cách Client và Server trao đổi dữ liệu, xử lý yêu cầu và đồng bộ trạng thái trong game Sudoku đối kháng.

## 2. Quy tắc giao tiếp
- Giao thức: TCP, cổng mặc định `8888` (đổi bằng tham số dòng lệnh `SudokuServer.exe 9000` hoặc biến môi trường `SUDOKU_PORT`; Client nhập IP/Port trên giao diện).
- Định dạng: JSON, UTF-8, mỗi message kết thúc bằng `\n`.
- Server là nơi quản lý và xác thực trạng thái chính thức của trận đấu.
- Client gửi `PING` mỗi 10 giây; Server ngắt kết nối nếu im lặng quá 45 giây. Message tối đa 64 KB.

## 3. Cấu trúc message
**Client → Server – `RequestMessage`:** `Type` (tên yêu cầu), `Payload` (chuỗi JSON, có thể rỗng).

**Server → Client – `ResponseMessage`:** `Status` (**tên loại message**: `WAITING`, `BOARD_UPDATE`, `ERROR`...), `Message` (thông báo), `Data` (đối tượng JSON hoặc `null`).

```json
{ "Type": "SUBMIT_MOVE", "Payload": "{\"Row\":0,\"Col\":2,\"Value\":5}" }
{ "Status": "MOVE_RESULT", "Message": "Đúng: +1 điểm", "Data": { "Row":0, "Col":2, "Value":5, "Accepted":true, "ScoreDelta":1, "Score":3, "Reason":"Đúng: +1 điểm" } }
```

## 4. Danh sách message (tất cả đã triển khai)

### Client → Server
| Type | Payload | Chức năng |
|---|---|---|
| `LOGIN` | `{Name}` | Đăng nhập bằng tên (1–20 ký tự, duy nhất). Dùng lại tên cũ để quay lại trận đang tạm dừng |
| `JOIN_QUEUE` / `LEAVE_QUEUE` | – | Vào / rời hàng chờ ghép trận |
| `CHALLENGE` | `{Target}` | Mời một người chơi đang rảnh |
| `CHALLENGE_RESPONSE` | `{Challenger, Accept}` | Chấp nhận / từ chối lời mời |
| `SUBMIT_MOVE` | `{Row, Col, Value}` | Gửi nước đi (Row, Col: 0–8; Value: 1–9) |
| `SURRENDER` | – | Bỏ cuộc |
| `LIST_PLAYERS` / `LIST_MATCHES` | – | Danh sách người chơi online / trận đang diễn ra |
| `SPECTATE_MATCH` | `{MatchId}` | Vào xem trận (nhận ngay trạng thái đầy đủ) |
| `LEAVE_SPECTATE` | – | Rời phòng xem (không ảnh hưởng trận đấu) |
| `PING` | – | Giữ kết nối |

### Server → Client (`Status`)
| Status | Data | Gửi tới |
|---|---|---|
| `LOGIN_OK` | – | Người vừa đăng nhập |
| `WAITING` | – | Người đang chờ ghép trận |
| `MATCH_FOUND` | `{MatchId, Opponent}` | Hai người chơi |
| `GAME_STARTED` | `MatchSnapshot` | Hai người chơi |
| `MOVE_RESULT` | `MoveResultData` | Người gửi nước đi (`ScoreDelta` +1 / -1 / 0) |
| `BOARD_UPDATE` | `MatchSnapshot` (bảng của mình, đối thủ chỉ có điểm/tiến độ) | Hai người chơi |
| `SPECTATOR_UPDATE` | `MatchSnapshot` (cả hai bảng) | Khán giả |
| `TIME_UPDATE` | `TimeUpdateData` | Người chơi + khán giả, mỗi giây |
| `GAME_PAUSED` / `GAME_RESUMED` | `TimeUpdateData` | Người chơi + khán giả |
| `GAME_OVER` | `GameOverData` (`Result`: WIN/LOSE/DRAW; khán giả: END) | Người chơi + khán giả |
| `PLAYER_LIST` / `MATCH_LIST` | danh sách | Người yêu cầu |
| `CHALLENGE_RECEIVED` / `CHALLENGE_DECLINED` | `{From}` / – | Người được mời / người mời |
| `OK`, `PONG`, `ERROR` | – | Phản hồi chung / lỗi |

## 5. Quy tắc xử lý
- **Đề bài:** Server tạo đề hợp lệ, nghiệm duy nhất; hai người nhận cùng đề, mỗi người có bảng riêng.
- **Nước đi:** đúng → ghi vào bảng, +1 điểm; sai → **không** ghi vào bảng, -1 điểm; ô đề bài / ô đã điền đúng → từ chối (không tính điểm). Client không tự quyết định kết quả.
- **Thắng/thua/hòa:** hoàn thành bảng trước thắng; hết 10 phút → điểm cao hơn thắng, bằng nhau hòa; bỏ cuộc → đối thủ thắng.
- **Ngắt kết nối:** Server tạm dừng trận (đồng hồ đứng, từ chối nước đi) tối đa 60 giây. Quay lại (`LOGIN` cùng tên) → nhận `BOARD_UPDATE` và trận tiếp tục; quá 60 giây → xử thua (cả hai cùng quá hạn → hòa).
- **Khán giả:** chỉ xem; `SUBMIT_MOVE` bị trả `ERROR`. Vào giữa trận nhận đủ trạng thái; rời phòng không ảnh hưởng trận.
- **Nhiều trận:** mỗi trận có `MatchId` riêng, dữ liệu tách biệt.
- **Lỗi:** JSON sai, thiếu `Type`, message không hỗ trợ, chưa đăng nhập, sai trạng thái → `ERROR`, kết nối vẫn giữ.
- **Lịch sử & log:** `data/match_history.jsonl` (người chơi, thời gian, điểm, kết quả); `logs/server.log` (kết nối, ngắt kết nối, lỗi, thao tác chính).
