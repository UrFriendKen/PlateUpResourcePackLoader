using Kitchen.Components;
using KitchenData;
using KitchenResourcePackLoader.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Rendering.DebugUI;

namespace KitchenResourcePackLoader
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ResourcePack
    {
        public string Name;
        public string PackPath => Path.Combine(Application.persistentDataPath, Main.RESOURCE_PACKS_FOLDER_NAME, Name);

        [JsonProperty]
        ReferableObjectsAudioPackObject ReferableObjectsAudioClips;

        [JsonProperty]
        PlayerAudioPackObject PlayerAudioClips;

        [JsonProperty]
        Dictionary<string, AnimationAudioPackObject> ApplianceAnimationAudioClips;

        //[JsonProperty]
        //Dictionary<string, DecorTexturePackObject> DecorTextures;

        public static ResourcePack GetTemplate()
        {
            ResourcePack rp = new ResourcePack();
            rp.ReferableObjectsAudioClips = new ReferableObjectsAudioPackObject()
            {
                Values = Enum.GetNames(typeof(SoundEvent)).ToDictionary(x => x, x => "")
            };

            rp.PlayerAudioClips = new PlayerAudioPackObject()
            {
                Values = new Dictionary<string, string>()
                {
                    { "Footstep", "" },
                    { "PickUp", "" },
                    { "Drop", "" },
                    { "Interaction", "" }
                }
            };
            rp.PlayerAudioClips.ProcessAudio = new Dictionary<string, PlayerAudioPackObject.ProcessAudioLookup>();
            foreach (Process process in GameData.Main.Get<Process>())
            {
                if (process.name.IsNullOrEmpty())
                    continue;
                rp.PlayerAudioClips.ProcessAudio.Add(process.name, new PlayerAudioPackObject.ProcessAudioLookup()
                {
                    ProcessID = process.ID,
                    Clip = ""
                });
            }

            rp.ApplianceAnimationAudioClips = new Dictionary<string, AnimationAudioPackObject>();
            foreach (Appliance appliance in GameData.Main.Get<Appliance>())
            {
                if (appliance.name.IsNullOrEmpty())
                    continue;
                AnimationSoundSource animationSoundSource = appliance.Prefab?.GetComponentsInChildren<AnimationSoundSource>().FirstOrDefault();
                if (animationSoundSource == null)
                    continue;
                rp.ApplianceAnimationAudioClips.Add(appliance.name, new AnimationAudioPackObject()
                {
                    ApplianceID = appliance.ID,
                    Values = animationSoundSource.SoundList?.ToDictionary(x => x.name, x => "")
                });
            }

            Dictionary<string, string> wallsShaderProperties = GetShaderPropertiesTemplate(ResourceUtils.GetResource<Shader>("Walls"));
            Dictionary<string, string> simpleFlatShaderProperties = GetShaderPropertiesTemplate(ResourceUtils.GetResource<Shader>("Simple Flat"));

            //rp.DecorTextures = new Dictionary<string, DecorTexturePackObject>();
            //foreach (Decor decor in GameData.Main.Get<Decor>())
            //{
            //    if (decor.name.IsNullOrEmpty())
            //        continue;

            //    Dictionary<string, string> values = null;
            //    switch (decor.Type)
            //    {
            //        case LayoutMaterialType.Wallpaper:
            //            values = wallsShaderProperties;
            //            break;
            //        case LayoutMaterialType.Floor:
            //            values = simpleFlatShaderProperties;
            //            break;
            //    }
            //    if (values == null)
            //        continue;

            //    rp.DecorTextures.Add(decor.name, new DecorTexturePackObject()
            //    {
            //        DecorID = decor.ID,
            //        Values = values
            //    });
            //}

            Dictionary<string, string> GetShaderPropertiesTemplate(Shader shader)
            {
                if (shader == null)
                    return null;
                Dictionary<string, string> properties = new Dictionary<string, string>();
                for (int i = 0; i < shader.GetPropertyCount(); i++)
                {
                    string propertyName = shader.GetPropertyName(i);
                    ShaderPropertyType propertyType = shader.GetPropertyType(i);
                    switch (propertyType)
                    {
                        case ShaderPropertyType.Vector:
                        case ShaderPropertyType.Color:
                            properties.Add(propertyName, Vector4.zero.ToString());
                            break;
                        case ShaderPropertyType.Range:
                        case ShaderPropertyType.Float:
                            properties.Add(propertyName, "0.00");
                            break;
                        case ShaderPropertyType.Texture:
                            properties.Add(propertyName, "");
                            break;
                        default:
                            break;
                    }
                }
                return properties;
            }
            return rp;
        }


        public static void ResetToDefaults()
        {
            Main.LogInfo($"Resetting resources to default...");
            new ReferableObjectsAudioPackObject().ResetToDefault();
            new PlayerAudioPackObject().ResetToDefault();
            new AnimationAudioPackObject().ResetToDefault();
            new DecorTexturePackObject().ResetToDefault();
        }

        public async void Apply(string packName)
        {
            Name = packName;
            await Apply();
        }

        private async Task<bool> Apply()
        {
            if (Name.IsNullOrEmpty())
                return false;

            Main.LogInfo($"Applying Resource Pack - {Name}");

            await ReferableObjectsAudioClips?.Apply(PackPath);

            await PlayerAudioClips?.Apply(PackPath);

            foreach (AnimationAudioPackObject animPackObj in ApplianceAnimationAudioClips.Values)
            {
                await animPackObj?.Apply(PackPath);
            }

            //foreach (DecorTexturePackObject decorTexObj in DecorTextures.Values)
            //{
            //    await decorTexObj?.Apply(PackPath);
            //}

            return true;
        }
    }
}
