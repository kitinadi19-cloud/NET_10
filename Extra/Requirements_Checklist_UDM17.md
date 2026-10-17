# REQUIREMENTS CHECKLIST — UDM_17
## Đề tài: Game Sudoku đối kháng qua Server

**Mục đích:** Theo dõi yêu cầu đề bài, người phụ trách, bằng chứng và trạng thái nghiệm thu.

**Quy ước:** Tài liệu tách yêu cầu chung, yêu cầu riêng UDM_17 và quyết định nhóm cần chốt. Các lựa chọn như thời lượng trận, công thức tính điểm, công nghệ lưu dữ liệu là đề xuất triển khai nếu đề bài không quy định cụ thể.

## 1. Thông tin dự án

- **Mã đề tài:** UDM_17
- **Tên đề tài:** Game Sudoku đối kháng qua Server
- **Mô hình:** Ứng dụng desktop gồm Server và Client kết nối qua mạng.
- **Công nghệ dự kiến:** C#, .NET, Windows Forms (WinForms), TCP Socket bất đồng bộ, JSON.
- **Lưu dữ liệu:** SQLite hoặc JSON; nhóm cần chọn một phương án.
- **Quản lý mã nguồn:** GitHub; commit cá nhân, nhánh riêng và Pull Request để review/ghép code.

## 2. Yêu cầu chung của dự án

Trạng thái sử dụng: `Chưa làm` / `Đang làm` / `Đã kiểm thử` / `Đạt` / `Chưa đạt`.

| ID | Nhóm | Checklist nghiệm thu | Mức độ | Phụ trách đề xuất | Bằng chứng cần có | Trạng thái |
|---|---|---|---|---|---|---|
| G01 | Ứng dụng | Sản phẩm là ứng dụng GUI desktop, không phải Web App. | Bắt buộc | TV4 | Ảnh/video ứng dụng chạy | Chưa làm |
| G02 | Kiến trúc mạng | Có Client–Server; Client và Server giao tiếp qua mạng thật, không chỉ gọi hàm nội bộ giả lập. | Bắt buộc | TV1, TV2, TV4 | Sơ đồ kiến trúc, demo kết nối | Chưa làm |
| G03 | Protocol | Mô tả cấu trúc message, trường dữ liệu, loại message và cách phản hồi/lỗi. | Bắt buộc | TV1, TV2 | Tài liệu protocol, JSON mẫu | Chưa làm |
| G04 | Trạng thái hệ thống | GUI thể hiện trạng thái kết nối, đang xử lý/chờ, thành công và thất bại khi phù hợp. | Bắt buộc | TV4 | Ảnh/video các trạng thái | Chưa làm |
| G05 | Kiểm tra dữ liệu | Kiểm tra đầu vào; từ chối dữ liệu không hợp lệ và thông báo lỗi dễ hiểu. | Bắt buộc | TV2, TV3, TV4 | Test case dữ liệu sai | Chưa làm |
| G06 | Lỗi mạng | Xử lý mất kết nối, timeout, đóng và giải phóng tài nguyên an toàn. | Bắt buộc | TV2, TV4 | Log và test ngắt kết nối | Chưa làm |
| G07 | Cô lập Client | Một Client lỗi/ngắt kết nối không làm sập Server hoặc làm hỏng Client/trận khác. | Bắt buộc | TV2 | Test nhiều Client, log Server | Chưa làm |
| G08 | Logging | Ghi log sự kiện quan trọng để gỡ lỗi; không ghi mật khẩu, secret hoặc dữ liệu nhạy cảm không cần thiết. | Bắt buộc | TV2, TV5 | File log mẫu đã rà soát | Chưa làm |
| G09 | Kiểm thử chức năng | Có test các luồng chức năng chính và ghi kết quả thực tế. | Bắt buộc | TV6, cả nhóm | Bảng test Pass/Fail | Chưa làm |
| G10 | Kiểm thử dữ liệu lỗi | Test dữ liệu sai định dạng, thiếu trường, giá trị ngoài phạm vi và thao tác không hợp lệ. | Bắt buộc | TV6, TV2, TV3 | Test case và kết quả | Chưa làm |
| G11 | Stress test | Có kịch bản kiểm tra hệ thống dưới tải; ghi số Client/mức tải và kết quả. | Bắt buộc | TV6, TV2 | Kịch bản, log, số liệu | Chưa làm |
| G12 | Performance test | Đo hiệu năng ở ít nhất hai mức tải; ghi cấu hình, cách đo và kết quả thật. | Bắt buộc | TV6, TV2 | Bảng độ trễ/throughput/tỷ lệ lỗi/tài nguyên phù hợp | Chưa làm |
| G13 | Báo cáo | Báo cáo mô tả thiết kế, hoạt động, triển khai và kiểm thử đúng với sản phẩm thực tế. | Bắt buộc | TV6 tổng hợp; mỗi người viết module mình | File báo cáo | Chưa làm |
| G14 | README | Có hướng dẫn cài đặt/chạy, cấu hình kết nối, cấu trúc dự án và cách kiểm thử. | Bắt buộc | TV1, cả nhóm | README.md | Chưa làm |
| G15 | Video demo | Video thể hiện chức năng chính; có âm thanh hoặc chú thích giải thích thao tác theo hướng dẫn. | Bắt buộc | TV6, cả nhóm | File/link video kiểm tra được | Chưa làm |
| G16 | GitHub | Repository có cấu trúc rõ ràng; lịch sử commit thể hiện đóng góp của từng thành viên. | Bắt buộc | TV1, cả nhóm | Repository, commit/PR | Chưa làm |

## 3. Yêu cầu riêng của đề tài UDM_17

| ID | Nhóm chức năng | Checklist nghiệm thu | Mức độ | Phụ trách đề xuất | Bằng chứng cần có | Trạng thái |
|---|---|---|---|---|---|---|
| U01 | Đề Sudoku | Server tạo/chọn đề Sudoku hợp lệ và gửi cùng đề ban đầu cho hai người chơi trong trận. | Bắt buộc | TV2, TV3 | Hai Client nhận cùng đề; test đề | Chưa làm |
| U02 | Hai người chơi | Hai Client kết nối Server và tham gia cùng một trận. | Bắt buộc | TV2, TV4 | Demo hai Client | Chưa làm |
| U03 | Bàn chơi riêng | Mỗi người có tiến độ riêng; thao tác của người này không ghi đè bàn của người kia. | Theo thiết kế đề xuất cần xác nhận | TV2, TV3, TV4 | Demo thao tác độc lập | Chưa làm |
| U04 | Xác thực nước đi | Server kiểm tra nước đi, ô cố định, tọa độ và giá trị; Server quyết định trạng thái cuối, không tin kết quả tự khai từ Client. | Bắt buộc | TV2, TV3 | Test nước đi đúng/sai | Chưa làm |
| U05 | Thời gian | Server quản lý thời gian trận và xử lý khi hết giờ. | Bắt buộc | TV2, TV4 | Demo đồng hồ/hết giờ | Chưa làm |
| U06 | Tiến độ/điểm | Cập nhật tiến độ và điểm theo luật nhóm thống nhất; dữ liệu do Server xác nhận. | Chức năng bắt buộc; công thức cần chốt | TV2, TV3 | Luật chơi, demo | Chưa làm |
| U07 | Kết quả trận | Server xác định và thông báo kết quả theo luật đã công bố. | Bắt buộc | TV2, TV3, TV4 | Demo kết quả | Chưa làm |
| U08 | Nhiều trận | Hỗ trợ nhiều trận độc lập; trạng thái và dữ liệu không bị lẫn. | Bắt buộc | TV2, TV5 | Demo ít nhất hai trận | Chưa làm |
| U09 | Lịch sử | Lưu và xem lại kết quả/lịch sử trận. | Bắt buộc | TV5 | Demo lưu và đọc lịch sử | Chưa làm |
| U10 | Khán giả | Khán giả xem được trận nhưng chỉ có quyền đọc, không thể sửa trạng thái trận. | Bắt buộc | TV2, TV4, TV5 | Demo khán giả, test quyền | Chưa làm |
| U11 | Khán giả vào giữa trận | Người vào xem giữa trận nhận trạng thái hiện tại, không chỉ các cập nhật sau thời điểm kết nối. | Bắt buộc | TV2, TV5 | Test vào xem giữa trận | Chưa làm |

## 4. Các quyết định nhóm phải chốt

| ID | Câu hỏi | Đề xuất ban đầu | Người phụ trách | Trạng thái |
|---|---|---|---|---|
| D01 | Thời lượng trận? | Chọn mốc cụ thể, ví dụ 5 hoặc 10 phút. | Cả nhóm, TV1 điều phối | Chưa chốt |
| D02 | Điều kiện thắng? | Hoàn thành Sudoku hợp lệ trước thắng; hết giờ thì áp dụng luật điểm đã công bố. | Cả nhóm | Chưa chốt |
| D03 | Công thức điểm? | Thống nhất cách tính ô đúng/sai, sửa đáp án và trường hợp bằng điểm. | Cả nhóm, TV3 đề xuất | Chưa chốt |
| D04 | Bàn chơi? | Đề xuất hai bàn/tiến độ riêng nhưng cùng đề ban đầu. | Cả nhóm | Chưa chốt |
| D05 | Người chơi mất kết nối? | Chọn rõ xử thua, cho kết nối lại hay kết thúc trận. | Cả nhóm, TV2 đề xuất | Chưa chốt |
| D06 | Đáp án sai? | Server trả kết quả; thống nhất có trừ điểm/phạt thời gian hay không. | Cả nhóm, TV3 đề xuất | Chưa chốt |
| D07 | Lưu dữ liệu? | Chọn SQLite hoặc JSON, tránh làm song song hai phương án nếu không cần. | TV5 đề xuất, TV1 chốt cùng nhóm | Chưa chốt |
| D08 | Mức tải kiểm thử? | Chọn ít nhất hai mức tải, ghi rõ số Client và cách đo. | TV6, TV2 | Chưa chốt |
| D09 | Phiên bản .NET? | Chọn SDK có trên máy các thành viên và thống nhất trước khi code. | TV1, TV2, TV4 | Chưa chốt |
| D10 | Hạn nộp/hình thức nộp? | Đối chiếu thông báo mới nhất của giảng viên, không tự suy đoán. | TV1 | Chưa xác nhận |

## 5. Chức năng mở rộng — chỉ làm khi yêu cầu bắt buộc đã ổn định

- Chat giữa người chơi.
- Đăng ký/đăng nhập và xếp hạng toàn cục.
- Animation, âm thanh, skin và hiệu ứng nâng cao.
- Ghép trận theo cấp độ hoặc giải đấu.
- Triển khai Server lên Internet/cloud nếu đề không yêu cầu.

Không để các chức năng mở rộng làm chậm các yêu cầu bắt buộc.

## 6. Tiêu chí nghiệm thu theo giai đoạn

### Mốc 1 — Kết thúc tuần 1
- [ ] Repository và solution khởi tạo được.
- [ ] Có tài liệu kiến trúc Client–Server và protocol.
- [ ] Server và Client trao đổi message qua TCP.
- [ ] Module Sudoku có test cơ bản.
- [ ] GUI Client mẫu và kế hoạch kiểm thử đã có.

### Mốc 2 — Kết thúc tuần 2
- [ ] Hai Client chơi được một trận từ đầu đến cuối.
- [ ] Server xác thực nước đi, quản lý thời gian và kết quả.
- [ ] Có thông báo lỗi cơ bản và xử lý ngắt kết nối.
- [ ] Có bản MVP chạy được để demo.

### Mốc 3 — Kết thúc tuần 3
- [ ] Lịch sử lưu và đọc lại được.
- [ ] Khán giả chỉ xem, không sửa trạng thái.
- [ ] Khán giả vào giữa trận nhận trạng thái hiện tại.
- [ ] Nhiều trận hoạt động độc lập.
- [ ] Có kết quả test dữ liệu lỗi, stress và performance ở ít nhất hai mức tải.

### Mốc 4 — Trước khi nộp
- [ ] Các yêu cầu bắt buộc được kiểm tra bằng bằng chứng.
- [ ] README và báo cáo khớp với phiên bản code cuối.
- [ ] Video demo có âm thanh hoặc chú thích phù hợp.
- [ ] Repository rõ ràng và có commit của từng thành viên.
- [ ] Không ghi số liệu kiểm thử chưa đo.

## 7. Cách cập nhật checklist

Với mỗi dòng:
1. Cập nhật trạng thái: Chưa làm / Đang làm / Đã kiểm thử / Đạt / Chưa đạt.
2. Gắn link commit/PR hoặc ảnh, log, video, báo cáo test.
3. Ghi lỗi còn tồn tại, người xử lý và ngày dự kiến hoàn thành.

**Nguyên tắc nghiệm thu:** Có code chưa đồng nghĩa đã đạt; phải chạy thử và có bằng chứng. Các mục “cần chốt” phải được nhóm quyết định trước khi triển khai sâu.
