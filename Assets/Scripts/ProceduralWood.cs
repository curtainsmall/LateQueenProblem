using Unity.Collections;
using UnityEngine;

[ExecuteAlways]
public class ProceduralWood : MonoBehaviour
{
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

    Texture2D _texture;
    MaterialPropertyBlock _block;

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
        if (_texture == null || _texture.width != _resolution)
        {
            DestroyTexture();
            _texture = new Texture2D(_resolution, _resolution, TextureFormat.RGBA32, true);
            _texture.wrapMode = TextureWrapMode.Repeat;
            _texture.filterMode = FilterMode.Bilinear;
            _texture.anisoLevel = 8;
        }

        int size = _resolution;
        NativeArray<Color32> data = _texture.GetRawTextureData<Color32>();

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

        _texture.Apply(true, false);

        if (_block == null) _block = new MaterialPropertyBlock();
        _block.SetTexture("_BaseMap", _texture);

        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.SetPropertyBlock(_block);
        }
    }

    private void OnDisable()
    {
        var empty = new MaterialPropertyBlock();
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.SetPropertyBlock(empty);
        }
    }

    private void OnDestroy()
    {
        DestroyTexture();
    }

    void DestroyTexture()
    {
        if (_texture == null) return;
        if (Application.isPlaying) Destroy(_texture);
        else DestroyImmediate(_texture);
        _texture = null;
    }
}