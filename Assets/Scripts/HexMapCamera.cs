using UnityEngine;

namespace HexMap
{
    /// <summary>
    /// 摄像机控制器
    /// </summary>
    public class HexMapCamera : MonoBehaviour
    {
        public static HexMapCamera Instance { get; private set; }

        [SerializeField] private float stickMinZoom = -250f; // 最小缩放值
        [SerializeField] private float stickMaxZoom = -45f; // 最大缩放值
        [SerializeField] private float swivelMinZoom = 90f; // 在最小缩放值下摄像机的旋转角度
        [SerializeField] private float swivelMaxZoom = 45f; // 在最大缩放值下摄像机的旋转角度
        [SerializeField] private float moveSpeedMinZoom = 400f; // 在最小缩放值下摄像机的移动速度
        [SerializeField] private float moveSpeedMaxZoom = 100f; // 在最大缩放值下摄像机的移动速度
        [SerializeField, Range(0.1f, 2f)] private float zoomSensitivity = 0.5f;
        [SerializeField] private float rotationSpeed = 180f;
        [SerializeField] private HexGrid grid = default;

        private Transform swivel; // 控制摄像机的旋转
        private Transform stick; // 控制摄像机的距离
        private float zoom = 1f; // 缩放值，取值范围[0 - 1]，0 表示最远，1 表示最近
        private bool isLocked;

        private float rotationAngle;

        public void Lock()
        {
            isLocked = true;
        }

        public void Unlock()
        {
            isLocked = false;
        }

        public void ValidatePosition()
        {
            AdjustPosition(0f, 0f);
        }

        private void Awake()
        {
            swivel = transform.GetChild(0);
            stick = swivel.GetChild(0);
        }

        private void OnEnable()
        {
            Instance = this;
        }

        private void Update()
        {
            if (isLocked) return;

            float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
            if (zoomDelta != 0f)
            {
                AdjustZoom(zoomDelta);
            }

            float rotationDelta = Input.GetAxis("Rotation");
            if (rotationDelta != 0f)
            {
                AdjustRotation(rotationDelta);
            }

            float xDelta = Input.GetAxis("Horizontal");
            float zDelta = Input.GetAxis("Vertical");
            if (xDelta != 0f || zDelta != 0f)
            {
                AdjustPosition(xDelta, zDelta);
            }
        }

        private void AdjustZoom(float delta)
        {
            zoom = Mathf.Clamp01(zoom + delta * zoomSensitivity);

            float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
            stick.localPosition = new Vector3(0f, 0f, distance);

            float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
            swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }

        private void AdjustRotation(float delta)
        {
            rotationAngle += delta * rotationSpeed * Time.deltaTime;

            if (rotationAngle < 0f)
            {
                rotationAngle += 360f;
            }
            else if (rotationAngle >= 360f)
            {
                rotationAngle -= 360f;
            }

            transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
        }

        private void AdjustPosition(float xDelta, float zDelta)
        {
            Vector3 direction = transform.localRotation * new Vector3(xDelta, 0f, zDelta).normalized;
            float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
            float moveSpeed = Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom);
            float distance = moveSpeed * damping * Time.deltaTime;

            Vector3 position = transform.localPosition;
            position += direction * distance;
            transform.localPosition = ClampPosition(position);
        }

        /// <summary>
        /// 将位置限制在地图范围内
        /// </summary>
        private Vector3 ClampPosition(Vector3 position)
        {
            float xMax = (grid.CellCountX - 0.5f) * (2f * HexMetrics.innerRadius);
            position.x = Mathf.Clamp(position.x, 0f, xMax);

            float zMax = (grid.CellCountZ - 1f) * (1.5f * HexMetrics.outerRadius);
            position.z = Mathf.Clamp(position.z, 0f, zMax);

            return position;
        }
    }
}