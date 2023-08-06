using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using WaterSystem;
using WaterSystem.Settings;

class ShaderPreprocessor : IPreprocessShaders
{
    struct Features : IEquatable<Features>
    {
        public Data.ReflectionSettings.Type Reflection;
        public bool Dispersion;
        public bool VolumeLight;

        public override bool Equals(object obj)
        {
            return obj is Features features && Equals(features);
        }

        public bool Equals(Features other)
        {
            return Reflection == other.Reflection &&
                   Dispersion == other.Dispersion &&
                   VolumeLight == other.VolumeLight;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Reflection, Dispersion, VolumeLight);
        }
    }

    readonly ShaderKeyword m_KeywordRefCube = new ShaderKeyword("_REFLECTION_CUBEMAP");
    readonly ShaderKeyword m_KeywordRefProbes = new ShaderKeyword("_REFLECTION_PROBES");
    readonly ShaderKeyword m_KeywordRefPlanar = new ShaderKeyword("_REFLECTION_PLANARREFLECTION");
    readonly ShaderKeyword m_KeywordRefSSR = new ShaderKeyword("_REFLECTION_SSR");

    readonly ShaderKeyword m_KeywordRefSSR_LOW = new ShaderKeyword("_SSR_SAMPLES_LOW");
    readonly ShaderKeyword m_KeywordRefSSR_MID = new ShaderKeyword("_SSR_SAMPLES_MEDIUM");
    readonly ShaderKeyword m_KeywordRefSSR_HIGH = new ShaderKeyword("_SSR_SAMPLES_HIGH");

    readonly ShaderKeyword m_KeywordShadow_LOW = new ShaderKeyword("_SHADOW_SAMPLES_LOW");
    readonly ShaderKeyword m_KeywordShadow_MID = new ShaderKeyword("_SHADOW_SAMPLES_MEDIUM");
    readonly ShaderKeyword m_KeywordShadow_HIGH = new ShaderKeyword("_SHADOW_SAMPLES_HIGH");

    readonly ShaderKeyword m_KeywordDispersion = new ShaderKeyword("_DISPERSION");

    public int callbackOrder
    {
        get { return 0; }
    }

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
    {
        if (!shader.name.StartsWith("Boat Attack/Water", StringComparison.OrdinalIgnoreCase))
            return;

        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        HashSet<Features> includedFeatures = new HashSet<Features>(128);
        Include(WaterProjectSettings.Instance.defaultQualitySettings, includedFeatures);
        foreach (var setting in WaterProjectSettings.Instance.qualitySettings)
        {
            Include(setting, includedFeatures);
        }

        List<ShaderCompilerData> strippedVariants = new List<ShaderCompilerData>(512);
        for (int i = 0; i < shaderCompilerData.Count; ++i)
        {
            if (!IsIncluded(shaderCompilerData[i].shaderKeywordSet, includedFeatures))
            {
                strippedVariants.Add(shaderCompilerData[i]);
                shaderCompilerData.RemoveAt(i);
                --i;
            }
        }

        string variantList = strippedVariants.Count > 0 ? string.Join("\r\n", strippedVariants.Select(s => FormatVariant(s))) : string.Empty;
        Debug.Log($"Water shaders - stripped {strippedVariants.Count} variants in {sw.Elapsed.TotalSeconds:0.0}s\r\n{variantList}");
    }

    private string FormatVariant(ShaderCompilerData s)
    {
        return string.Join(",", s.shaderKeywordSet.GetShaderKeywords());
    }

    private void Include(WaterQualitySettings setting, HashSet<Features> includedFeatures)
    {
        Features features;
        features.Reflection = setting.reflectionSettings.reflectionType;
        features.Dispersion = setting.causticSettings.Mode == Data.CausticSettings.CausticMode.Simple && setting.causticSettings.Dispersion;
        features.VolumeLight = setting.lightingSettings.Mode == Data.LightingSettings.LightingMode.Volume;
        includedFeatures.Add(features);
    }

    private bool IsIncluded(ShaderKeywordSet shaderKeywordSet, HashSet<Features> includedFeatures)
    {
        Features feature = GetShaderFeatures(shaderKeywordSet);

        // Only compile cube/plane/probe reflections with one SSR setting
        if (feature.Reflection != Data.ReflectionSettings.Type.ScreenSpaceReflection && (shaderKeywordSet.IsEnabled(m_KeywordRefSSR_MID) || shaderKeywordSet.IsEnabled(m_KeywordRefSSR_HIGH)))
        {
            return false;
        }
        else if (feature.Reflection < 0)
        {
            // No reflection, probably vertex shader, don't strip
            return true;
        }
        else
        {
            return includedFeatures.Contains(feature);
        }
    }

    private Features GetShaderFeatures(ShaderKeywordSet shaderKeywordSet)
    {
        Features result;
        if (shaderKeywordSet.IsEnabled(m_KeywordRefCube)) result.Reflection = Data.ReflectionSettings.Type.Cubemap;
        else if (shaderKeywordSet.IsEnabled(m_KeywordRefPlanar)) result.Reflection = Data.ReflectionSettings.Type.PlanarReflection;
        else if (shaderKeywordSet.IsEnabled(m_KeywordRefProbes)) result.Reflection = Data.ReflectionSettings.Type.ReflectionProbe;
        else if (shaderKeywordSet.IsEnabled(m_KeywordRefSSR)) result.Reflection = Data.ReflectionSettings.Type.ScreenSpaceReflection;
        else result.Reflection = (Data.ReflectionSettings.Type)(-1);

        result.Dispersion = shaderKeywordSet.IsEnabled(m_KeywordDispersion);
        result.VolumeLight = shaderKeywordSet.IsEnabled(m_KeywordShadow_LOW) || shaderKeywordSet.IsEnabled(m_KeywordShadow_MID) || shaderKeywordSet.IsEnabled(m_KeywordShadow_HIGH);
        return result;
    }
}
