using UnityEngine;

/// <summary>
/// Configura a camera em perspectiva isometrica para visualizacao 3D das cartas.
/// </summary>
[RequireComponent(typeof(Camera))]
public class IsometricCameraSetup : MonoBehaviour
{
    [Header("Camera Configuration")]
    [SerializeField, Range(10f, 60f)] private float cameraAngleX = 30f;
    [SerializeField, Range(-45f, 45f)] private float cameraAngleY = 0f;
    [SerializeField] private float cameraDistance = 9.5f;
    [SerializeField] private Vector3 lookAtPoint = new Vector3(0f, -1.6f, 0f);
    [SerializeField, Range(20f, 85f)] private float fieldOfView = 46f;
    [SerializeField] private bool applyOnStart = true;

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Start()
    {
        if (applyOnStart)
            ApplyCameraConfiguration();
    }

    private void OnEnable()
    {
        if (Application.isPlaying && applyOnStart)
            ApplyCameraConfiguration();
    }

    /// <summary>
    /// Apply the isometric camera configuration.
    /// </summary>
    public void ApplyCameraConfiguration()
    {
        if (_camera == null)
            _camera = GetComponent<Camera>();
        if (_camera == null)
            return;

        _camera.orthographic = false;
        _camera.fieldOfView = Mathf.Clamp(fieldOfView, 20f, 85f);

        float angleXRad = cameraAngleX * Mathf.Deg2Rad;
        float angleYRad = cameraAngleY * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Sin(angleYRad) * cameraDistance,
            Mathf.Sin(angleXRad) * cameraDistance,
            -Mathf.Cos(angleXRad) * Mathf.Cos(angleYRad) * cameraDistance
        );

        transform.position = lookAtPoint + offset;
        transform.rotation = Quaternion.LookRotation(lookAtPoint - transform.position, Vector3.up);
    }

    public void Configure(float angleX, float angleY, float distance, float fov, Vector3 targetLookAt)
    {
        cameraAngleX = Mathf.Clamp(angleX, 10f, 60f);
        cameraAngleY = Mathf.Clamp(angleY, -45f, 45f);
        cameraDistance = Mathf.Max(0.2f, distance);
        fieldOfView = Mathf.Clamp(fov, 20f, 85f);
        lookAtPoint = targetLookAt;
        ApplyCameraConfiguration();
    }

    /// <summary>
    /// Update camera angle at runtime.
    /// </summary>
    public void SetCameraAngle(float angleX, float angleY)
    {
        cameraAngleX = Mathf.Clamp(angleX, 10f, 60f);
        cameraAngleY = Mathf.Clamp(angleY, -45f, 45f);
        ApplyCameraConfiguration();
    }

    /// <summary>
    /// Update camera distance at runtime.
    /// </summary>
    public void SetCameraDistance(float distance)
    {
        cameraDistance = Mathf.Max(0.2f, distance);
        ApplyCameraConfiguration();
    }

    public void SetLookAtPoint(Vector3 target)
    {
        lookAtPoint = target;
        ApplyCameraConfiguration();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(lookAtPoint, 0.12f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, lookAtPoint);
    }
}
