using UnityEngine;
using UnityEngine.InputSystem;

public struct CameraInput
{
	public Vector2 LookInput;
	public InputDevice LookDevice;
}

public class PlayerCamera : MonoBehaviour
{
	PlayerCameraSettings m_CameraSettings;
	Vector3 m_EulerAngles;

	public void Init(PlayerCameraSettings cameraSettings, Transform target)
	{
		m_CameraSettings = cameraSettings;
		ChangeCameraTarget(target);
	}

	public void ChangeCameraTarget(Transform target)
	{
		transform.position = target.position;
		transform.eulerAngles = target.eulerAngles;
		m_EulerAngles = target.eulerAngles;
	}

	public void UpdateRotation(ref CameraInput input, float deltaTime)
	{
		float lY = input.LookInput.y;
		float lX = input.LookInput.x;
		float vSens = input.LookDevice is Mouse ? m_CameraSettings.MouseVerticalSensitivity : (m_CameraSettings.ControllerVerticalSensitivity * deltaTime);
		float hSens = input.LookDevice is Mouse ? m_CameraSettings.MouseHorizontalSensitivity : (m_CameraSettings.ControllerHorizontalSensitivity * deltaTime);
		float pitch = lY * vSens;
		float yaw = lX * hSens;
		m_EulerAngles.x = Mathf.Clamp(m_EulerAngles.x - pitch, m_CameraSettings.MinVerticalAngle, m_CameraSettings.MaxVerticalAngle);
		m_EulerAngles.y += yaw;
		transform.eulerAngles = m_EulerAngles;
	}

	public void SetRotation(Vector3 eulerAngles)
	{
		m_EulerAngles = eulerAngles;
		transform.eulerAngles = eulerAngles;
	}

	public void UpdatePosition(Transform target) => transform.position = target.position;
}