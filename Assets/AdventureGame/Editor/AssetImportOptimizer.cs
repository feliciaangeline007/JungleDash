using UnityEngine;
using UnityEditor;
using System.IO;

namespace AdventureGame.Editor
{
    [InitializeOnLoad]
    public static class AssetImportOptimizer
    {
        private const string OptimizationKey = "Unity6_RawAssetsOptimized_v1";

        static AssetImportOptimizer()
        {
            EditorApplication.delayCall += CheckAndRunOptimization;
        }

        private static void CheckAndRunOptimization()
        {
            if (!SessionState.GetBool(OptimizationKey, false))
            {
                SessionState.SetBool(OptimizationKey, true);
                OptimizeAllRawAssets();
            }
        }

        [MenuItem("Migration/Optimize All Raw Assets (Step 2)", false, 10)]
        public static void OptimizeAllRawAssets()
        {
            AssetDatabase.StartAssetEditing();
            int textureCount = 0;
            int modelCount = 0;
            int audioCount = 0;

            try
            {
                string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();

                foreach (string path in allAssetPaths)
                {
                    if (!path.StartsWith("Assets/")) continue;
                    if (path.StartsWith("Assets/AdventureGame/Editor/")) continue;

                    string ext = Path.GetExtension(path).ToLowerInvariant();

                    // 1. Textures
                    if (ext == ".png" || ext == ".tga" || ext == ".tif" || ext == ".psd" || ext == ".jpg")
                    {
                        if (OptimizeTexture(path)) textureCount++;
                    }
                    // 2. Models
                    else if (ext == ".fbx" || ext == ".obj")
                    {
                        if (OptimizeModel(path)) modelCount++;
                    }
                    // 3. Audio
                    else if (ext == ".wav" || ext == ".mp3" || ext == ".ogg")
                    {
                        if (OptimizeAudio(path)) audioCount++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green><b>[Step 2 Migration Complete]</b> Optimized {textureCount} Textures, {modelCount} Models, {audioCount} Audio clips to modern Unity 6 standards.</color>");
        }

        private static bool OptimizeTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return false;

            bool isNormal = path.ToLowerInvariant().Contains("normal");
            bool isMaskOrSpec = path.ToLowerInvariant().Contains("spec") || 
                                path.ToLowerInvariant().Contains("mask") || 
                                path.ToLowerInvariant().Contains("roughness") || 
                                path.ToLowerInvariant().Contains("metallic") ||
                                path.ToLowerInvariant().Contains("occlusion") ||
                                path.ToLowerInvariant().Contains("ao");
            bool isUI = path.ToLowerInvariant().Contains("/ui/") || 
                        path.ToLowerInvariant().Contains("/fonts/") ||
                        importer.textureType == TextureImporterType.Sprite;

            bool modified = false;

            if (isNormal)
            {
                if (importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    modified = true;
                }
                if (importer.sRGBTexture)
                {
                    importer.sRGBTexture = false;
                    modified = true;
                }
            }
            else if (isMaskOrSpec)
            {
                if (importer.sRGBTexture)
                {
                    importer.sRGBTexture = false; // Critical: Linear data
                    modified = true;
                }
            }
            else if (isUI)
            {
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    modified = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    modified = true;
                }
            }
            else
            {
                if (!importer.sRGBTexture)
                {
                    importer.sRGBTexture = true;
                    modified = true;
                }
                if (!importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = true;
                    modified = true;
                }
                if (!importer.streamingMipmaps)
                {
                    importer.streamingMipmaps = true;
                    modified = true;
                }
            }

            // Android Platform Override: ASTC
            TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
            var targetFormat = isNormal ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6;
            if (!androidSettings.overridden || androidSettings.format != targetFormat)
            {
                androidSettings.overridden = true;
                androidSettings.format = targetFormat;
                importer.SetPlatformTextureSettings(androidSettings);
                modified = true;
            }

            // Standalone Platform Override: BC7 / BC5
            TextureImporterPlatformSettings standaloneSettings = importer.GetPlatformTextureSettings("Standalone");
            var pcFormat = isNormal ? TextureImporterFormat.BC5 : TextureImporterFormat.BC7;
            if (!standaloneSettings.overridden || standaloneSettings.format != pcFormat)
            {
                standaloneSettings.overridden = true;
                standaloneSettings.format = pcFormat;
                importer.SetPlatformTextureSettings(standaloneSettings);
                modified = true;
            }

            if (modified)
            {
                importer.SaveAndReimport();
                return true;
            }
            return false;
        }

        private static bool OptimizeModel(string path)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return false;

            bool modified = false;

            if (importer.isReadable)
            {
                importer.isReadable = false; // Save 50% CPU RAM
                modified = true;
            }

            if (importer.meshOptimizationFlags != MeshOptimizationFlags.Everything)
            {
                importer.meshOptimizationFlags = MeshOptimizationFlags.Everything;
                modified = true;
            }

            if (importer.indexFormat != ModelImporterIndexFormat.UInt16)
            {
                importer.indexFormat = ModelImporterIndexFormat.UInt16;
                modified = true;
            }

            if (importer.importNormals != ModelImporterNormals.Import)
            {
                importer.importNormals = ModelImporterNormals.Import;
                modified = true;
            }

            if (importer.importTangents != ModelImporterTangents.CalculateMikk)
            {
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                modified = true;
            }

            if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                modified = true;
            }

            if (modified)
            {
                importer.SaveAndReimport();
                return true;
            }
            return false;
        }

        private static bool OptimizeAudio(string path)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return false;

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            float length = clip != null ? clip.length : 1f;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            bool modified = false;

            if (length <= 2.0f)
            {
                if (settings.loadType != AudioClipLoadType.DecompressOnLoad || settings.compressionFormat != AudioCompressionFormat.PCM)
                {
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = AudioCompressionFormat.PCM;
                    modified = true;
                }
            }
            else if (length <= 10.0f)
            {
                if (settings.loadType != AudioClipLoadType.CompressedInMemory || settings.compressionFormat != AudioCompressionFormat.Vorbis)
                {
                    settings.loadType = AudioClipLoadType.CompressedInMemory;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = 0.7f;
                    modified = true;
                }
            }
            else
            {
                if (settings.loadType != AudioClipLoadType.Streaming || settings.compressionFormat != AudioCompressionFormat.Vorbis)
                {
                    settings.loadType = AudioClipLoadType.Streaming;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = 0.65f;
                    importer.loadInBackground = true;
                    modified = true;
                }
            }

            if (!importer.forceToMono)
            {
                importer.forceToMono = true; // 50% RAM savings for 3D positional audio
                modified = true;
            }

            if (!settings.preloadAudioData)
            {
                settings.preloadAudioData = true;
                modified = true;
            }

            if (modified)
            {
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
                return true;
            }
            return false;
        }
    }
}
