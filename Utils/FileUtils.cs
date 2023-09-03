using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace KitchenResourcePackLoader.Utils
{
    public static class FileUtils
    {
        public static async Task<AudioClip> LoadAudioClip(string path, AudioType audioType = AudioType.WAV)
        {
            AudioClip clip = null;
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, audioType))
            {
                uwr.SendWebRequest();
                try
                {
                    while (!uwr.isDone) await Task.Delay(5);

                    if (uwr.result != UnityWebRequest.Result.Success)
                        Main.LogError($"{uwr.error}");
                    else
                    {
                        clip = DownloadHandlerAudioClip.GetContent(uwr);
                    }
                }
                catch (Exception err)
                {
                    Main.LogError($"{err.Message}, {err.StackTrace}");
                }
            }
            return clip;
        }

        public static async Task<Texture2D> LoadTexture(string path)
        {
            Texture2D tex = null;
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path))
            {
                uwr.SendWebRequest();
                try
                {
                    while (!uwr.isDone) await Task.Delay(5);

                    if (uwr.result != UnityWebRequest.Result.Success)
                        Main.LogError($"{uwr.error}");
                    else
                    {
                        tex = DownloadHandlerTexture.GetContent(uwr);
                    }
                }
                catch (Exception err)
                {
                    Main.LogError($"{err.Message}, {err.StackTrace}");
                }
            }
            return tex;
        }
    }
}
