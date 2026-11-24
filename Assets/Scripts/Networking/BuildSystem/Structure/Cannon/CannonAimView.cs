using Cinemachine;   // Cinemachine 2.x. Nếu dùng Cinemachine 3 xem ghi chú ở cuối file
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Interaction
{
    /// <summary>
    /// Phần hiển thị + đọc chuột của súng pháo, CHỈ chạy ở máy người điều khiển (local).
    /// Được CannonAction bật/tắt qua Show/Hide. Không phải NetworkBehaviour.
    /// - Vẽ vòng tầm bắn quanh pháo (LineRenderer tạo bằng code).
    /// - Tâm ngắm bám theo chuột trên mặt phẳng XZ, bị kẹp trong [minRange, fireRange].
    /// - Click chuột => controller.SendPayload(action, Position).
    /// - Camera: tạo 1 Virtual Camera riêng (ưu tiên cao) bay lên ngay trên đầu pháo rồi cúi xuống
    ///   nhìn thẳng đứng (topdown). Kết thúc: bay ngược về đúng vị trí + góc của vcam trong Player.
    /// </summary>
    public class CannonAimView : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private bool _ignoreWhenOverUI = true;
        [SerializeField] private Camera _camera;                // để trống = Camera.main (dùng để raycast chuột)

        [Header("Visual")]
        [SerializeField] private Material _lineMaterial;        // để trống = Sprites/Default
        [SerializeField] private Color _rangeColor = new Color(1f, 0.9f, 0.2f, 0.9f);
        [SerializeField] private Color _crosshairOk = new Color(0.3f, 1f, 0.3f, 1f);
        [SerializeField] private Color _crosshairOnCooldown = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField, Min(0.01f)] private float _lineWidth = 0.08f;
        [SerializeField, Min(8)] private int _circleSegments = 64;
        [SerializeField] private float _groundOffset = 0.05f;   // nhấc lên khỏi mặt đất tránh z-fight
        [Tooltip("Tuỳ chọn: object tâm ngắm của bạn (sprite nằm ngang...). Để trống = tự vẽ bằng LineRenderer.")]
        [SerializeField] private Transform _customCrosshair;

        [Header("Camera Zoom (Cinemachine 2.x, Perspective)")]
        [SerializeField] private bool _zoomCamera = true;
        [Tooltip("Virtual Camera góc nghiêng (kiểu LoL) trong Player. Để trống = tự tìm trong Player đang điều khiển pháo.")]
        [SerializeField] private CinemachineVirtualCamera _playerVcam;
        [Tooltip("CinemachineBrain trên Main Camera. Để trống = lấy từ Camera.main.")]
        [SerializeField] private CinemachineBrain _brain;

        [Tooltip("Bật = tự tính độ cao để vòng tầm bắn vừa khít chiều dọc màn hình")]
        [SerializeField] private bool _autoFitRange = true;
        [Tooltip("Hệ số dư quanh vòng tầm bắn khi autoFit (1.15 = dư 15%)")]
        [SerializeField, Min(1f)] private float _fitMargin = 1.15f;
        [Tooltip("Độ cao camera phía trên pháo (mét) khi tắt autoFit")]
        [SerializeField, Min(1f)] private float _manualHeight = 20f;
        [SerializeField, Min(1f)] private float _minHeight = 8f;

        [Tooltip("Bật = giữ hướng (yaw) của camera góc nghiêng, chỉ cúi xuống. Tắt = dùng Fixed Yaw bên dưới (0 = đỉnh màn hình hướng +Z, 12h).")]
        [SerializeField] private bool _keepOriginalYaw = true;
        [SerializeField] private float _fixedYaw = 0f;

        [Tooltip("Thời gian làm mượt khi bay lên / bay về (giây)")]
        [SerializeField, Min(0.05f)] private float _moveSmoothTime = 0.45f;
        [Tooltip("Tốc độ xoay: cao = xoay nhanh hơn")]
        [SerializeField, Min(0.5f)] private float _rotateSharpness = 6f;
        [Tooltip("Priority của vcam pháo = Priority vcam Player + giá trị này")]
        [SerializeField] private int _vcamPriorityBoost = 20;

        private InteractionController _controller;
        private CannonAction _action;
        private bool _active;

        // ---- Zoom state ----
        private enum ZoomPhase { Idle, ToTopDown, Returning }
        private ZoomPhase _phase = ZoomPhase.Idle;

        private CinemachineVirtualCamera _cannonVcam;     // vcam riêng, tạo bằng code
        private CinemachineVirtualCamera _livePlayerVcam; // vcam gốc (Player) đang bị thay thế tạm thời
        private CinemachineBrain _zoomBrain;
        private CinemachineBlendDefinition _savedBlend;
        private bool _blendSaved;
        private int _blendRestoreFrame = -1;
        private CinemachineVirtualCameraBase.StandbyUpdateMode _savedStandby;

        private Vector3 _topPos;
        private Quaternion _topRot;
        private Vector3 _posVel;

        private LineRenderer _rangeOuter;
        private LineRenderer _rangeInner;
        private Transform _crosshairRoot;
        private LineRenderer[] _crosshairLines;

        public Vector3 AimPoint { get; private set; }

        // ================= API cho CannonAction =================

        public void Show(InteractionController controller, CannonAction action)
        {
            _controller = controller;
            _action = action;
            _active = true;
            BuildVisualsIfNeeded();
            SetVisible(true);
            DrawRange();
            BeginZoom();
        }

        public void Hide()
        {
            _active = false;
            _controller = null;
            _action = null;
            SetVisible(false);
            EndZoom();
        }

        private void OnDisable()
        {
            Hide();
            SnapRestoreZoom();   // component tắt thì Update không chạy nữa => trả camera ngay
        }

        private void OnDestroy()
        {
            if (_cannonVcam != null) Destroy(_cannonVcam.gameObject);
        }

        // ================= Camera zoom (Cinemachine) =================

        private void BeginZoom()
        {
            if (!_zoomCamera) return;
            FlushBlendRestore();   // nếu lần trước còn đang chờ trả blend thì trả luôn

            // Show lại khi đang bay về: chỉ đổi hướng bay, vcam pháo vẫn đang live
            if (_phase == ZoomPhase.Returning)
            {
                ComputeTopPose(_cannonVcam.transform.rotation.eulerAngles.y);
                _phase = ZoomPhase.ToTopDown;
                return;
            }
            if (_phase != ZoomPhase.Idle) return;

            var player = _playerVcam != null
                ? _playerVcam
                : Camera.main.transform.root.GetComponentInChildren<CinemachineVirtualCamera>();
            var brain = _brain != null
                ? _brain
                : (Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null);

            if (player == null || brain == null)
            {
                Debug.LogWarning("[CannonAim] Thiếu CinemachineVirtualCamera trong Player hoặc CinemachineBrain trên Main Camera, bỏ qua zoom", this);
                return;
            }

            _livePlayerVcam = player;
            _zoomBrain = brain;

            // 1) Vcam riêng (không Body/Aim => tự do đặt transform)
            if (_cannonVcam == null)
            {
                var go = new GameObject("CannonTopDownVcam");
                _cannonVcam = go.AddComponent<CinemachineVirtualCamera>();
                go.SetActive(false);
            }

            // 2) Bắt đầu đúng pose camera đang hiển thị để không giật
            var startPos = brain.transform.position;
            var startRot = brain.transform.rotation;
            _cannonVcam.transform.SetPositionAndRotation(startPos, startRot);

            var lens = player.m_Lens;
            lens.Dutch = 0f;
            _cannonVcam.m_Lens = lens;
            _cannonVcam.Priority = player.Priority + _vcamPriorityBoost;

            // 3) Brain cắt thẳng (Cut) sang vcam pháo, vì tự mình làm mượt; trả blend cũ sau khi xong
            _savedBlend = brain.m_DefaultBlend;
            _blendSaved = true;
            brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f);

            // 4) Vcam Player cần tiếp tục cập nhật pose khi standby để lúc về biết đích đến chính xác
            _savedStandby = player.m_StandbyUpdate;
            player.m_StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.Always;

            _posVel = Vector3.zero;
            float yaw = startRot.eulerAngles.y;
            ComputeTopPose(yaw);

            _cannonVcam.gameObject.SetActive(true);
            _phase = ZoomPhase.ToTopDown;
        }

        /// <summary>Điểm đến: ngay trên đầu pháo (theo trục đứng), nhìn thẳng xuống.</summary>
        private void ComputeTopPose(float originalYaw)
        {
            var c = _action != null ? _action.Center : transform.position;

            float height = _manualHeight;
            if (_autoFitRange && _action != null && _livePlayerVcam != null)
            {
                float halfFov = Mathf.Max(5f, _livePlayerVcam.m_Lens.FieldOfView * 0.5f) * Mathf.Deg2Rad;
                height = _action.FireRange * _fitMargin / Mathf.Tan(halfFov);
            }
            height = Mathf.Max(height, _minHeight);

            _topPos = new Vector3(c.x, c.y + height, c.z);

            float yaw = _keepOriginalYaw ? originalYaw : _fixedYaw;
            _topRot = Quaternion.Euler(90f, yaw, 0f);   // forward = xuống, đỉnh màn hình = hướng yaw (0 = 12h = +Z)
        }

        private void EndZoom()
        {
            if (_phase == ZoomPhase.ToTopDown) _phase = ZoomPhase.Returning;
        }

        private void ZoomTick()
        {
            if (_blendRestoreFrame >= 0 && Time.frameCount >= _blendRestoreFrame) FlushBlendRestore();
            if (_phase == ZoomPhase.Idle || _cannonVcam == null) return;

            Vector3 goalPos;
            Quaternion goalRot;
            if (_phase == ZoomPhase.ToTopDown)
            {
                goalPos = _topPos;
                goalRot = _topRot;
            }
            else
            {
                // Đích khi về = pose HIỆN TẠI của vcam Player (nó vẫn đi theo nhân vật)
                var st = _livePlayerVcam.State;
                goalPos = st.FinalPosition;
                goalRot = st.FinalOrientation;
            }

            var t = _cannonVcam.transform;
            t.position = Vector3.SmoothDamp(t.position, goalPos, ref _posVel, _moveSmoothTime);
            t.rotation = Quaternion.Slerp(t.rotation, goalRot, 1f - Mathf.Exp(-_rotateSharpness * Time.deltaTime));

            if (_phase == ZoomPhase.Returning
                && (t.position - goalPos).sqrMagnitude < 0.0004f
                && Quaternion.Angle(t.rotation, goalRot) < 0.3f)
            {
                FinishZoom();
                _blendRestoreFrame = Time.frameCount + 3;   // chờ Brain xử lý xong cú Cut rồi mới trả blend
            }
        }

        /// <summary>Tắt vcam pháo (Brain tự cắt về vcam Player, pose trùng nhau nên không giật).</summary>
        private void FinishZoom()
        {
            if (_cannonVcam != null) _cannonVcam.gameObject.SetActive(false);
            if (_livePlayerVcam != null) _livePlayerVcam.m_StandbyUpdate = _savedStandby;
            _phase = ZoomPhase.Idle;
        }

        private void FlushBlendRestore()
        {
            if (_blendSaved && _zoomBrain != null) _zoomBrain.m_DefaultBlend = _savedBlend;
            _blendSaved = false;
            _blendRestoreFrame = -1;
        }

        private void SnapRestoreZoom()
        {
            if (_phase != ZoomPhase.Idle) FinishZoom();
            FlushBlendRestore();
        }

        // ================= Loop =================

        private void Update()
        {
            ZoomTick();   // phải chạy cả khi !_active để animate bay về

            if (!_active || _action == null || _controller == null) return;

            var cam = _camera != null ? _camera : Camera.main;
            if (cam == null) return;

            var center = _action.Center;
            var plane = new Plane(Vector3.up, new Vector3(0f, center.y, 0f));
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!plane.Raycast(ray, out float enter)) return;

            var hit = ray.GetPoint(enter);
            var flat = hit - center; flat.y = 0f;
            float dist = flat.magnitude;
            var dir = dist > 0.0001f ? flat / dist : Vector3.forward;
            dist = Mathf.Clamp(dist, _action.MinRange, _action.FireRange);

            AimPoint = center + dir * dist;

            UpdateCrosshair(AimPoint);

            if (Input.GetMouseButtonDown(0))
            {
                if (_ignoreWhenOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

                _controller.SendPayload(_action, new InteractionPayload
                {
                    Id = 0,
                    Value = 0f,
                    Position = AimPoint
                });
                Debug.Log($"[CannonAim] Gửi lệnh bắn tại {AimPoint}", this);
            }
        }

        // ================= Visual =================

        private void DrawRange()
        {
            var c = _action.Center;
            float y = c.y + _groundOffset;
            FillCircle(_rangeOuter, c, _action.FireRange, y);
            FillCircle(_rangeInner, c, _action.MinRange, y);
            _rangeInner.gameObject.SetActive(_action.MinRange > 0.05f);
        }

        private void FillCircle(LineRenderer lr, Vector3 c, float radius, float y)
        {
            lr.positionCount = _circleSegments;
            for (int i = 0; i < _circleSegments; i++)
            {
                float a = (i / (float)_circleSegments) * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(c.x + Mathf.Cos(a) * radius, y, c.z + Mathf.Sin(a) * radius));
            }
        }

        private void UpdateCrosshair(Vector3 p)
        {
            if (_crosshairRoot == null) return;
            _crosshairRoot.position = new Vector3(p.x, p.y + _groundOffset, p.z);

            if (_crosshairLines == null) return;
            bool ready = _action.Cooldown.ExpiredOrNotRunning(_controller.Runner);
            var col = ready ? _crosshairOk : _crosshairOnCooldown;
            for (int i = 0; i < _crosshairLines.Length; i++)
            {
                _crosshairLines[i].startColor = col;
                _crosshairLines[i].endColor = col;
            }
        }

        private void SetVisible(bool v)
        {
            if (_rangeOuter != null) _rangeOuter.gameObject.SetActive(v);
            if (_rangeInner != null) _rangeInner.gameObject.SetActive(v);
            if (_crosshairRoot != null) _crosshairRoot.gameObject.SetActive(v);
        }

        private void BuildVisualsIfNeeded()
        {
            if (_rangeOuter != null) return;

            if (_lineMaterial == null) _lineMaterial = new Material(Shader.Find("Sprites/Default"));

            _rangeOuter = CreateLine("RangeOuter", transform, _rangeColor, loop: true);
            _rangeInner = CreateLine("RangeInner", transform, new Color(_rangeColor.r, _rangeColor.g, _rangeColor.b, 0.5f), loop: true);

            if (_customCrosshair != null)
            {
                _crosshairRoot = _customCrosshair;
                _crosshairLines = null;
            }
            else
            {
                var root = new GameObject("Crosshair").transform;
                root.SetParent(transform, false);
                _crosshairRoot = root;

                const float r = 0.4f;
                var circle = CreateLine("Circle", root, _crosshairOk, loop: true, worldSpace: false);
                circle.positionCount = 24;
                for (int i = 0; i < 24; i++)
                {
                    float a = (i / 24f) * Mathf.PI * 2f;
                    circle.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
                }

                var h = CreateLine("H", root, _crosshairOk, loop: false, worldSpace: false);
                h.positionCount = 2;
                h.SetPosition(0, new Vector3(-r * 1.6f, 0f, 0f));
                h.SetPosition(1, new Vector3(r * 1.6f, 0f, 0f));

                var v = CreateLine("V", root, _crosshairOk, loop: false, worldSpace: false);
                v.positionCount = 2;
                v.SetPosition(0, new Vector3(0f, 0f, -r * 1.6f));
                v.SetPosition(1, new Vector3(0f, 0f, r * 1.6f));

                _crosshairLines = new[] { circle, h, v };
            }
        }

        private LineRenderer CreateLine(string name, Transform parent, Color color, bool loop, bool worldSpace = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = _lineMaterial;
            lr.useWorldSpace = worldSpace;
            lr.loop = loop;
            lr.widthMultiplier = _lineWidth;
            lr.startColor = lr.endColor = color;
            lr.alignment = LineAlignment.TransformZ;   // nằm phẳng trên mặt đất
            lr.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }
    }
}

/* ===== Ghi chú Cinemachine 3 (Unity.Cinemachine) =====
   File này viết cho Cinemachine 2.x. Nếu project dùng Cinemachine 3 thì cần đổi tên API:
   - using Cinemachine;                         ->  using Unity.Cinemachine;
   - CinemachineVirtualCamera                   ->  CinemachineCamera
   - m_Lens                                     ->  Lens
   - Priority (int)                             ->  Priority.Value
   - m_StandbyUpdate / StandbyUpdateMode        ->  StandbyUpdate / enum tương ứng
   - CinemachineBrain.m_DefaultBlend            ->  DefaultBlend
   Nếu gặp lỗi biên dịch thì gửi lại thông báo lỗi để chỉnh theo đúng phiên bản bạn đang dùng.
*/
