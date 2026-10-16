# Hướng dẫn Lab 2 — “Snow Boarder” (PRU213 · QE190149)

> Tài liệu này tóm tắt bằng tiếng Việt. Bản báo cáo đầy đủ (tiếng Anh, theo đúng
> yêu cầu “brief document” của đề) nằm ở `Lab2_Report_QE190149.md`.

---

## 1. Cách mở và chạy game

1. Mở Unity **6000.6.0f1** → **Open project** → chọn thư mục `LAB2_PRU213_QE190149`.
2. Trong cửa sổ **Project**, mở `Assets/Scenes/MainMenu.unity`.
3. Bấm **▶ Play**.

Luồng game: **Main Menu → Level 1 (đường trượt) → End Game (kết thúc)**.
Build Settings đã được cấu hình sẵn theo đúng thứ tự này.

### Điều khiển

| Hành động | Bàn phím | Tay cầm |
|---|---|---|
| Rẽ trái / phải | `←` / `→` (hoặc `A` / `D`) | Cần gạt trái |
| Nhảy (né chướng ngại vật) | `Space`, `↑` hoặc `W` | Nút A |
| Cúi người tăng tốc (boost) | `Shift trái` | Cò phải |
| Thắng / giảm tốc | `↓` hoặc `S` | Cò trái |
| Điều hướng menu | Chuột | — |

---

## 2. Đối chiếu với yêu cầu trong đề (100 điểm)

| Yêu cầu (điểm) | Đã làm | File phụ trách |
|---|---|---|
| Snowboarding Adventure (20) | Rẽ trái/phải bằng mô-men xoắn: **nghiêng lái** khi trên tuyết, **xoay tự do** khi trên không; nhảy né vật cản; boost | `PlayerController.cs` |
| Slope Physics (15) | Dùng `Rigidbody2D` + `SurfaceEffector2D` sẵn có của level; trọng lực, quán tính, ma sát lấy từ physics engine; chỉnh theo độ khó | `PlayerController.cs`, `GameSettings.cs` |
| Obstacle Course & Challenges (20) | ~30 bông tuyết, 6 power-up, 12 chướng ngại vật (cây phải nhảy qua / vũng băng làm chậm), 6 dốc nhảy — trải dài gần 200 đơn vị, vạch đích ở x=152 | `Editor/Lab2SceneBuilder.cs` |
| User Interface (10) | Menu chính (Start / Options / Quit) + HUD trong game (điểm, tốc độ, mạng, combo, thời gian) | `MainMenuController.cs`, `HUDController.cs` |
| Collisions & Triggers (15) | Va chạm cây = tai nạn (**slow-motion + màn hình đỏ + chữ CRASH!**), vũng băng = giảm tốc + chữ SLOWED!; trigger = tuyết rơi, power-up, vạch đích | `CrashDetector.cs`, `Obstacle.cs`, `Collectible.cs`, `PowerUp.cs`, `FinishLine.cs` |
| Game Logic & Scoring (20) | Điểm theo tốc độ, vật phẩm và trick; hệ thống combo + hệ số nhân x1→x5 | `ScoreManager.cs`, `TrickController.cs` |
| Visuals & Aesthetics | Hình 2D mùa đông, **thời tiết động** (tuyết rơi + tầm nhìn đổi theo thời gian), bụi tuyết theo tốc độ | `SnowfallController.cs`, `DustTrail.cs` |
| Tính năng tùy chọn | 3 độ khó, lưu điểm cao (`PlayerPrefs`), power-up bất tử, hiển thị thời gian | `GameSettings.cs`, `GameManager.cs` |

---

## 3. Ba bản level theo độ khó

Chọn độ khó ở menu sẽ **nạp một scene level khác nhau** (cả 3 đều sinh từ một scene
“master” bằng `Lab 2 → Build All Scenes`):

| Độ khó | Scene | Độ dài | Địa hình | Chướng ngại vật | Dốc nhảy | Tốc độ |
|---|---|---|---|---|---|---|
| **Easy** | `Level1_Easy` | ~405 đơn vị, **kết thúc trước vực lớn** | giãn ×2.15, ép Y 0.5 → rất thoải | **4** (1 cây) | 4 | ×0.8 |
| **Normal** | `Level1_Normal` | ~600 đơn vị | ×2.3, Y 0.85 | **8** (2 cây) | 6 | ×1.0 |
| **Hard** | `Level1_Hard` | ~635 đơn vị, **có vực lớn** | ×2.3, Y 1.15 → dốc hơn | **15** (5 cây) | 8 | ×1.3 |

- **Cả 3 đều để trống 90–120 đơn vị đầu** → đoạn khởi động luôn dễ.
- Hệ số tốc độ được **nhân lên trên thông số bạn chỉnh trong Inspector** (không ghi đè).
- Camera được kéo ra xa hơn (`orthographicSize = 12.5`) để nhìn được nhiều đường hơn.

---

## 4. Cấu trúc scene

### `MainMenu.unity`
Màn hình tiêu đề kiểu “mountain arcade” nền tối: nền gradient navy → xanh thép, dãy
núi tối + cây tuyết, nhân vật snowboarder tạo dáng trên dốc tuyết, vài bông tuyết
bay, logo **SNOW BOARDER** in nghiêng đậm (SNOW trắng / BOARDER xanh cyan), gạch
ngang cyan, tagline *“DROP IN. RIDE YOUR LINE.”*, điểm **BEST RUN** ở góc phải và các
nút bo góc: **PLAY >** (cam), **HOW TO PLAY**, **OPTIONS**, **QUIT**.
Panel Options: chọn độ khó (Easy / Normal / Hard) và thanh âm lượng.
Panel How To Play: liệt kê toàn bộ phím điều khiển.

### `Level1.unity`
Giữ nguyên cảnh trượt có sẵn (địa hình Sprite Shape, camera Cinemachine, nhân vật
“Barry”, vạch đích) và bổ sung:
* Canvas HUD,
* nhóm `Gameplay` chứa tuyết rơi (snowflake), power-up, chướng ngại vật, dốc nhảy,
* đối tượng `Weather` (hệ thống hạt tuyết),
* `Game Manager` và `Score Manager`,
* `TrickController` gắn vào Barry.

### `EndGame.unity`
Hiển thị kết quả (Hoàn thành / Thua), tổng điểm, số bông tuyết, số trick, combo cao
nhất, điểm cao nhất; nút Chơi lại / Về menu / Thoát.

---

## 4. Danh sách script

| File | Nhiệm vụ |
|---|---|
| `Scripts/PlayerController.cs` | Rẽ, tăng tốc, hook boost cho power-up |
| `Scripts/CrashDetector.cs` | Xử lý va chạm, chuỗi tai nạn |
| `Scripts/DustTrail.cs` | Bụi tuyết theo tốc độ |
| `Scripts/FinishLine.cs` | Trigger hoàn thành màn |
| `Scripts/Collectible.cs` | Nhặt bông tuyết |
| `Scripts/PowerUp.cs` | Tăng tốc / bất tử / thêm mạng |
| `Scripts/Ramp.cs` | Dốc nhảy (trigger phóng nhân vật) |
| `Scripts/Obstacle.cs` | Chướng ngại “chết” và “giảm tốc” |
| `Scripts/TrickController.cs` | Nhận diện trick trên không |
| `Scripts/ScoreManager.cs` | Điểm, combo, hệ số nhân |
| `Scripts/GameManager.cs` | Mạng, thua/thắng, chuyển scene |
| `Scripts/GameSettings.cs` | Độ khó, âm lượng, điểm cao |
| `Scripts/HUDController.cs` | HUD trong game |
| `Scripts/MainMenuController.cs` | Menu chính + options |
| `Scripts/EndGameController.cs` | Màn kết thúc |
| `Scripts/SnowfallController.cs` | Thời tiết động |
| `Scripts/Parallax.cs` | Tạo chiều sâu cho mây nền |
| `Scripts/InvincibilityBlink.cs` | Nhấp nháy nhân vật khi bất tử |
| `Editor/Lab2SceneBuilder.cs` | Sinh menu/HUD/đường trượt/màn kết |
| `Editor/Lab2SmokeTest.cs` | Test tự động (chạy không cần mở Unity) |
| `Editor/Lab2Screenshot.cs` | Chụp ảnh menu / màn chơi |
| `Editor/PhysicsSpy.cs` | Ghi log các cú nhảy vật lý bất thường (debug) |

---

## 5. Sinh lại scene (nếu cần)

Trong Unity: menu **Lab 2 → Build All Scenes**.

Hoặc chạy ẩn (không mở giao diện):

```
Unity.exe -batchmode -quit -projectPath "<đường dẫn project>" ^
          -executeMethod Lab2SceneBuilder.BuildAll
```

Có thể chạy test tự động (mô phỏng bấm Start rồi chơi đến hết màn, báo lỗi nếu có):

```
Unity.exe -batchmode -nographics -projectPath "<đường dẫn project>" ^
          -executeMethod Lab2SmokeTest.Run
```

Hoặc chụp ảnh menu/màn chơi (không dùng `-nographics`):

```
Unity.exe -batchmode -projectPath "<đường dẫn project>" ^
          -executeMethod Lab2Screenshot.Run
```

Menu **Lab 2** trong Unity có sẵn: Build All Scenes, Rebuild từng scene,
Build + Smoke Test, Build + Screenshot.

---

## 6. Những lỗi đã sửa ở vòng rà soát
| Lỗi | Nguyên nhân | Cách sửa |
|---|---|---|
| Nhân vật **rơi xuyên lòng đất** | Collider của nhân vật là một capsule rất mỏng (0.21) nên ở tốc độ ~32 đơn vị/giây nó lọt qua `EdgeCollider2D` của sườn dốc | Đổi thành collider "ván trượt" dày (3 × 0.6), bật Continuous collision + Interpolate + Never Sleep, hạ bước vật lý xuống 100 Hz, và thêm **lưới an toàn**: nếu vẫn rơi khỏi màn thì được đưa về vị trí an toàn gần nhất (không mất mạng) |
| Nhân vật **kẹt ở chướng ngại vật** | Game chỉ chạy một chiều nên không thể né ngang | Cây = vật cản cứng phải **nhảy** qua; đá → **gò tuyết xốp** đi xuyên được nhưng mất tốc độ |
| **Kẹt ở dốc nhảy** | Dốc là vật cứng, đi chậm thì bị chặn ở mép dốc | Dốc nhảy thành **trigger phóng** nhân vật lên (1 lần cho mỗi lần chạm) |
| **Kẹt trong vũng băng** | Trigger của vũng băng quá cao nên nhân vật luôn nằm trong đó; mỗi frame bị trừ 55% tốc độ → không thể thoát | Mỗi vật cản chỉ tác dụng **1 lần cho mỗi lần chạm** (`Obstacle.TryTakeHit`), hạ thấp vũng băng và bỏ lực bật lên |
| **Nhân vật bị bắn lên trời** | Bộ điều khiển nghiêng (carve) dùng lò xo quá cứng, đẩy ván xuyên vào tuyết → lực đẩy bùng nổ | Đổi sang bộ điều khiển **tốc độ quay giới hạn**, thêm giới hạn tốc độ/ vận tốc góc và chống cộng dồn lực nhảy |
| **Hiệu ứng va chạm khó nhìn** | Slow-motion + màn hình đỏ + chữ CRASH! | Bỏ hết, chỉ còn **nhân vật nhấp nháy đỏ** (báo mất máu) |
| **Nhảy / nghiêng không nhạy** | Điều kiện nhảy quá nghiêm, góc nghiêng chỉ ±22° | Thêm **đệm lệnh nhảy + coyote time**; nghiêng ±40° (400°/s), trên không 450°/s |
| **Vật thể tự biến mất** | Cây/vũng băng bị hủy khi bất tử; bông tuyết & power-up bị hủy khi ăn; mây nền trôi quá xa | Vật cản **không bị phá hủy** (chỉ tắt collider 1,5s rồi bật lại), bông tuyết/power-up **mọc lại**, giảm parallax mây |
| **Màn thua bị đè chữ** | Nút Retry/Menu nằm ngay trên dòng chữ thống kê | Bố cục lại: chữ phía trên, 3 nút bo góc phía dưới, panel phủ đủ |
| **Vụ nổ che hết màn hình** | Hiệu ứng va chạm sẵn có của level quá lớn, lại bị phóng to 2.2× | Đặt kích thước hạt **tuyệt đối nhỏ** (0.3–0.8) và giảm mật độ |
| **Cây quá to** | Cao 6 đơn vị | Giảm còn **3.5 đơn vị** (vẫn phải nhảy qua) |
| **Map quá dốc** | Địa hình gốc dốc | Ép dọc trục Y còn **75%** (giữ đỉnh núi ở chỗ cũ) → đồi thoải hơn |
| **Nhảy thấp** | Lực nhảy 14 | Tăng lên **18** (nhảy cao ~8 đơn vị) |
| **Xoay/nghiêng chưa mượt** | Dùng mô-men xoắn, bị va chạm với tuyết làm giật | Điều khiển **trực tiếp vận tốc góc** (có giới hạn gia tốc) → mượt tuyệt đối |
| **Xoay quá chậm** | Giá trị trong scene là số cũ (32°/260°/s) do Unity giữ giá trị đã lưu | Điều khiển trực tiếp vận tốc góc; **giá trị do bạn tự chỉnh trong Inspector được giữ nguyên**, builder không ghi đè nữa |
| **Chạm đầu xuống tuyết không mất máu** | Trước đây chỉ va cây mới mất máu | Thêm luật **tiếp đất bằng đầu** (ván xoay quá ±110° khi chạm tuyết) → mất 1 mạng, nhấp nháy đỏ |
| **Màn ngắn** | Địa hình gốc ngắn | Kéo dài địa hình theo trục X (×1.5) → màn dài gấp đôi (~400 đơn vị), vạch đích ở x=300 |
| **Dốc/vực nhiều** | Địa hình gốc dốc | Ép trục Y còn 0.85 → đồi thoải; **đoạn đầu để trống, không có chướng ngại vật** |
| **Chướng ngại vật nhiều** | 12 cái | Giảm còn **6** (3 cây + 3 vũng băng), rải đều |
| **Đích khó nhận ra** | Cột đỏ của level gốc | Thay bằng **băng-rôn ca-rô trắng–đen** trên 2 cột |
| Điểm tốc độ tăng quá nhanh | Hệ số quá cao và tính cả khi gần như đứng yên | Giảm hệ số và chỉ tính khi tốc độ > 10 |
| Nhân vật không rõ trạng thái bất tử | Không có phản hồi hình ảnh | Thêm hiệu ứng **nhấp nháy** + chữ "INVINCIBLE!" trên HUD |
| Chuyển màn đôi khi bị gọi 2 lần | Thiếu khoá | Thêm cờ `loading` trong `GameManager` |

## 7. Xử lý sự cố: bàn phím không ăn

Nếu bấm phím mà nhân vật không phản ứng, kiểm tra theo thứ tự:

1. **Bấm chuột vào cửa sổ Game** trước khi bấm phím. Nếu cửa sổ Game mất focus,
   Unity có thể **tạm dừng game** (chế độ *Play Focused* trên thanh công cụ Game).
2. Code đã có **2 đường đọc phím**: Input System mới (`Keyboard.current`) và
   **dự phòng bằng API Input cũ**. Để dự phòng hoạt động, project đã được đặt
   **Active Input Handling = Both** (Project Settings → Player).
3. Nếu vẫn không được, mở `Edit → Project Settings → Player → Active Input Handling`
   và chọn **Both**, rồi khởi động lại Unity.
4. Có thể kiểm tra nhanh bằng menu **Lab 2 → Build All Scenes then Input Test** —
   công cụ này tự bấm phím trái và in ra góc xoay của nhân vật.

## 8. Ghi chú kỹ thuật

* **Thông số của bạn được giữ nguyên**: builder **không ghi đè** các giá trị trong
  Inspector của `Player Controller` nữa, nên bạn chỉnh bao nhiêu lần build lại vẫn
  giữ đúng.
* **Lưu ý về tốc độ xoay**: nếu bạn để `Carve Turn Rate` quá lớn (ví dụ 100000 °/s ≈
  167 vòng/giây) thì khi tiếp đất với ván đang xoay, luật **“tiếp đất bằng đầu”** sẽ
  khiến bạn mất mạng liên tục. Giá trị dễ chơi khoảng **600–1200**.
* **Chỉnh độ nhạy khi đang chơi**: chọn `Barry` → component `Player Controller`:
  `Carve Turn Rate` (trên tuyết), `Max Turn Speed` (trên không),
  `Carve/Spin Acceleration` (độ bén), `Jump Force` (độ cao nhảy),
  `Head Crash Angle` (góc bị coi là tiếp đất bằng đầu, mặc định 110°).

* **Input System**: project được cấu hình **Both** (Input System mới + API cũ). Script
  ưu tiên `Keyboard.current` / `Gamepad.current`; nếu không có thiết bị bàn phím nào
  (ví dụ khi chạy batchmode) thì tự động **dự phòng sang `Input` cũ** để không bao giờ
  mất điều khiển.
* **Cinemachine**: gói `com.unity.cinemachine` đã được thêm vào
  `Packages/manifest.json` để rig camera có sẵn của level hoạt động.
* **Ảnh sinh tự động**: bông tuyết, biểu tượng power-up và dốc nhảy được script
  sinh ra thành file PNG trong `Assets/Sprites` (không cần tài nguyên ngoài).
* Thư mục `Lab2Assets/` ở gốc project là bản sao tài nguyên gốc bạn tải về; nội dung
  đã được đưa vào `Assets/` nên có thể xoá thư mục này cho gọn.
* Nút **Lab 2** trên thanh menu của Unity là công cụ sinh scene — không ảnh hưởng gì
  tới lúc chơi game.
