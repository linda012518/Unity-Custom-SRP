
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

		// 亮度小于这个系数丢弃
		//   0.333 - too little (faster)
		//   0.250 - low quality
		//   0.166 - default
		//   0.125 - high quality 
		//   0.063 - overkill (slower)
		[UnityEngine.Range(0.063f, 0.333f)]
		public float relativeThreshold;

		// 最终混合因子，选择混合强度，0相当于没开
		// This can effect sharpness.
		//   1.00 - upper limit (softer)
		//   0.75 - default amount of filtering
		//   0.50 - lower limit (sharper, less sub-pixel aliasing removal)
		//   0.25 - almost off
		//   0.00 - completely off
		[UnityEngine.Range(0f, 1f)]
		public float subpixelBlending;
	}

	public FXAA fxaa;
}