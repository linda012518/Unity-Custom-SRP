
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

		// 引入最高亮度减去最低亮度系数，小于这个系数丢弃
		//   0.0833 - upper limit (default, the start of visible unfiltered edges)
		//   0.0625 - high quality (faster)
		//   0.0312 - visible limit (slower)
		[UnityEngine.Range(0.0312f, 0.0833f)]
		public float fixedThreshold;
	}

	public FXAA fxaa;
}