using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Linda Post FX Settings")]
public class PostFXSettings : ScriptableObject
{
    [SerializeField]
    Shader shader = default;

	[System.NonSerialized]
	Material material;

	[System.Serializable]
	public struct BloomSettings
	{

		[Range(0f, 16f)]
		public int maxIterations;

		[Min(1f)]
		public int downscaleLimit;

		public bool bicubicUpsampling;//三线性采样，减少灰度图出来块状模糊

		[Min(0f)]
		public float threshold;

		[Range(0f, 1f)]
		public float thresholdKnee;

		[Min(0f)]
		public float intensity;//全局强度

		public bool fadeFireflies;//消除闪烁

		public enum Mode { Additive, Scattering }

		public Mode mode;

		[Range(0.05f, 0.95f)]
		public float scatter;//散射程度
	}

	[SerializeField]
	BloomSettings bloom = new BloomSettings { scatter = 0.7f };

	public BloomSettings Bloom => bloom;


	[Serializable]
	public struct ColorAdjustmentsSettings 
	{
		public float postExposure;//曝光

		[Range(-100f, 100f)]
		public float contrast;//对比度

		[ColorUsage(false, true)]
		public Color colorFilter;//颜色滤镜

		[Range(-180f, 180f)]
		public float hueShift;//色相偏移

		[Range(-100f, 100f)]
		public float saturation;//饱和度
	}

	[SerializeField]
	ColorAdjustmentsSettings colorAdjustments = new ColorAdjustmentsSettings { colorFilter = Color.white };

	public ColorAdjustmentsSettings ColorAdjustments => colorAdjustments;


	[Serializable]
	public struct WhiteBalanceSettings//白平衡
	{

		[Range(-100f, 100f)]
		public float temperature;//温度，用于使图像更冷或更暖 
		[Range(-100f, 100f)]
		public float tint;//调整温度变化的颜色
	}

	[SerializeField]
	WhiteBalanceSettings whiteBalance = default;

	public WhiteBalanceSettings WhiteBalance => whiteBalance;

	[Serializable]
	public struct SplitToningSettings//色调分离
	{

		[ColorUsage(false)]
		public Color shadows, highlights;//阴影色和高光色

		[Range(-100f, 100f)]
		public float balance;//平衡度
	}

	[SerializeField]
	SplitToningSettings splitToning = new SplitToningSettings
	{
		shadows = Color.gray,
		highlights = Color.gray
	};

	public SplitToningSettings SplitToning => splitToning;

	[Serializable]
	public struct ChannelMixerSettings
	{
		//颜色混合
		public Vector3 red, green, blue;
	}

	[SerializeField]
	ChannelMixerSettings channelMixer = new ChannelMixerSettings
	{
		red = Vector3.right,
		green = Vector3.up,
		blue = Vector3.forward
	};

	public ChannelMixerSettings ChannelMixer => channelMixer;

	[Serializable]
	public struct ShadowsMidtonesHighlightsSettings
	{

		[ColorUsage(false, true)]
		public Color shadows, midtones, highlights;

		[Range(0f, 2f)]
		public float shadowsStart, shadowsEnd, highlightsStart, highLightsEnd;
	}

	[SerializeField]
	ShadowsMidtonesHighlightsSettings shadowsMidtonesHighlights = new ShadowsMidtonesHighlightsSettings
	{
		shadows = Color.white,
		midtones = Color.white,
		highlights = Color.white,
		shadowsEnd = 0.3f,
		highlightsStart = 0.55f,
		highLightsEnd = 1f
	};

	public ShadowsMidtonesHighlightsSettings ShadowsMidtonesHighlights => shadowsMidtonesHighlights;





	[System.Serializable]
	public struct ToneMappingSettings
	{
		public enum Mode { None , ACES, Neutral, Reinhard }

		public Mode mode;
	}

	[SerializeField]
	ToneMappingSettings toneMapping = default;

	public ToneMappingSettings ToneMapping => toneMapping;


	public Material Material
	{
		get
		{
			if (material == null && shader != null)
			{
				material = new Material(shader);
				material.hideFlags = HideFlags.HideAndDontSave;
			}
			return material;
		}
	}
}
