using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    const string bufferName = "Shadows";

    CommandBuffer buffer = new CommandBuffer() { name = bufferName };

    ScriptableRenderContext context;

    CullingResults cullingResults;

    ShadowSettings settings;

    const int maxShadowedDirectionalLightCount = 4, maxShadowedOtherLightCount = 16, maxCascades = 4;

    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;//这里的索引是所有灯光的索引，在CPU端的
        public float slopeScaleBias;
        public float nearPlaneOffset;
    }

    ShadowedDirectionalLight[] shadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    struct ShadowedOtherLight
    {
        public int visibleLightIndex;//这里的索引是所有灯光的索引，在CPU端的
        public float slopeScaleBias;
        public float normalBias;
        public bool isPoint;
    }

    ShadowedOtherLight[] shadowedOtherLights = new ShadowedOtherLight[maxShadowedOtherLightCount];

    int shadowedDirectionalLightCount, shadowedOtherLightCount;

    static int
        dirShadowAltasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
        dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
        otherShadowAtlasId = Shader.PropertyToID("_OtherShadowAtlas"),
        otherShadowMatricesId = Shader.PropertyToID("_OtherShadowMatrices"),
        otherShadowTilesId = Shader.PropertyToID("_OtherShadowTiles"),
        cascadeCountId = Shader.PropertyToID("_CascadeCount"),
        cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingShperes"),
        cascadeDataId = Shader.PropertyToID("_CascadeData"),
        shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize"),
        shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade"),
        shadowPancakingId = Shader.PropertyToID("_ShadowPancaking");//处理穿过裁剪面的物体阴影，只有平行光要，其它光不需要

    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
    static Matrix4x4[] otherShadowMatrices = new Matrix4x4[maxShadowedOtherLightCount];
    static Vector4[]
        cascadeCullingShperes = new Vector4[maxCascades],
        cascadeData = new Vector4[maxCascades],
        otherShadowTiles = new Vector4[maxShadowedOtherLightCount];

    static string[] directionalFilterKeywords = {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };

    static string[] otherFilterKeywords = {
        "_OTHER_PCF3",
        "_OTHER_PCF5",
        "_OTHER_PCF7",
    };

    static string[] cascadeBlendKeywords = {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER"
    };

    static string[] shadowMaskKeywords = {
        "_SHADOW_MASK_ALWAYS",  //适配ShadowMask阴影模式，静态物体没有实时阴影
        "_SHADOW_MASK_DISTANCE" //适配Distance ShadowMask阴影模式，动静物体都有实时阴影，通过距离判断静态物体用实时或烘焙
    };

    bool useShadowMask;

    Vector4 atlasSizes;//xy平行光阴影大小和倒数，zw其它光阴影大小和倒数

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;

        shadowedDirectionalLightCount = 0;
        shadowedOtherLightCount = 0;
        useShadowMask = false;
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (shadowedDirectionalLightCount < maxShadowedDirectionalLightCount && 
            light.shadows != LightShadows.None && light.shadowStrength > 0)
        {
            float maskChannel = -1;

            LightBakingOutput lightBaking = light.bakingOutput;
            if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                useShadowMask = true;
                maskChannel = lightBaking.occlusionMaskChannel;
            }

            //检测光是否照看不见的区域，并返回区别，超出距离也会认为没有实时光照阴影用烘焙的，放到后边执行，先执行 useShadowMask
            if (!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
            {
                //用负值强度，确保使用烘焙的阴影
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
            }

            shadowedDirectionalLights[shadowedDirectionalLightCount] = 
                new ShadowedDirectionalLight() { visibleLightIndex = visibleLightIndex, slopeScaleBias = light.shadowBias, nearPlaneOffset = light.shadowNearPlane };
            return new Vector4(light.shadowStrength, settings.directional.cascadeCount * shadowedDirectionalLightCount++, light.shadowNormalBias, maskChannel);
        }
        return new Vector4(0f, 0f, 0f, -1f);
    }

    public Vector4 ReserveOtherShadows(Light light, int visibleLightIndex)
    {
        if (light.shadows == LightShadows.None || light.shadowStrength <= 0)
            return new Vector4(0f, 0f, 0f, -1f);

        float maskChannel = -1;
        LightBakingOutput lightBaking = light.bakingOutput;
        if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
        {
            useShadowMask = true;
            maskChannel = lightBaking.occlusionMaskChannel;
        }

        bool isPoint = light.type == LightType.Point;
        int newLightCount = shadowedOtherLightCount + (isPoint ? 6 : 1);

        if (newLightCount >= maxShadowedOtherLightCount || !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            //用负值强度，确保使用烘焙的阴影
            return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
        }

        shadowedOtherLights[shadowedOtherLightCount] = new ShadowedOtherLight
        {
            visibleLightIndex = visibleLightIndex, //这里的索引是所有灯光的索引，在CPU端的
            slopeScaleBias = light.shadowBias,
            normalBias = light.shadowNormalBias,
            isPoint = isPoint
        };

        //这里的count索引是GPU端的
        Vector4 data = new Vector4(light.shadowStrength, shadowedOtherLightCount++, isPoint ? 1 : 0, maskChannel);
        shadowedOtherLightCount = newLightCount;
        return data;
    }


    public void Render()
    {
        if (shadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            //不声明纹理会在 WebGL 2.0 报错，设置1x1纹理容错
            buffer.GetTemporaryRT(dirShadowAltasId, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }

        if (shadowedOtherLightCount > 0)
        {
            RenderOtherShadows();
        }
        else
        {
            //不声明纹理会在 WebGL 2.0 报错，随便给一个，也可以调置1x1纹理容错
            buffer.SetGlobalTexture(otherShadowAtlasId, dirShadowAltasId);
        }

        buffer.BeginSample(bufferName);
        SetKeywords(shadowMaskKeywords, useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);

        //点光和聚光灯有固定位置，极端情况会出现超出平行光距离，但还是有其它光阴影，把淡入淡出提出来共用，超出距离也没有其它光阴影
        float f = 1 - settings.directional.cascadeFade;
        buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(1.0f / settings.maxDistance, 1.0f / settings.distanceFade, 1.0f / (1 - f * f)));
        buffer.SetGlobalInt(cascadeCountId, settings.directional.cascadeCount);

        buffer.SetGlobalVector(shadowAtlasSizeId, atlasSizes);

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    void RenderDirectionalShadows()
    {
        int altasSize = (int)settings.directional.atlasSize;

        atlasSizes.x = altasSize;
        atlasSizes.y = 1f / altasSize;

        buffer.GetTemporaryRT(dirShadowAltasId, altasSize, altasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(dirShadowAltasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.SetGlobalFloat(shadowPancakingId, 1f);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        int tiles = shadowedDirectionalLightCount * settings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = altasSize / split;

        for (int i = 0; i < shadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }

        buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingShperes);
        buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        SetKeywords(directionalFilterKeywords, (int)settings.directional.filter - 1);
        SetKeywords(cascadeBlendKeywords, (int)settings.directional.cascadeBlend - 1);
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    void RenderOtherShadows()
    {
        int altasSize = (int)settings.other.atlasSize;

        atlasSizes.z = altasSize;
        atlasSizes.w = 1f / altasSize;

        buffer.GetTemporaryRT(otherShadowAtlasId, altasSize, altasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(otherShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.SetGlobalFloat(shadowPancakingId, 0f);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        int tiles = shadowedOtherLightCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = altasSize / split;

        for (int i = 0; i < shadowedOtherLightCount;)
        {
            if (shadowedOtherLights[i].isPoint)
            {
                RenderPointShadows(i, split, tileSize);
                i += 6;
            }
            else
            {
                RenderSpotShadows(i, split, tileSize);
                i += 1;
            }
            
        }

        buffer.SetGlobalMatrixArray(otherShadowMatricesId, otherShadowMatrices);
        buffer.SetGlobalVectorArray(otherShadowTilesId, otherShadowTiles);
        SetKeywords(otherFilterKeywords, (int)settings.other.filter - 1);
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    void SetKeywords(string[] keywords, int enabledIndex)
    {
        for (int i = 0; i < keywords.Length; i++)
        {
            if (enabledIndex == i)
            {
                buffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }

    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        ShadowDrawingSettings shadowSetting = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex)
        {
            useRenderingLayerMaskTest = true
        };

        int cascadeCount = settings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = settings.directional.CascadeRatios;

        float cullingFactor = Mathf.Max(0f, 0.8f - settings.directional.cascadeFade);

        float tileScale = 1f / split;
        for (int i = 0; i < cascadeCount; i++)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex, i, cascadeCount, ratios, tileSize, light.nearPlaneOffset,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
            splitData.shadowCascadeBlendCullingFactor = cullingFactor;//每个级联看的区域不一样，设置剔除看不到的物体范转
            shadowSetting.splitData = splitData;
            if (index == 0) //每个灯光用同样的剔除球，只存一次即可
            {
                SetCascadeData(i, splitData.cullingSphere, tileSize);
            }
            int tileIndex = tileOffset + i;
            //VP矩阵固定，设置视口可以选图片局部绘制
            Vector2 offset = SetTileViewport(tileIndex, split, tileSize);
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
            ExecuteBuffer();
            context.DrawShadows(ref shadowSetting);
            buffer.SetGlobalDepthBias(0f, 0f);
        }

    }

    void RenderSpotShadows(int index, int split, int tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];
        ShadowDrawingSettings shadowSetting = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex)
        {
            useRenderingLayerMaskTest = true
        };

        cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

        shadowSetting.splitData = splitData;

        //计算一个象素包含了多大场景：场景总大小 / 总象素
        //透视投影随距离增加，单象素包含场景线性增大，距离1的时候场景大小是2倍正切值θ
        //投影矩阵的第一个元素是：1 / aspect * tan(fov / 2) 注后面 fov / 2 = θ
        //聚光灯宽高一样，aspect = 1，所以矩阵第一个元素：1 / tanθ
        //距离1的时候比率：2 * tanθ / tileSize，换算后得下公式
        float texelSize = 2f / (tileSize * projectionMatrix.m00);
        float filterSize = texelSize * ((float)settings.other.filter + 1f);
        float bias = light.normalBias * filterSize * 1.4142136f;
        Vector2 offset = SetTileViewport(index, split, tileSize);
        float tileScale = 1f / split;
        SetOtherTileData(index, offset, tileScale, bias);

        otherShadowMatrices[index] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);

        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
        ExecuteBuffer();
        context.DrawShadows(ref shadowSetting);
        buffer.SetGlobalDepthBias(0f, 0f);
    }

    void RenderPointShadows(int index, int split, int tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];
        ShadowDrawingSettings shadowSetting = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex)
        {
            useRenderingLayerMaskTest = true
        };

        //计算一个象素包含了多大场景：场景总大小 / 总象素
        //透视投影随距离增加，单象素包含场景线性增大，距离1的时候场景大小是2倍正切值θ
        //投影矩阵的第一个元素是：1 / aspect * tan(fov / 2) 注后面 fov / 2 = θ
        //聚光灯宽高一样，aspect = 1，所以矩阵第一个元素：1 / tanθ
        //距离1的时候比率：2 * tanθ / tileSize，换算后得下公式
        //点光源广角为90度，tanθ = 1，可以不用放循环
        float texelSize = 2f / tileSize;
        float filterSize = texelSize * ((float)settings.other.filter + 1f);
        float bias = light.normalBias * filterSize * 1.4142136f;
        float tileScale = 1f / split;

        //立方体阴影会采样的图集边缘导致近处无阴影，稍微增加FOV避免采样到边缘
        float fovBias = Mathf.Atan(1f + bias + filterSize) * Mathf.Rad2Deg * 2f - 90f;

        for (int i = 0; i < 6; i++)
        {
            cullingResults.ComputePointShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex, (CubemapFace)i, fovBias, 
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

            //unity渲染点光阴影会把三角形顺序颠倒，导致一些露光，这一行第一个分量总是0忽略，待查看原理
            viewMatrix.m11 = -viewMatrix.m11;
            viewMatrix.m12 = -viewMatrix.m12;
            viewMatrix.m13 = -viewMatrix.m13;

            shadowSetting.splitData = splitData;
            int tileIndex = index + i;


            Vector2 offset = SetTileViewport(tileIndex, split, tileSize);
            
            SetOtherTileData(tileIndex, offset, tileScale, bias);

            otherShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);

            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
            ExecuteBuffer();
            context.DrawShadows(ref shadowSetting);
            buffer.SetGlobalDepthBias(0f, 0f);
        }
    }


    void SetOtherTileData(int index, Vector2 offset, float scale, float bias)
    {
        //把边界缩小半个象素，保证不会采样到外界
        float border = atlasSizes.w * 0.5f;
        Vector4 data = Vector4.zero;
        data.x = offset.x * scale + border;
        data.y = offset.y * scale + border;
        data.z = scale - border - border;
        data.w = bias;
        otherShadowTiles[index] = data;
    }

    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
    {
        //剔除球的直径除以纹理大小=纹素大小，直径是场景一共有多大
        float texelSize = 2 * cullingSphere.w / tileSize;
        //PCF会导致阴影摩尔纹，因为采样的阴影范围扩大了，会在周围多采样几圈，所在纹素大小应该是PCF范围加1，来增大偏差
        float filterSize = texelSize * ((float)settings.directional.filter + 1f);
        cullingSphere.w -= filterSize;//PCF增加采样范围会超出级联区域，把级联球半径变小保证不会超出球半径，不会超出采样范围
        cullingSphere.w *= cullingSphere.w;//预乘半径传入shader
        cascadeCullingShperes[index] = cullingSphere;//从小球到大球传入
        cascadeData[index].x = 1f / cullingSphere.w;
        cascadeData[index].y = filterSize * 1.4142136f;//乘根号2，在最坏情况下在偏移正方形对角线长度
    }

    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, float scale)
    {
        //是否要反转Z轴
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }

        //float scale = 1f / split;
        //https://zhuanlan.zhihu.com/p/83499311
        //https://blog.csdn.net/qq_38275140/article/details/87459130?spm=1001.2014.3001.5502
        //把光空间片段位置转换为裁切空间的标准化设备坐标。
        //当我们在顶点着色器输出一个裁切空间顶点位置到gl_Position时，OpenGL自动进行一个透视除法，将裁切空间坐标的范围-w到w转为-1到1
        //NDC空间是-1~1所以要缩小一半，但要和采样出来的深度图比较，转换成0~1
        //相当于在shader里少了x = x * 0.5 + 0.5;的操作，shader里再除w  //平行光不需要除w进行透视较正
        //Matrix4x4 scaleOffset = Matrix4x4.TRS(Vector3.one * 0.5f, Quaternion.identity, Vector3.one * 0.5f);
        //Matrix4x4 scaleOffset = Matrix4x4.identity;
        //scaleOffset.m00 = scaleOffset.m11 = scaleOffset.m22 = 0.5f;
        //scaleOffset.m03 = scaleOffset.m13 = scaleOffset.m23 = 0.5f;
        //平移和缩放到每一格采样，有适当xy偏移的转换矩阵
        //Matrix4x4 tileMatrix = Matrix4x4.identity;
        //tileMatrix.m00 = tileMatrix.m11 = scale;
        //tileMatrix.m03 = offset.x * scale;
        //tileMatrix.m13 = offset.y * scale;

        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);

        return m;
    }

    Vector2 SetTileViewport(int index, int split, int tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAltasId);
        if (shadowedOtherLightCount > 0)
        {
            buffer.ReleaseTemporaryRT(otherShadowAtlasId);
        }
        ExecuteBuffer();
    }

}
