# Silent Castle – Hệ thống AI quái (Unity + Photon Fusion 2)

## Cấu trúc file

| File | Vai trò |
|---|---|
| `IMonsterTarget.cs` | Interface mục tiêu (Player/Cửa) + `IDayNightProvider` (nối vào hệ thống ngày đêm có sẵn) |
| `WorldContext.cs` | Dữ liệu world dùng chung: player, cửa, vùng thành, ngày/đêm, lãnh địa, claim. Có hàm chọn lãnh địa thông minh `PickTerritory` |
| `MonsterDefinition.cs` | ScriptableObject: toàn bộ chỉ số, công thức level, ngưỡng bỏ chạy, thông số ngày/đêm |
| `TargetSelector.cs` | `TargetingProfile` + thuật toán chấm điểm chọn mục tiêu |
| `MonsterBrain.cs` | NetworkBehaviour chính của mỗi con quái |
| `MonsterStates.cs` | FSM: SelectTarget → Chase → Attack → Flee → Heal → (lặp) / Retreated / Dead |
| `MonsterSkill.cs` | Khung skill + kỹ năng tích nộ (Rage) |
| `StatModifiers.cs` | Hệ số nhân tạm thời (speed, attack speed, damage) |
| `MonsterMover.cs` | Bọc NavMeshAgent |
| `NetworkMonsterTargetBase.cs` | Lớp nền cho Player/Gate, tự đăng ký vào WorldContext |

## Cài đặt

1. Scene: tạo object `WorldContext`. Gán component ngày đêm của bạn (cho nó implement `IDayNightProvider`), collider bao thành (Box/Sphere), danh sách điểm lãnh địa.
2. Player và Gate: kế thừa `NetworkMonsterTargetBase`, nối vào hệ thống máu hiện có.
3. Prefab quái: `NetworkObject`, `NetworkTransform`, `NavMeshAgent`, `MonsterMover`, `MonsterBrain`. Spawn bằng `Runner.Spawn` trên host, vào vị trí nằm trên NavMesh.
4. Tạo asset: `Monster Definition`, `Targeting Profile`, `Skills/Rage`. Kéo Rage vào `skills` của Definition.
5. Khi player đánh trúng quái (chạy trên host): `brain.ReceiveDamage(dmg)`.

## Cách từng yêu cầu được đáp ứng

- **Chọn target**: có player sống ngoài thành thì chọn player, không thì chọn cửa. Hết cửa thì đánh player trong thành. Player chết bị loại.
- **Cân bằng nhiều người chơi**: điểm = −khoảng cách − phạt theo số quái khác đang nhắm (càng đông phạt càng nặng) + thưởng bám mục tiêu (giảm dần theo thời gian) + thưởng mục tiêu yếu + nhiễu riêng từng con. Chỉ đổi mục tiêu khi mục tiêu mới hơn hẳn (`switchMargin`) và đã đánh tối thiểu `minCommitSeconds`. Kết quả: quái dàn trải ra các nhà, không dồn một nhà, cũng không chạy qua chạy lại.
- **Bỏ chạy**: ngưỡng máu = lerp(`fleeHpAtLevel1`, `fleeHpAtMaxLevel`) × cá tính riêng ±20%. Đang nộ thì ngưỡng giảm một nửa nên càng lì.
- **Hồi máu**: chạy tới lãnh địa, hồi đầy rồi quay lại `SelectTarget`.
- **Lên cấp**: mỗi đòn trúng cộng `expPerHit`, giết người cộng `expPerKill`. Đủ exp thì tăng level, tăng chỉ số, hồi một phần máu.
- **Ngày/đêm**: định kỳ ước lượng thời gian chạy về lãnh địa bằng đường đi NavMesh thật. Nếu `SecondsUntilDawn` ≤ thời gian về × hệ số an toàn + đệm thì bỏ chạy (lý do Dawn), tới nơi thì ẩn đi (despawn). Còn quá ít đêm thì không xuất kích nữa. Ở ngoài nắng quá `daylightSurvivalSeconds` thì chết (`DaylightBurn01` để làm hiệu ứng cháy).
- **Nộ**: mỗi đòn tích nộ, đầy thì tăng tốc chạy, tốc độ đánh, sát thương trong một khoảng thời gian.
- **Mở rộng**: quái có skill mới thì kế thừa `MonsterSkillDef` và `MonsterSkillRuntime` (hook: `TryCast`, `OnDealtDamage`, `OnTick`, `OnLevelUp`, `BlocksMovement`). Hành vi khác thì override `CreateState` trong lớp con của `MonsterBrain`. Mỗi loại quái có `MonsterDefinition` và `TargetingProfile` riêng. Mỗi con có state, exp, skill, cá tính độc lập.

## Lưu ý Fusion 2

- Giả định Host/Server mode: AI chỉ chạy khi `HasStateAuthority`, client chỉ nhận `[Networked]` và RPC hiệu ứng. Shared mode thì cho master client chạy AI và gửi damage tới player qua RPC.
- Timer nội bộ của AI không đồng bộ. Nếu dùng host migration thì cần chuyển các biến đó sang `[Networked]`.
- `Claim` và `Territory occupancy` nằm trong `WorldContext` trên host nên mọi quái trên host chia sẻ chung.
- Với tải lớn (hàng chục quái), có thể tăng khoảng thời gian `UpdateHomeEstimate` và `reevalInterval`.

## Tinh chỉnh nhanh

- Quái nhát hơn: tăng `fleeHpAtLevel1`. Quái lì hơn: giảm `fleeHpAtMaxLevel`.
- Dàn trải mạnh hơn: tăng `loadWeight`. Bám mục tiêu lâu hơn: tăng `stickBonus`, `minCommitSeconds`.
- Về sớm hơn khi sắp sáng: tăng `dawnSafetyMultiplier` hoặc `dawnSafetyBufferSeconds`.
