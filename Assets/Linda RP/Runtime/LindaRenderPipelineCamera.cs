using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public class LindaRenderPipelineCamera : MonoBehaviour
{

	[SerializeField]
	CameraSettings settings = default;

	public CameraSettings Settings => settings ?? (settings = new CameraSettings());
}