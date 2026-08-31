using UnityEngine;

namespace Sipoga.Tactics
{
    [RequireComponent(typeof(Camera))]
    public sealed class TacticalPrototypeCamera : MonoBehaviour
    {
        private Camera _camera;
        private TacticalUnitAgent _selectedAgent;
        private bool _isTacticalView = true;
        private float _tacticalZoom = 12.2f;

        public bool IsTacticalView
        {
            get { return _isTacticalView; }
        }

        public Vector3 PlanarForward
        {
            get
            {
                Vector3 forward = transform.forward;
                forward.y = 0f;
                return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            }
        }

        public Vector3 PlanarRight
        {
            get
            {
                Vector3 right = transform.right;
                right.y = 0f;
                return right.sqrMagnitude > 0.001f ? right.normalized : Vector3.right;
            }
        }

        public void Initialize()
        {
            _camera = GetComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.018f, 0.024f, 0.030f);
            _camera.allowHDR = true;
            _camera.fieldOfView = 64f;
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = 120f;
            TacticalPrototypeVisual.ConfigurePipelineCamera(gameObject);
            _camera.orthographic = true;
            _camera.orthographicSize = _tacticalZoom;
            gameObject.tag = "MainCamera";

            if (GetComponent<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }

            SnapToCurrentMode();
        }

        public void SetSelectedAgent(TacticalUnitAgent agent)
        {
            _selectedAgent = agent;
        }

        public void ToggleMode()
        {
            SetTacticalView(!_isTacticalView);
        }

        public void SetTacticalView(bool tacticalView)
        {
            _isTacticalView = tacticalView;
            if (_camera != null)
            {
                _camera.orthographic = tacticalView;
            }
        }

        private void Update()
        {
            if (_camera == null || !_isTacticalView)
            {
                return;
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _tacticalZoom = Mathf.Clamp(_tacticalZoom - scroll * 0.8f, 8.5f, 17f);
            }
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                return;
            }

            if (_isTacticalView)
            {
                UpdateTacticalCamera();
            }
            else
            {
                UpdateOperatorCamera();
            }
        }

        private void UpdateTacticalCamera()
        {
            Vector3 targetPosition = new Vector3(0f, 19.5f, -12.5f);
            Quaternion targetRotation = Quaternion.LookRotation(
                new Vector3(0f, 1.3f, 0.6f) - targetPosition,
                Vector3.up);
            float positionBlend = 1f - Mathf.Exp(-6.5f * Time.unscaledDeltaTime);
            float rotationBlend = 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPosition, positionBlend);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationBlend);
            _camera.orthographic = true;
            _camera.orthographicSize = Mathf.Lerp(
                _camera.orthographicSize,
                _tacticalZoom,
                1f - Mathf.Exp(-9f * Time.unscaledDeltaTime));
        }

        private void UpdateOperatorCamera()
        {
            if (_selectedAgent == null)
            {
                return;
            }

            _camera.orthographic = false;
            Vector3 forward = _selectedAgent.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            Vector3 focus = _selectedAgent.transform.position + Vector3.up * 0.45f;
            Vector3 targetPosition = focus - forward * 4.2f + Vector3.up * 2.7f;
            Quaternion targetRotation = Quaternion.LookRotation(
                focus + forward * 1.7f - targetPosition,
                Vector3.up);
            float positionBlend = 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime);
            float rotationBlend = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPosition, positionBlend);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationBlend);
            _camera.fieldOfView = Mathf.Lerp(
                _camera.fieldOfView,
                68f,
                1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        }

        private void SnapToCurrentMode()
        {
            if (_isTacticalView)
            {
                transform.position = new Vector3(0f, 19.5f, -12.5f);
                transform.rotation = Quaternion.LookRotation(
                    new Vector3(0f, 1.3f, 0.6f) - transform.position,
                    Vector3.up);
            }
            else if (_selectedAgent != null)
            {
                Vector3 focus = _selectedAgent.transform.position + Vector3.up * 0.45f;
                transform.position = focus - _selectedAgent.transform.forward * 4.2f + Vector3.up * 2.7f;
                transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
            }
        }
    }
}
