
[System.Serializable]
public struct CameraBufferSettings
{

	public bool allowHDR;

	public bool copyColor, copyColorReflection, copyDepth, copyDepthReflections;

	[UnityEngine.Range(0.1f, 2f)]
	public float renderScale;

	public enum BicubicRescalingMode { Off, UpOnly, UpAndDown }

	public BicubicRescalingMode bicubicRescaling;

	[System.Serializable]
	public struct FXAA
	{

		public bool enabled;
	}

	public FXAA fxaa;
}