# Super Racing

## Các xe hiện có

Garage và Main Menu đọc xe từ `Assets/Data/GameCatalog.asset`. Mỗi xe có một `CarDefinition` riêng nên có thể chỉnh thông số trong Inspector mà không sửa code.

| Xe | Tốc độ tối đa | Motor Torque | Brake Torque | Góc lái | Grip |
|---|---:|---:|---:|---:|---:|
| SPORT GT | 180 km/h | 2200 | 3000 | 32° | 1.15 |
| Balanced | 170 km/h | 1800 | 3200 | 32° | 1.05 |
| Control | 145 km/h | 1500 | 3500 | 38° | 1.25 |

Ý nghĩa thông số:

- `Max Speed Kmh`: vận tốc tối đa.
- `Motor Torque`: lực tăng tốc; giá trị lớn giúp xe đạt tốc độ nhanh hơn.
- `Brake Torque`: lực phanh và lực giữ xe khi cuộc đua chưa bắt đầu.
- `Steering Angle`: góc đánh lái tối đa ở tốc độ thấp.
- `Grip`: độ bám ngang. Cao thì ổn định hơn; thấp thì dễ trượt và drift hơn.

Khi bắt đầu race, `RaceManager` gọi `VehicleController.ApplyStats(...)`, vì vậy thông số trong `CarDefinition` của xe đang chọn sẽ được áp dụng vào xe đua.

## Thêm một xe mới

### 1. Chuẩn bị prefab

1. Import model, texture và material vào `Assets/Game/Art/Vehicles/`.
2. Tạo prefab trong `Assets/Game/Prefabs/Vehicles/`.
3. Đặt pivot của root gần tâm xe, trục `Y` hướng lên và trục `Z` hướng về phía trước.
4. Root prefab cần có `Rigidbody`, collider thân xe và `VehicleController`.
5. Tạo bốn `WheelCollider` theo đúng cấu trúc sau để controller tự tìm được bánh:

```text
Vehicle Root
└── WheelColliders
    ├── WheelCollider_FL
    ├── WheelCollider_FR
    ├── WheelCollider_RL
    └── WheelCollider_RR
```

6. Gán bốn bánh hiển thị cho `WheelVisualSync` và kiểm tra hướng/scale của model.
7. Mở prefab và thử trong scene `Test_Vehicle` trước khi đưa vào catalog.

Bạn có thể duplicate một prefab xe hiện có để giữ đúng cấu trúc component và bánh xe, sau đó thay model/material.

### 2. Tạo dữ liệu xe

1. Trong Project window, nhấn chuột phải: `Create > Super Racing > Car Definition`.
2. Lưu asset mới trong `Assets/Data/`, ví dụ `NewCar.asset`.
3. Điền:
   - `Car Id`: mã duy nhất, viết thường, không trùng xe khác.
   - `Display Name`: tên hiển thị trong Garage.
   - `Vehicle Prefab`: prefab vừa chuẩn bị.
   - `Preview Sprite`: tùy chọn; Garage hiện dùng model 3D nên có thể để trống.
   - Các thông số lái ở phần `Driving Stats`.

### 3. Đưa xe vào game

1. Chọn `Assets/Data/GameCatalog.asset`.
2. Tăng `Cars > Size` thêm 1.
3. Kéo `CarDefinition` mới vào phần tử trống.
4. Mở Garage và chạy Play Mode. Xe mới sẽ xuất hiện trong danh sách chọn xe mà không cần sửa code.

Thứ tự trong mảng `Cars` cũng là thứ tự hiển thị trong Garage. Phần tử đầu tiên là xe mặc định khi người chơi chưa chọn xe.

Garage chỉ đưa một đại diện của mỗi bộ mesh vào catalog. Hai `CarDefinition` dùng cùng toàn bộ mesh chỉ được xem là các biến thể material/thông số, không phải hai mẫu xe khác nhau.

## Preview xe

- Xe trong Main Menu và Garage tự xoay khi không thao tác.
- Kéo chuột ngang trên vùng xe để xoay thủ công 360°.
- Khi thả chuột, xe tiếp tục tự xoay từ đúng góc hiện tại.
- Góc nhìn ban đầu của Garage và Main Menu đều là `8°` quanh trục `Y`.
