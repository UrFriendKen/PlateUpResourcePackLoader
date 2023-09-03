using KitchenData;
using System.Reflection;
using UnityEngine;

namespace KitchenResourcePackLoader.Utils
{
    public static class AudioUtils
    {
        static FieldInfo f_Clip = typeof(AudioAsset).GetField("Clip", BindingFlags.NonPublic | BindingFlags.Instance);
        public static AudioAsset ToAudioAsset(this AudioClip clip)
        {
            AudioAsset audioAsset = new AudioAsset();
            object obj = (object)audioAsset;
            f_Clip.SetValue(obj, clip);
            return (AudioAsset)obj;
        }
    }
}
