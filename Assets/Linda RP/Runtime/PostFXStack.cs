using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static PostFXSettings;

public partial class PostFXStack
{
    enum Pass
    {
        BloomScatterFinal,
        BloomScatter,
        BloomPrefilterFireflies,
        BloomPrefilter,
        BloomAdd,
        BloomVertical,
        BloomHorizontal,
        Copy,
        ColorGradingNone,
        ToneMappingACES,
        ToneMappingNeutral,
        ToneMappingReinhard,
        ApplyColorGrading,
        ApplyColorGradingWithLuma,
        FinalRescale,
        FXAA,
        FXAAWithLuma
    }

    const string bufferName = "Post FX";

    CommandBuffer buffer = new CommandBuffer() { name = bufferName };

    Camera camera;

    ScriptableRenderContext context;

    PostFXSettings setting;

    int
        bloomBucibicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling"),
        bloomIntensityId = Shader.PropertyToID("_BloomIntensity"),
        bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter"),
        bloomThresholdId = Shader.PropertyToID("_BloomThreshold"),
        bloomResultId = Shader.PropertyToID("_BloomResult"),
        fxSourceId = Shader.PropertyToID("_PostFXSource"),
        fxSource2Id = Shader.PropertyToID("_PostFXSource2");

    int
        colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT"),
        colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters"),
        colorGradingLUTInLogId = Shader.PropertyToID("_ColorGradingLUTInLogC"),
        colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments"),
        colorFilterId = Shader.PropertyToID("_ColorFilter"),
        whiteBalanceId = Shader.PropertyToID("_WhiteBalance"),
        splitToningShadowsId = Shader.PropertyToID("_SplitToningShadows"),
        splitToningHighlightsId = Shader.PropertyToID("_SplitToningHighlights"),
        channelMixerRedId = Shader.PropertyToID("_ChannelMixerRed"),
        channelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen"),
        channelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue"),
        smhShadowsId = Shader.PropertyToID("_SMHShadows"),
        smhMidtonesId = Shader.PropertyToID("_SMHMidtones"),
        smhHighlightsId = Shader.PropertyToID("_SMHHighlights"),
        smhRangeId = Shader.PropertyToID("_SMHRange");

    int
        copyBicubicId = Shader.PropertyToID("_CopyBicubic"),
        colorGradingResultId = Shader.PropertyToID("_ColorGradingResult"),
        finalResultId = Shader.PropertyToID("_FinalResult"),
        finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend"),
        finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");

    int fxaaConfigId = Shader.PropertyToID("_FXAAConfig");

    CameraBufferSettings.BicubicRescalingMode bicubicRescaling;

    const int maxBloomPyramidLevels = 16;

    int bloomPyramidId;

    bool keepAlpha, useHDR;

    int colorLUTResolution;

    static Rect fullViewRect = new Rect(0f, 0f, 1f, 1f);

    public bool IsActive => setting != null;

    CameraSettings.FinalBlendMode finalBlendMode;

    Vector2Int bufferSize;

    CameraBufferSettings.FXAA fxaa;

    const string
        fxaaQualityLowKeyword = "FXAA_QUALITY_LOW",
        fxaaQualityMediumKeyword = "FXAA_QUALITY_MEDIUM";

    public PostFXStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 0; i < maxBloomPyramidLevels * 2; i++)
        {
            //这里不用存了，和bloomPyramidId一定是连续的
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    public void Setup(ScriptableRenderContext context, Camera camera, Vector2Int bufferSize, PostFXSettings setting, bool keepAlpha, bool useHDR, int colorLUTResolution, CameraSettings.FinalBlendMode finalBlendMode, CameraBufferSettings.BicubicRescalingMode bicubicRescaling, CameraBufferSettings.FXAA fxaa)
    {
        this.keepAlpha = keepAlpha;
        this.fxaa = fxaa;
        this.bicubicRescaling = bicubicRescaling;
        this.bufferSize = bufferSize;
        this.finalBlendMode = finalBlendMode;
        this.colorLUTResolution = colorLUTResolution;
        this.useHDR = useHDR;
        this.context = context;
        this.camera = camera;
        //场景里没有相机不用后处理
        this.setting = camera.cameraType <= CameraType.SceneView ? setting : null;
        //使编辑窗口后处理按钮生效，可以控制是否显示后处理
        ApplySceneViewState();
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, setting.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    void DrawFinal(RenderTargetIdentifier from, Pass pass)
    {
        buffer.SetGlobalFloat(finalSrcBlendId, (float)finalBlendMode.source);
        buffer.SetGlobalFloat(finalDstBlendId, (float)finalBlendMode.destination);

        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,
            //tile-based GPU基于图块的GPU会有冗余数据，导致边缘黑边 RenderBufferLoadAction.Load 可以解决
            finalBlendMode.destination == BlendMode.Zero && camera.rect == fullViewRect ?
                RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        buffer.SetViewport(camera.pixelRect);
        buffer.DrawProcedural(Matrix4x4.identity, setting.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    public void Render(int sourceId)
    {
        if (DoBloom(sourceId))
        {
            DoColorGradingAndToneMappingDoFinal(bloomResultId);
            buffer.ReleaseTemporaryRT(bloomResultId);
        }
        else
        {
            DoColorGradingAndToneMappingDoFinal(sourceId);
        }
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void ConfigureColorAdjustments()
    {
        ColorAdjustmentsSettings colorAdjustments = setting.ColorAdjustments;
        buffer.SetGlobalVector(colorAdjustmentsId, new Vector4(
            Mathf.Pow(2f, colorAdjustments.postExposure), //曝光是2的幂
            colorAdjustments.contrast * 0.01f + 1f, //对比度0-2 范围
            colorAdjustments.hueShift * (1f / 360f),//色相偏移转换为 -1~1
            colorAdjustments.saturation * 0.01f + 1f//饱和度0-2 范围
        ));
        buffer.SetGlobalColor(colorFilterId, colorAdjustments.colorFilter.linear);
    }

    void ConfigureWhiteBalance()
    {
        WhiteBalanceSettings whiteBalance = setting.WhiteBalance;
        //LMS 色彩空间
        buffer.SetGlobalVector(whiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(whiteBalance.temperature, whiteBalance.tint));
    }

    void ConfigureSplitToning()
    {
        SplitToningSettings splitToning = setting.SplitToning;
        Color splitColor = splitToning.shadows;
        splitColor.a = splitToning.balance * 0.01f;//缩放到-1~1
        buffer.SetGlobalColor(splitToningShadowsId, splitColor);
        buffer.SetGlobalColor(splitToningHighlightsId, splitToning.highlights);
    }

    void ConfigureChannelMixer()
    {
        ChannelMixerSettings channelMixer = setting.ChannelMixer;
        buffer.SetGlobalVector(channelMixerRedId, channelMixer.red);
        buffer.SetGlobalVector(channelMixerGreenId, channelMixer.green);
        buffer.SetGlobalVector(channelMixerBlueId, channelMixer.blue);
    }

    void ConfigureShadowsMidtonesHighlights()
    {
        ShadowsMidtonesHighlightsSettings smh = setting.ShadowsMidtonesHighlights;
        buffer.SetGlobalColor(smhShadowsId, smh.shadows.linear);
        buffer.SetGlobalColor(smhMidtonesId, smh.midtones.linear);
        buffer.SetGlobalColor(smhHighlightsId, smh.highlights.linear);
        buffer.SetGlobalVector(smhRangeId, new Vector4(smh.shadowsStart, smh.shadowsEnd, smh.highlightsStart, smh.highLightsEnd));
    }

    void ConfigureFXAA()
    {
        if (fxaa.quality == CameraBufferSettings.FXAA.Quality.Low)
        {
            buffer.EnableShaderKeyword(fxaaQualityLowKeyword);
            buffer.DisableShaderKeyword(fxaaQualityMediumKeyword);
        }
        else if (fxaa.quality == CameraBufferSettings.FXAA.Quality.Medium)
        {
            buffer.DisableShaderKeyword(fxaaQualityLowKeyword);
            buffer.EnableShaderKeyword(fxaaQualityMediumKeyword);
        }
        else
        {
            buffer.DisableShaderKeyword(fxaaQualityLowKeyword);
            buffer.DisableShaderKeyword(fxaaQualityMediumKeyword);
        }
        buffer.SetGlobalVector(fxaaConfigId, new Vector4(fxaa.fixedThreshold, fxaa.relativeThreshold, fxaa.subpixelBlending));
    }

    void DoColorGradingAndToneMappingDoFinal(int sourceId)
    {
        ConfigureColorAdjustments();
        ConfigureWhiteBalance();
        ConfigureSplitToning();
        ConfigureChannelMixer();
        ConfigureShadowsMidtonesHighlights();

        int lutHeight = colorLUTResolution;
        int lutWidth = lutHeight * lutHeight;
        buffer.GetTemporaryRT(colorGradingLUTId, lutWidth, lutHeight, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(
            lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f)
        ));

        ToneMappingSettings.Mode mode = setting.ToneMapping.mode;
        Pass pass = Pass.ColorGradingNone + (int)mode;
        buffer.SetGlobalFloat(colorGradingLUTInLogId, useHDR && pass != Pass.ColorGradingNone ? 1f : 0f);

        Draw(sourceId, colorGradingLUTId, pass);

        buffer.SetGlobalVector(colorGradingLUTParametersId,new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f));

        buffer.SetGlobalFloat(finalSrcBlendId, 1f);
        buffer.SetGlobalFloat(finalDstBlendId, 0f);

        if (fxaa.enabled)
        {
            ConfigureFXAA();

            buffer.GetTemporaryRT(colorGradingResultId, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            Draw(sourceId, colorGradingResultId, keepAlpha ? Pass.ApplyColorGrading : Pass.ApplyColorGradingWithLuma);
        }

        if (bufferSize.x == camera.pixelWidth)
        {
            if (fxaa.enabled)
            {
                DrawFinal(colorGradingResultId, keepAlpha ? Pass.FXAA : Pass.FXAAWithLuma);
                buffer.ReleaseTemporaryRT(colorGradingResultId);
            }
            else
            {
                DrawFinal(sourceId, Pass.ApplyColorGrading);
            }
        }
        else
        {
            buffer.GetTemporaryRT(finalResultId, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, RenderTextureFormat.Default);

            if (fxaa.enabled)
            {
                Draw(colorGradingResultId, finalResultId, keepAlpha ? Pass.FXAA : Pass.FXAAWithLuma);
                buffer.ReleaseTemporaryRT(colorGradingResultId);
            }
            else
            {
                Draw(sourceId, finalResultId, Pass.ApplyColorGrading);
            }
            
            bool bicubicSampling =
                bicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
                bicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpOnly &&
                bufferSize.x < camera.pixelWidth;
            buffer.SetGlobalFloat(copyBicubicId, bicubicSampling ? 1f : 0f);
            DrawFinal(finalResultId, Pass.FinalRescale);
            buffer.ReleaseTemporaryRT(finalResultId);
        }
        buffer.ReleaseTemporaryRT(colorGradingLUTId);
    }

    bool DoBloom(int sourceId)
    {
        PostFXSettings.BloomSettings bloom = setting.Bloom;

        int width, height;
        if (bloom.ignoreRenderScale)
        {
            width = camera.pixelWidth / 2;
            height = camera.pixelHeight / 2;
        }
        else
        {
            width = bufferSize.x / 2;
            height = bufferSize.y / 2;
        }

        if (bloom.maxIterations == 0 || bloom.intensity <= 0f || height < bloom.downscaleLimit * 2 || width < bloom.downscaleLimit * 2)
        {
            return false;
        }

        buffer.BeginSample("Bloom");

        Vector4 threshold;//x=t;y=-t+tk;z=2tk;w=1/4tk+0.00001
        threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
        threshold.y = threshold.x * bloom.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        buffer.SetGlobalVector(bloomThresholdId, threshold);

        RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        buffer.GetTemporaryRT(bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format);
        Draw(sourceId, bloomPrefilterId, bloom.fadeFireflies ? Pass.BloomPrefilterFireflies : Pass.BloomPrefilter);
        width /= 2;
        height /= 2;

        int fromId = bloomPrefilterId, toId = bloomPyramidId + 1;

        int i;
        for (i = 0; i < bloom.maxIterations; i++)
        {
            if (height < bloom.downscaleLimit || width < bloom.downscaleLimit)
                break;

            int midId = toId - 1;
            buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            buffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);
            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }

        buffer.ReleaseTemporaryRT(bloomPrefilterId);

        buffer.SetGlobalFloat(bloomBucibicUpsamplingId, bloom.bicubicUpsampling ? 1f : 0f);

        Pass combinePass, finalPass;
        float finalIntensity;
        if (bloom.mode == PostFXSettings.BloomSettings.Mode.Additive)
        {
            combinePass = finalPass = Pass.BloomAdd;
            buffer.SetGlobalFloat(bloomIntensityId, 1f);
            finalIntensity = bloom.intensity;
        }
        else
        {
            combinePass = Pass.BloomScatter;
            finalPass = Pass.BloomScatterFinal;
            buffer.SetGlobalFloat(bloomIntensityId, bloom.scatter);
            finalIntensity = Mathf.Min(bloom.intensity, 0.95f);
        }

        if (i > 1)
        {
            buffer.ReleaseTemporaryRT(fromId - 1);
            toId -= 5;

            for (i -= 1; i > 0; i--)
            {
                buffer.SetGlobalTexture(fxSource2Id, toId + 1);
                Draw(fromId, toId, combinePass);

                buffer.ReleaseTemporaryRT(fromId);
                buffer.ReleaseTemporaryRT(fromId + 1);
                fromId = toId;
                toId -= 2;
            }
        }
        else
        {
            buffer.ReleaseTemporaryRT(bloomPyramidId);
        }

        buffer.SetGlobalFloat(bloomIntensityId, finalIntensity);
        buffer.SetGlobalTexture(fxSource2Id, sourceId);
        buffer.GetTemporaryRT(bloomResultId, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, format);
        Draw(fromId, bloomResultId, finalPass);
        buffer.ReleaseTemporaryRT(fromId);

        buffer.EndSample("Bloom");

        return true;
    }
}
