using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class FaceCamera : MonoBehaviour
{
    [Header("Target Camera")]
    [Tooltip("Leave empty to automatically use Camera.main.")]
    public Transform targetCamera;

    [Tooltip("Automatically find Camera.main if Target Camera is empty.")]
    public bool autoFindMainCamera = true;

    [Tooltip("In Edit Mode, use the Scene View camera so you can preview the rotation.")]
    public bool useSceneViewCameraInEditMode = true;

    [Header("Facing")]
    [Tooltip("Makes the object look directly at the camera.")]
    public bool lookAtCamera = true;

    [Tooltip("Good for UI menus. Keeps the UI upright and only rotates around Y.")]
    public bool onlyRotateAroundY = true;

    [Tooltip("Automatically flips the object 180 degrees. Usually needed for World Space UI.")]
    public bool autoFlipForWorldSpaceUI = true;

    [Tooltip("Extra rotation if you need small manual adjustments.")]
    public Vector3 extraRotationOffset = Vector3.zero;

    [Header("Smoothing")]
    public bool smoothRotation = true;

    [Tooltip("Higher value = faster rotation.")]
    [Min(0.01f)]
    public float rotationSpeed = 12f;

    [Header("Optional Distance Scaling")]
    [Tooltip("Makes the UI scale based on distance from the camera.")]
    public bool scaleWithDistance = false;

    [Tooltip("Base scale used when scaling with distance.")]
    public Vector3 baseScale = Vector3.one;

    [Min(0.01f)]
    public float distanceScaleMultiplier = 0.25f;

    [Min(0.01f)]
    public float minScale = 0.5f;

    [Min(0.01f)]
    public float maxScale = 2.5f;

    private void Reset()
    {
        baseScale = transform.localScale;
        TryFindCamera();
        FaceTarget(true);
    }

    private void Awake()
    {
        if (baseScale == Vector3.zero)
            baseScale = transform.localScale;

        TryFindCamera();
    }

    private void OnEnable()
    {
        TryFindCamera();
        FaceTarget(true);
    }

    private void LateUpdate()
    {
        FaceTarget(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (baseScale == Vector3.zero)
            baseScale = transform.localScale;

        if (!Application.isPlaying)
            FaceTarget(true);
    }
#endif

    [ContextMenu("Find Camera")]
    public void TryFindCamera()
    {
        if (targetCamera != null)
            return;

        if (autoFindMainCamera && Camera.main != null)
            targetCamera = Camera.main.transform;
    }

    [ContextMenu("Face Camera Now")]
    public void FaceCameraNow()
    {
        FaceTarget(true);
    }

    private void FaceTarget(bool instant)
    {
        Transform cam = GetCameraTransform();

        if (cam == null)
            return;

        if (!lookAtCamera)
            return;

        Vector3 directionToCamera = cam.position - transform.position;

        if (onlyRotateAroundY)
            directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(directionToCamera.normalized, Vector3.up);

        if (autoFlipForWorldSpaceUI)
            targetRotation *= Quaternion.Euler(0f, 180f, 0f);

        targetRotation *= Quaternion.Euler(extraRotationOffset);

        if (smoothRotation && Application.isPlaying && !instant)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                1f - Mathf.Exp(-rotationSpeed * Time.deltaTime)
            );
        }
        else
        {
            transform.rotation = targetRotation;
        }

        if (scaleWithDistance)
        {
            float distance = Vector3.Distance(transform.position, cam.position);
            float scaleAmount = Mathf.Clamp(distance * distanceScaleMultiplier, minScale, maxScale);
            transform.localScale = baseScale * scaleAmount;
        }
    }

    private Transform GetCameraTransform()
    {
        if (targetCamera != null)
            return targetCamera;

        if (autoFindMainCamera && Camera.main != null)
            return Camera.main.transform;

#if UNITY_EDITOR
        if (!Application.isPlaying && useSceneViewCameraInEditMode)
        {
            if (SceneView.lastActiveSceneView != null &&
                SceneView.lastActiveSceneView.camera != null)
            {
                return SceneView.lastActiveSceneView.camera.transform;
            }
        }
#endif

        return null;
    }
}