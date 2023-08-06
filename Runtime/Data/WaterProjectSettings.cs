using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;

namespace WaterSystem.Settings
{
    public class WaterProjectSettings : ScriptableObject
    {
        private static WaterProjectSettings _instance;

        /// <summary>
        /// The current instance of the WaterSystems Project Settings, this contains the base resources and also the quality settings for each level
        /// </summary>
        public static WaterProjectSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Resources.Load<WaterProjectSettings>(SettingsConsts.AssetString);
                }
                if (_instance == null)
                {
                    Debug.Log("Making new WaterProjectSettings asset");
                    _instance = CreateInstance<WaterProjectSettings>();
                    SaveSettings(_instance);
                }
                return _instance;
            }
            set => _instance = value;
        }

        /// <summary>
        /// Default base WaterQualitySettings.
        /// </summary>
        public WaterQualitySettings defaultQualitySettings;

        /// <summary>
        /// List of WaterQualitySettings for all the Quality Levels
        /// </summary>
        public List<WaterQualitySettings> qualitySettings = new List<WaterQualitySettings>();

        /// <summary>
        /// Resources for the WaterSystem, contains shaders, meshes, textures, etc..
        /// </summary>
        public Resources _resources;

        public WaterProjectSettings()
        {
            defaultQualitySettings = new WaterQualitySettings();
        }

        /// <summary>
        /// Returns the WaterQualitySettings that matches the current Quality Level.
        /// </summary>
        public static WaterQualitySettings QualitySettings
        {
            get
            {
                var qualityLevel = UnityEngine.QualitySettings.GetQualityLevel();
                if (_instance.qualitySettings.Count < qualityLevel) return _instance.defaultQualitySettings;
                return _instance.qualitySettings[qualityLevel] ?? _instance.defaultQualitySettings;
            }
        }

        public Resources resources
        {
            get
            {
                if (_resources != null) return _resources;

                _resources = new Resources();
                _resources.Init();
                return _resources;
            }
        }

        private static void SaveSettings(WaterProjectSettings settings)
        {
            // check for folder
#if UNITY_EDITOR
#if UNITY_2023_1_OR_NEWER
            if (!AssetDatabase.AssetPathExists(SettingsConsts.BuildRelativeFolder))
#else
            if (!AssetDatabase.IsValidFolder(SettingsConsts.BuildRelativeFolder))
#endif
            {
                AssetDatabase.CreateFolder(SettingsConsts.AssetFolder, SettingsConsts.Build);
            }

            AssetDatabase.CreateAsset(settings, SettingsConsts.FullBuildPath);
            AssetDatabase.SaveAssetIfDirty(settings);
#endif
        }
    }

    public static class SettingsConsts
    {
        public const string Build = "Resources";
        public const string AssetFolder = "Assets";
        public static string BuildRelativeFolder => AssetFolder + "/" + Build;
        public const string AssetString = "WaterSystemSettings";
        private const string ext = ".asset";
        public static string FullBuildPath => BuildRelativeFolder + "/" + AssetString + ext;
    }
}