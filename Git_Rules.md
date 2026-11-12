# Quy tắc Branch, Commit và Pull Request

**Dự án:** Game Sudoku Đối Kháng Qua Server — **UDM_17**

## 1. Branch (nhánh làm việc)

- `main` là nhánh chính; **không commit trực tiếp lên `main`**.
- Mỗi thành viên tạo branch riêng cho chức năng đang làm.
- Đặt tên branch theo mẫu: `feature/ten-chuc-nang`, `fix/ten-loi`, `test/ten-kiem-thu`, `docs/ten-tai-lieu`.
- Ví dụ: `feature/server-tcp`, `feature/sudoku-logic`, `feature/client-ui`, `docs/protocol`.

Tạo branch mới từ `main` đã cập nhật:

```bash
git switch main
git pull origin main
git switch -c feature/ten-chuc-nang
```

## 2. Commit (ghi nhận thay đổi)

- Commit bằng tài khoản Git cá nhân.
- Mỗi commit nên tập trung vào một thay đổi rõ ràng.
- Dùng mẫu: `<loai>: <mo-ta-ngan-gon>`.

| Loại | Dùng khi | Ví dụ |
|---|---|---|
| `feat:` | Thêm chức năng | `feat: add TCP connection` |
| `fix:` | Sửa lỗi | `fix: handle client disconnect` |
| `docs:` | Sửa tài liệu | `docs: define JSON protocol` |
| `test:` | Thêm/sửa kiểm thử | `test: validate Sudoku moves` |
| `refactor:` | Sắp xếp lại code | `refactor: separate game logic` |
| `chore:` | Cấu hình/bảo trì | `chore: update gitignore` |

Trước khi commit, kiểm tra file thay đổi và không đưa lên GitHub mật khẩu, secret, file build hoặc dữ liệu không cần thiết.

```bash
git status
git add <file-hoac-thu-muc>
git commit -m "feat: describe the change"
git push -u origin feature/ten-chuc-nang
```

## 3. Pull Request (PR)

Khi hoàn thành một đầu việc:

1. Push branch lên GitHub.
2. Mở Pull Request từ branch của mình vào `main`.
3. Ghi rõ nội dung đã thay đổi, cách kiểm thử và Issue liên quan (ví dụ: `Closes #12`).
4. Nhờ ít nhất một thành viên khác hoặc nhóm trưởng review.
5. Sửa các góp ý và chỉ merge khi code đã được kiểm tra.

**Mẫu mô tả PR:**

```markdown
## Thay đổi
- Nêu chức năng hoặc lỗi đã xử lý.

## Kiểm thử
- [ ] Đã chạy thử chức năng.
- [ ] Đã kiểm tra các trường hợp liên quan.

## Issue
Closes #<issue-number>

## Lưu ý
Nêu lỗi còn tồn tại hoặc module bị ảnh hưởng (nếu có).
```

## 4. Quy tắc chung

- Không tự ý merge code chưa review vào `main`.
- Nếu thay đổi `Protocol.md` hoặc cách các module giao tiếp, phải cập nhật tài liệu và báo cho thành viên liên quan.
- Nếu có conflict, phối hợp giải quyết rồi kiểm tra lại.
- Sau khi merge, cập nhật `main` trước khi làm đầu việc tiếp theo.

**Nhớ:** Làm trên branch riêng → commit rõ ràng → push → tạo PR → review → merge.
