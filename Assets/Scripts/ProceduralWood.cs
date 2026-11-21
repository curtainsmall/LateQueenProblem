using Unity.Collections;
using UnityEngine;

[ExecuteAlways]
public class ProceduralWood : MonoBehaviour
{
    [SerializeField, Tooltip("要套用木纹的渲染器。留空时作用于所有子渲染器。")]
    Renderer[] _targetRenderers;

    [SerializeField, Tooltip("程序生成的木纹贴图边长，单位像素。正方形，建议 512 到 2048。")]
    int _resolution = 1024;

    [SerializeField, Tooltip("木纹条带的密度。数值越大，纵向条纹越密。")]
    float _grainFrequency = 34f;

    [SerializeField, Tooltip("条纹的弯曲程度。0 为完全笔直，数值越大越扭曲。")]
    float _warpStrength = 2.2f;

    [SerializeField, Tooltip("木纹的亮部颜色。")]
    Color _lightColor = new Color(0.86f, 0.66f, 0.42f, 1f);

    [SerializeField, Tooltip("木纹的暗部颜色。")]
    Color _darkColor = new Color(0.42f, 0.25f, 0.14f, 1f);

    Texture2D _woodTexture;
    MaterialPropertyBlock _propertyBlock;

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        if (_resolution < 64) _resolution = 64;
        Rebuild();
    }

    [ContextMenu("Rebuild")]
    void Rebuild()
    {
        ClearPropertyBlocks();
        if (_targetRenderers == null || _targetRenderers.Length == 0) return;

        if (_woodTexture == null || _woodTexture.width != _resolution)
        {
            DestroyTexture();
            _woodTexture = new Texture2D(_resolution, _resolution, TextureFormat.RGBA32, true);
            _woodTexture.wrapMode = TextureWrapMode.Repeat;
            _woodTexture.filterMode = FilterMode.Bilinear;
            _woodTexture.anisoLevel = 8;
        }

        int size = _resolution;
        NativeArray<Color32> data = _woodTexture.GetRawTextureData<Color32>();

        for (int y = 0; y < size; y++)
        {
            float fy = (y + 0.5f) / size;
            for (int x = 0; x < size; x++)
            {
                float fx = (x + 0.5f) / size;

                float warp = Mathf.PerlinNoise(fx * 3.0f, fy * 1.4f) - 0.5f;
                float phase = fy * _grainFrequency + warp * _warpStrength;
                float band = Mathf.Abs(Mathf.Sin(phase * Mathf.PI));
                float line = Mathf.Pow(band, 0.35f);

                float fiber = Mathf.PerlinNoise(fx * 60f, fy * 900f);
                float blotch = Mathf.PerlinNoise(fx * 4.5f, fy * 4.5f);
                float micro = Mathf.PerlinNoise(fx * 700f, fy * 700f);

                float value = Mathf.Clamp01(0.55f + 0.45f * line + 0.06f * fiber + 0.06f * blotch + 0.03f * micro - 0.05f);
                Color color = Color.Lerp(_darkColor, _lightColor, Mathf.Clamp01((value - 0.4f) / 0.6f));

                data[y * size + x] = new Color32(
                    (byte)(Mathf.Clamp01(color.r) * 255f + 0.5f),
                    (byte)(Mathf.Clamp01(color.g) * 255f + 0.5f),
                    (byte)(Mathf.Clamp01(color.b) * 255f + 0.5f),
                    255);
            }
        }

        _woodTexture.Apply(true, false);

        if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
        _propertyBlock.SetTexture("_BaseMap", _woodTexture);

        foreach (Renderer target in _targetRenderers)
        {
            if (target == null) continue;
            target.SetPropertyBlock(_propertyBlock);
        }
    }

    private void OnDisable()
    {
        ClearPropertyBlocks();
    }

    private void OnDestroy()
    {
        DestroyTexture();
    }

    void ClearPropertyBlocks()
    {
        foreach (Renderer target in GetComponentsInChildren<Renderer>(true))
        {
            target.SetPropertyBlock(null);
        }
    }

    void DestroyTexture()
    {
        if (_woodTexture == null) return;
        if (Application.isPlaying) Destroy(_woodTexture);
        else DestroyImmediate(_woodTexture);
        _woodTexture = null;
    }
}
