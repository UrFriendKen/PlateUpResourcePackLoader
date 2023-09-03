using Kitchen;
using Kitchen.Components;
using KitchenData;
using KitchenResourcePackLoader.Patches;
using KitchenResourcePackLoader.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace KitchenResourcePackLoader
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class ResourcePackObject
    {
        [JsonProperty]
        public Dictionary<string, string> Values;

        protected abstract void CacheDefault();
        public abstract void ResetToDefault();
        public async Task<bool> Apply(string packRootPath)
        {
            if (Values.IsNullOrEmpty())
                return true;
            CacheDefault();
            Dictionary<string, string> preprocessed = DictionaryKeysToLower(Values);
            return await OnApply(packRootPath, preprocessed);
        }
        public abstract Task<bool> OnApply(string packPathRoot, Dictionary<string, string> values);

        private Dictionary<string, string> DictionaryKeysToLower(Dictionary<string, string> raw)
        {
            Dictionary<string, string> output = new Dictionary<string, string>();

            foreach (var kvp in raw)
            {
                string key = kvp.Key.ToLowerInvariant();
                if (!output.ContainsKey(key))
                {
                    output.Add(key, kvp.Value);
                }
            }
            return output;
        }
    }

    public abstract class AudioPackObject : ResourcePackObject
    {
        protected AudioType AudioTypeFromExtension(string extension)
        {
            switch (extension)
            {
                case "ogg":
                    return AudioType.OGGVORBIS;
                case "mp2":
                case "mp3":
                    return AudioType.MPEG;
                case "wav":
                    return AudioType.WAV;
                default:
                    return AudioType.UNKNOWN;
            }
        }

        protected async Task<AudioClip> GetAudioClipFromPath(string packPathRoot, string relpath)
        {
            relpath = relpath.Trim();
            if (relpath.IsNullOrEmpty())
                return null;

            string assetPath = Path.Combine(packPathRoot, relpath);
            if (!File.Exists(assetPath))
            {
                Main.LogWarning($"{relpath} not found!");
                return null;
            }

            string extension = Path.GetExtension(relpath);
            return await FileUtils.LoadAudioClip(assetPath, AudioTypeFromExtension(extension));
        }
    }

    public abstract class TexturePackObject : ResourcePackObject
    {
        protected async Task<Texture2D> GetTextureFromPath(string packPathRoot, string relpath)
        {
            relpath = relpath.Trim();
            if (relpath.IsNullOrEmpty())
                return null;

            string assetPath = Path.Combine(packPathRoot, relpath);
            if (!File.Exists(assetPath))
            {
                Main.LogWarning($"{relpath} not found!");
                return null;
            }

            return await FileUtils.LoadTexture(assetPath);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class AnimationAudioPackObject : AudioPackObject
    {
        [JsonProperty]
        public int ApplianceID;

        static Dictionary<int, List<AudioClip>> DefaultApplianceSoundLists;

        protected override void CacheDefault()
        {
            if (DefaultApplianceSoundLists != null)
                return;
            DefaultApplianceSoundLists = new Dictionary<int, List<AudioClip>>();
            if (ApplianceID != 0 && !DefaultApplianceSoundLists.ContainsKey(ApplianceID) && GameData.Main.TryGet(ApplianceID, out Appliance appliance))
            {
                AnimationSoundSource animationSoundSource = appliance.Prefab?.GetComponentsInChildren<AnimationSoundSource>().FirstOrDefault();
                if (animationSoundSource != null)
                {
                    DefaultApplianceSoundLists.Add(ApplianceID, animationSoundSource.SoundList);
                }
            }
        }

        public override void ResetToDefault()
        {
            if (DefaultApplianceSoundLists == null)
                return;

            foreach (KeyValuePair<int, List<AudioClip>> cachedItem in DefaultApplianceSoundLists)
            {
                int applianceID = cachedItem.Key;
                List<AudioClip> cachedClips = cachedItem.Value;
                if (applianceID != 0 && GameData.Main.TryGet(applianceID, out Appliance appliance))
                {
                    AnimationSoundSource animationSoundSource = appliance.Prefab?.GetComponentsInChildren<AnimationSoundSource>().FirstOrDefault();
                    if (animationSoundSource != null)
                    {
                        animationSoundSource.SoundList = cachedClips;
                    }
                }
            }
            DefaultApplianceSoundLists = null;
        }

        public override async Task<bool> OnApply(string packPathRoot, Dictionary<string, string> values)
        {
            if (!GameData.Main.TryGet(ApplianceID, out Appliance appliance))
                return false;
            AnimationSoundSource animationSoundSource = appliance.Prefab?.GetComponentsInChildren<AnimationSoundSource>().FirstOrDefault();
            if (animationSoundSource == null)
            {
                return false;
            }

            bool allSuccess = true;
            for (int i = 0; i < animationSoundSource.SoundList.Count; i++)
            {
                if (!values.TryGetValue(animationSoundSource.SoundList[i].name.ToLowerInvariant(), out string relpath) || relpath.Trim().IsNullOrEmpty())
                    continue;

                AudioClip clip = await GetAudioClipFromPath(packPathRoot, relpath);
                if (clip == null)
                {
                    allSuccess = false;
                    continue;
                }

                clip.name = animationSoundSource.SoundList[i].name;
                animationSoundSource.SoundList[i] = clip;
            }
            return allSuccess;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ReferableObjectsAudioPackObject : AudioPackObject
    {
        protected static Dictionary<SoundEvent, IAudioAsset> DefaultReferableObjectsAudioClips;

        protected override void CacheDefault()
        {
            if (DefaultReferableObjectsAudioClips == null)
                DefaultReferableObjectsAudioClips = new Dictionary<SoundEvent, IAudioAsset>(GameData.Main.ReferableObjects.Clips);
        }

        public override void ResetToDefault()
        {
            if (DefaultReferableObjectsAudioClips != null)
            {
                GameData.Main.ReferableObjects.Clips = DefaultReferableObjectsAudioClips;
                DefaultReferableObjectsAudioClips = null;
            }
        }

        public override async Task<bool> OnApply(string packPathRoot, Dictionary<string, string> values)
        {
            bool allSuccess = true;
            foreach (SoundEvent soundEvent in Enum.GetValues(typeof(SoundEvent)).Cast<SoundEvent>())
            {
                if (!values.TryGetValue(soundEvent.ToString().ToLowerInvariant(), out string relpath) || relpath.Trim().IsNullOrEmpty())
                    continue;

                AudioClip clip = await GetAudioClipFromPath(packPathRoot, relpath);
                if (clip == null)
                {
                    allSuccess = false;
                    continue;
                }

                AudioAsset asset = clip.ToAudioAsset();
                GameData.Main.ReferableObjects.Clips[soundEvent] = asset;
            }
            return allSuccess;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class PlayerAudioPackObject : AudioPackObject
    {
        [JsonObject(MemberSerialization.OptIn)]
        public class ProcessAudioLookup
        {
            [JsonProperty]
            public int ProcessID;
            [JsonProperty]
            public string Clip;
        }

        [JsonProperty]
        public Dictionary<string, ProcessAudioLookup> ProcessAudio;

        protected override void CacheDefault()
        {
        }

        public override void ResetToDefault()
        {
            LocalViewRouter_Patch.PlayerSoundsResetToDefault();
        }

        public override async Task<bool> OnApply(string packPathRoot, Dictionary<string, string> values)
        {
            bool allSuccess = true;
            Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
            foreach (KeyValuePair<string, string> item in values)
            {
                string relpath = item.Value;
                if (relpath.Trim().IsNullOrEmpty())
                    continue;
                AudioClip clip = await GetAudioClipFromPath(packPathRoot, relpath);
                if (clip == null)
                {
                    allSuccess = false;
                    continue;
                }

                clips.Add(item.Key, clip);
            }

            List<PlayerView.ProcessAudioLookup> processSounds = new List<PlayerView.ProcessAudioLookup>();
            if (!ProcessAudio.IsNullOrEmpty())
            {
                foreach (ProcessAudioLookup processAudioLookup in ProcessAudio.Values)
                {
                    string relpath = processAudioLookup.Clip.Trim();
                    if (relpath.IsNullOrEmpty() || !GameData.Main.TryGet(processAudioLookup.ProcessID, out Process process))
                        continue;

                    AudioClip clip = await GetAudioClipFromPath(packPathRoot, relpath);
                    if (clip == null)
                    {
                        allSuccess = false;
                        continue;
                    }

                    processSounds.Add(new PlayerView.ProcessAudioLookup()
                    {
                        Key = process,
                        Value = clip
                    });
                }
            }

            ApplyAudioClips(clips, processSounds);
            return allSuccess;
        }

        private void ApplyAudioClips(Dictionary<string, AudioClip> clips, List<PlayerView.ProcessAudioLookup> processSounds)
        {
            if (clips.TryGetValue("footstep", out AudioClip footstepClip))
                LocalViewRouter_Patch.PendingFootstepSound = footstepClip;
            if (clips.TryGetValue("pickup", out AudioClip pickupClip))
                LocalViewRouter_Patch.PendingPickupSound = pickupClip;
            if (clips.TryGetValue("drop", out AudioClip dropClip))
                LocalViewRouter_Patch.PendingDropSound = dropClip;
            if (clips.TryGetValue("interaction", out AudioClip interactionClip))
                LocalViewRouter_Patch.PendingInteractionSound = interactionClip;
            LocalViewRouter_Patch.PendingProcessSounds = processSounds;

        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class DecorTexturePackObject : TexturePackObject
    {
        [JsonProperty]
        public int DecorID;

        static Dictionary<int, Material> DefaultDecorMaterials;
        protected override void CacheDefault()
        {
            if (DefaultDecorMaterials != null)
                return;
            DefaultDecorMaterials = new Dictionary<int, Material>();
            if (DecorID != 0 && !DefaultDecorMaterials.ContainsKey(DecorID) && GameData.Main.TryGet(DecorID, out Decor decor))
            {
                DefaultDecorMaterials.Add(DecorID, decor.Material);
            }
        }

        public override void ResetToDefault()
        {
            if (DefaultDecorMaterials == null)
                return;

            foreach (KeyValuePair<int, Material> cachedItem in DefaultDecorMaterials)
            {
                int decorID = cachedItem.Key;
                Material material = cachedItem.Value;
                if (decorID != 0 && GameData.Main.TryGet(decorID, out Decor decor))
                {
                    decor.Material = material;
                }
            }
            DefaultDecorMaterials = null;
        }

        public override async Task<bool> OnApply(string packPathRoot, Dictionary<string, string> values)
        {
            if (!GameData.Main.TryGet(DecorID, out Decor decor))
                return false;

            Shader shader = null;
            switch (decor.Type)
            {
                case LayoutMaterialType.Wallpaper:
                    shader = ResourceUtils.GetResource<Shader>("Walls");
                    break;
                case LayoutMaterialType.Floor:
                    shader = ResourceUtils.GetResource<Shader>("Simple Flat");
                    break;
            }

            if (shader == null)
            {
                Main.LogError("Shader is null");
                return false;
            }

            bool allSuccess = true;
            Material newDecorMaterial = new Material(shader);
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                string shaderPropertyName = shader.GetPropertyName(i);
                if (!values.TryGetValue(shaderPropertyName.ToLowerInvariant(), out string value) || value.Trim().IsNullOrEmpty())
                    continue;
                ShaderPropertyType propertyType = shader.GetPropertyType(i);
                value = value.Trim();
                switch (propertyType)
                {
                    case ShaderPropertyType.Color:
                        if (!VectorUtils.TryParse(value, out Vector4 vector1))
                        {
                            allSuccess = false;
                            break;
                        }
                        Color color = new Color(vector1.x, vector1.y, vector1.z, vector1.w);
                        newDecorMaterial.SetColor(shaderPropertyName, color);
                        break;
                    case ShaderPropertyType.Range:
                    case ShaderPropertyType.Float:
                        if (!float.TryParse(value, out float floatVal))
                        {
                            allSuccess = false;
                            break;
                        }
                        newDecorMaterial.SetFloat(shaderPropertyName, floatVal);
                        break;
                    case ShaderPropertyType.Texture:
                        Texture2D texture = await GetTextureFromPath(packPathRoot, value);
                        if (texture == null)
                        {
                            allSuccess = false;
                            break;
                        }
                        Main.LogInfo("Setting Texture");
                        newDecorMaterial.SetTexture(shaderPropertyName, texture);
                        if (shaderPropertyName == "_Overlay")
                            newDecorMaterial.EnableKeyword("_HASTEXTUREOVERLAY_ON");
                        break;
                    case ShaderPropertyType.Vector:
                        if (!VectorUtils.TryParse(value, out Vector4 vector2))
                        {
                            allSuccess = false;
                            break;
                        }
                        Main.LogInfo(vector2);
                        newDecorMaterial.SetVector(shaderPropertyName, vector2);
                        break;
                    default:
                        break;
                }
            }
            decor.Material = newDecorMaterial;

            return allSuccess;
        }
    }
}
