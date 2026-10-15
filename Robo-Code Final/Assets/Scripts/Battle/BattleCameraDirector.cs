using System.Collections;
using System.Reflection;
using UnityEngine;

// Zooms the camera you already have onto the fighters, then back out.
// Works with Cinemachine 3 (CinemachineCamera), Cinemachine 2 (CinemachineVirtualCamera),
// or a plain orthographic camera. Uses reflection so it compiles whatever is installed.
public class BattleCameraDirector : MonoBehaviour
{
    [Tooltip("Battle zoom as a fraction of the normal zoom. 0.6 = 40% closer.")]
    public float zoomFactor = 0.67f;
    public float zoomTime = 0.6f;

    private Component vcam;
    private FieldInfo lensField;
    private FieldInfo orthoField;
    private Transform originalTarget;
    private float originalSize;
    private Camera plainCamera;
    private Vector3 plainCameraPosition;
    private Transform focus;
    private Coroutine zoomRoutine;

    public void Enter(Transform focusPoint)
    {
        focus = focusPoint;
        FindCamera();

        if (vcam != null)
        {
            originalSize = GetSize();
            originalTarget = GetTarget();
            SetTarget(focus);
        }
        else if (plainCamera != null)
        {
            originalSize = plainCamera.orthographicSize;
            plainCameraPosition = plainCamera.transform.position;
        }
        else
        {
            return;
        }

        Zoom(originalSize * zoomFactor, true);
    }

    public void Exit()
    {
        if (vcam != null)
        {
            SetTarget(originalTarget);
            Zoom(originalSize, false);
        }
        else if (plainCamera != null)
        {
            Zoom(originalSize, false);
        }
    }

    private void Zoom(float to, bool entering)
    {
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
        zoomRoutine = StartCoroutine(ZoomRoutine(to, entering));
    }

    private IEnumerator ZoomRoutine(float to, bool entering)
    {
        float from = vcam != null ? GetSize() : plainCamera.orthographicSize;
        Vector3 camFrom = plainCamera != null ? plainCamera.transform.position : Vector3.zero;
        Vector3 camTo = plainCamera != null
            ? (entering && focus != null ? new Vector3(focus.position.x, focus.position.y, camFrom.z) : plainCameraPosition)
            : Vector3.zero;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, zoomTime);
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            float size = Mathf.Lerp(from, to, e);

            if (vcam != null) SetSize(size);
            else
            {
                plainCamera.orthographicSize = size;
                plainCamera.transform.position = Vector3.Lerp(camFrom, camTo, e);
            }
            yield return null;
        }
        zoomRoutine = null;
    }

    // ---------------------------------------------------------------- camera access

    private void FindCamera()
    {
        vcam = null;
        plainCamera = null;

        foreach (MonoBehaviour m in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            string n = m.GetType().Name;
            if ((n == "CinemachineCamera" || n == "CinemachineVirtualCamera") && m.isActiveAndEnabled)
            {
                vcam = m;
                break;
            }
        }

        if (vcam != null)
        {
            System.Type t = vcam.GetType();
            lensField = t.GetField("Lens") ?? t.GetField("m_Lens");
            orthoField = lensField != null ? lensField.FieldType.GetField("OrthographicSize") : null;
            if (orthoField == null)
            {
                Debug.LogWarning("[Battle] Found a Cinemachine camera but couldn't read its lens. No battle zoom.");
                vcam = null;
            }
            return;
        }

        plainCamera = Camera.main;
        if (plainCamera != null && !plainCamera.orthographic) plainCamera = null;
    }

    private float GetSize()
    {
        object lens = lensField.GetValue(vcam);
        return (float)orthoField.GetValue(lens);
    }

    private void SetSize(float size)
    {
        object lens = lensField.GetValue(vcam); // boxed struct
        orthoField.SetValue(lens, size);
        lensField.SetValue(vcam, lens);
    }

    private Transform GetTarget()
    {
        System.Type t = vcam.GetType();
        FieldInfo targetField = t.GetField("Target");               // Cinemachine 3
        if (targetField != null)
        {
            FieldInfo tracking = targetField.FieldType.GetField("TrackingTarget");
            if (tracking != null) return tracking.GetValue(targetField.GetValue(vcam)) as Transform;
        }
        PropertyInfo follow = t.GetProperty("Follow");               // Cinemachine 2
        return follow != null ? follow.GetValue(vcam) as Transform : null;
    }

    private void SetTarget(Transform target)
    {
        System.Type t = vcam.GetType();
        FieldInfo targetField = t.GetField("Target");
        if (targetField != null)
        {
            FieldInfo tracking = targetField.FieldType.GetField("TrackingTarget");
            if (tracking != null)
            {
                object boxed = targetField.GetValue(vcam);
                tracking.SetValue(boxed, target);
                targetField.SetValue(vcam, boxed);
                return;
            }
        }
        PropertyInfo follow = t.GetProperty("Follow");
        if (follow != null && follow.CanWrite) follow.SetValue(vcam, target);
    }
}
