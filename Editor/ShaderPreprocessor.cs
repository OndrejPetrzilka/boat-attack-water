using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using WaterSystem;
using WaterSystem.Settings;

class ShaderPreprocessor : IPreprocessShaders
{
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

    readonly ShaderKeyword[] m_keywords;

    public int callbackOrder
    {
        get { return 0; }
    }

    public ShaderPreprocessor()
    {
        m_keywords = new ShaderKeyword[]
        {
            m_KeywordRefCube, m_KeywordRefProbes, m_KeywordRefPlanar, m_KeywordRefSSR,
            m_KeywordRefSSR_LOW, m_KeywordRefSSR_MID, m_KeywordRefSSR_HIGH,
            m_KeywordShadow_LOW, m_KeywordShadow_MID, m_KeywordShadow_HIGH,
            m_KeywordDispersion,
        };
    }

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
    {
        if (!shader.name.StartsWith("Boat Attack/Water", StringComparison.OrdinalIgnoreCase))
            return;

        // Set of included variants, each entry is bitfield
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        HashSet<int> includedVariants = new HashSet<int>(1024);

        Include(WaterProjectSettings.Instance.defaultQualitySettings, includedVariants);
        foreach (var setting in WaterProjectSettings.Instance.qualitySettings)
        {
            Include(setting, includedVariants);
        }

        int startCount = shaderCompilerData.Count;
        for (int i = 0; i < shaderCompilerData.Count; ++i)
        {
            if (!IsIncluded(shaderCompilerData[i].shaderKeywordSet, includedVariants))
            {
                shaderCompilerData.RemoveAt(i);
                --i;
            }
        }
        Debug.Log($"Water shaders - stripped {startCount - shaderCompilerData.Count} variants in {sw.Elapsed.TotalSeconds:0.0}s");
    }

    private void Include(WaterQualitySettings setting, HashSet<int> includedVariants)
    {
        int bitField = 0;
        if (setting.reflectionSettings.reflectionType == Data.ReflectionSettings.Type.Cubemap)
        {
            AddBit(ref bitField, m_KeywordRefCube);
        }
        else if (setting.reflectionSettings.reflectionType == Data.ReflectionSettings.Type.PlanarReflection)
        {
            AddBit(ref bitField, m_KeywordRefPlanar);
        }
        else if (setting.reflectionSettings.reflectionType == Data.ReflectionSettings.Type.ReflectionProbe)
        {
            AddBit(ref bitField, m_KeywordRefProbes);
        }
        else if (setting.reflectionSettings.reflectionType == Data.ReflectionSettings.Type.ScreenSpaceReflection)
        {
            AddBit(ref bitField, m_KeywordRefSSR);

            if (setting.ssrSettings.steps == Data.SsrSettings.Steps.Low)
            {
                AddBit(ref bitField, m_KeywordRefSSR_LOW);
            }
            else if (setting.ssrSettings.steps == Data.SsrSettings.Steps.Medium)
            {
                AddBit(ref bitField, m_KeywordRefSSR_MID);
            }
            else if (setting.ssrSettings.steps == Data.SsrSettings.Steps.High)
            {
                AddBit(ref bitField, m_KeywordRefSSR_HIGH);
            }
        }

        if (setting.causticSettings.Mode == Data.CausticSettings.CausticMode.Simple && setting.causticSettings.Dispersion)
        {
            AddBit(ref bitField, m_KeywordDispersion);
        }

        if (setting.lightingSettings.Mode == Data.LightingSettings.LightingMode.Volume)
        {
            if (setting.lightingSettings.VolumeSamples == Data.LightingSettings.VolumeSample.Low)
            {
                AddBit(ref bitField, m_KeywordShadow_LOW);
            }
            else if (setting.lightingSettings.VolumeSamples == Data.LightingSettings.VolumeSample.Medium)
            {
                AddBit(ref bitField, m_KeywordShadow_MID);
            }
            else if (setting.lightingSettings.VolumeSamples == Data.LightingSettings.VolumeSample.High)
            {
                AddBit(ref bitField, m_KeywordShadow_HIGH);
            }
        }
        includedVariants.Add(bitField);
    }

    private void AddBit(ref int bitField, ShaderKeyword keyword)
    {
        for (int i = 0; i < m_keywords.Length; i++)
        {
            if (m_keywords[i].index == keyword.index)
            {
                bitField |= (1 << i);
                return;
            }
        }
    }

    private bool IsIncluded(ShaderKeywordSet shaderKeywordSet, HashSet<int> includedVariants)
    {
        // Only include sets, which contain exact variants of our keywords
        int keywordMask = 0;
        for (int i = 0; i < m_keywords.Length; i++)
        {
            if (shaderKeywordSet.IsEnabled(m_keywords[i]))
            {
                keywordMask |= (1 << i);
            }
        }
        return includedVariants.Contains(keywordMask);
    }
}