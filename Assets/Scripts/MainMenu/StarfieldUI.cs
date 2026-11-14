using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class StarfieldUI : MonoBehaviour
{
    [Header("Starfield Settings")]
    [Range(0.1f, 5f)] public float speed = 0.4f;
    [Range(1, 8)] public int layers = 4;
    [Range(0.5f, 3f)] public float starSize = 1.2f;
    [Range(0.2f, 2f)] public float density = 1.0f;

    [Header("Colors")]
    public Color backgroundColor = Color.black;
    public Color primaryStarColor = Color.white;
    public Color secondaryStarColor = new Color(0.65f, 0.8f, 1f);

    private Material starMaterial;

    void Start()
    {
        RawImage img = GetComponent<RawImage>();
        Shader shader = CreateStarfieldShader();
        
        if (shader != null)
        {
            starMaterial = new Material(shader);
            img.material = starMaterial;
            UpdateShaderProperties();
        }
    }

    void Update()
    {
        if (starMaterial != null)
        {
            UpdateShaderProperties();
        }
    }

    void UpdateShaderProperties()
    {
        starMaterial.SetFloat("_Speed", speed);
        starMaterial.SetInt("_Layers", layers);
        starMaterial.SetFloat("_StarSize", starSize);
        starMaterial.SetFloat("_Density", density);
        starMaterial.SetColor("_BgColor", backgroundColor);
        starMaterial.SetColor("_ColorA", primaryStarColor);
        starMaterial.SetColor("_ColorB", secondaryStarColor);
        starMaterial.SetVector("_Resolution", new Vector4(Screen.width, Screen.height, 0, 0));
    }

    Shader CreateStarfieldShader()
    {
        string shaderCode = @"
        Shader ""Hidden/UIStarfield""
        {
            Properties
            {
                _MainTex (""Texture"", 2D) = ""white"" {}
            }
            SubShader
            {
                Tags { ""Queue""=""Transparent"" ""RenderType""=""Transparent"" }
                Blend SrcAlpha OneMinusSrcAlpha
                ZWrite Off
                Cull Off

                Pass
                {
                    CGPROGRAM
                    #pragma vertex vert
                    #pragma fragment frag
                    #include ""UnityCG.cginc""

                    struct appdata
                    {
                        float4 vertex : POSITION;
                        float2 uv : TEXCOORD0;
                    };

                    struct v2f
                    {
                        float2 uv : TEXCOORD0;
                        float4 vertex : SV_POSITION;
                    };

                    float _Speed;
                    int _Layers;
                    float _StarSize;
                    float _Density;
                    float4 _BgColor;
                    float4 _ColorA;
                    float4 _ColorB;
                    float4 _Resolution;

                    v2f vert (appdata v)
                    {
                        v2f o;
                        o.vertex = UnityObjectToClipPos(v.vertex);
                        o.uv = v.uv;
                        return o;
                    }

                    float Hash21(float2 p)
                    {
                        p = frac(p * float2(123.34, 456.21));
                        p += dot(p, p + 45.32);
                        return frac(p.x * p.y);
                    }

                    float3 StarLayer(float2 uv, float scale)
                    {
                        float3 col = float3(0, 0, 0);
                        float2 gv = frac(uv) - 0.5;
                        float2 id = floor(uv);

                        for (int y = -1; y <= 1; y++)
                        {
                            for (int x = -1; x <= 1; x++)
                            {
                                float2 offs = float2(x, y);
                                float n = Hash21(id + offs);
                                
                                if (n > (1.0 - 0.35 * _Density))
                                {
                                    float size = frac(n * 345.32) * 0.8 + 0.2;
                                    float2 pos = offs + float2(frac(n * 12.34), frac(n * 56.78)) - 0.5;
                                    float d = length(gv - pos);
                                    
                                    // Klarer Kern + sehr dezenter, eng begrenzter Glow
                                    float radius = 0.025 * _StarSize * size;
                                    float core = smoothstep(radius, radius * 0.6, d);
                                    float glow = (0.004 * _StarSize * size) / max(d, 0.005);
                                    glow *= smoothstep(0.14, 0.0, d);
                                    
                                    float star = core + glow;
                                    
                                    float3 starCol = lerp(_ColorA.rgb, _ColorB.rgb, frac(n * 91.7));
                                    col += star * starCol;
                                }
                            }
                        }
                        return col;
                    }

                    fixed4 frag (v2f i) : SV_Target
                    {
                        float2 uv = (i.uv - 0.5);
                        float aspect = _Resolution.x / max(_Resolution.y, 1.0);
                        uv.x *= aspect;

                        float t = _Time.y * _Speed;
                        float3 finalCol = _BgColor.rgb;

                        for (int l = 0; l < 8; l++)
                        {
                            if (l >= _Layers) break;
                            
                            float depth = frac(float(l) / float(_Layers) + t);
                            float scale = lerp(18.0, 0.5, depth);
                            float fade = depth * smoothstep(1.0, 0.85, depth);
                            
                            float2 layerUV = uv * scale + float(l) * 17.3;
                            finalCol += StarLayer(layerUV, scale) * fade;
                        }

                        return float4(finalCol, 1.0);
                    }
                    ENDCG
                }
            }
        }";

        #if UNITY_EDITOR
        return UnityEditor.ShaderUtil.CreateShaderAsset(shaderCode);
        #else
        return Shader.Find("Hidden/UIStarfield");
        #endif
    }
}
