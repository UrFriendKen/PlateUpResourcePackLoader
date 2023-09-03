using Kitchen;
using Newtonsoft.Json;
using PreferenceSystem;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace KitchenResourcePackLoader
{
    public class Main : BaseMain
    {
        public const string MOD_GUID = $"IcedMilo.PlateUp.{MOD_NAME}";
        public const string MOD_NAME = "Resource Pack Loader";
        public const string MOD_VERSION = "0.1.0";

        internal const string SELECTED_RESOURCE_PACK_NAME_ID = "selectedResourceNamePack";

        PreferenceSystemManager PrefManager;

        internal const string RESOURCE_PACKS_FOLDER_NAME = "ResourcePacks";
        internal const string DATA_FILE_NAME = "data";
        internal const string LOADED_PACKS_FILE_NAME = "loaded";

        private List<string> LoadedPacksCache = new List<string>();

        private Dictionary<string, string> ResourcePackFullPaths;
        private string ResourcePacksRootPath => Path.Combine(Application.persistentDataPath, RESOURCE_PACKS_FOLDER_NAME);

        public Main() : base(MOD_GUID, MOD_NAME, MOD_VERSION, Assembly.GetExecutingAssembly())
        {
        }

        public override void OnPostActivate(KitchenMods.Mod mod)
        {
            if (!Directory.Exists(ResourcePacksRootPath))
            {
                Directory.CreateDirectory(ResourcePacksRootPath);
            }

            List<string> resourcePackNames = Directory.GetDirectories(ResourcePacksRootPath).Select(Path.GetFileName).ToList();

            for (int i = resourcePackNames.Count() - 1; i > -1; i--)
            {
                string absPath = Path.Combine(Application.persistentDataPath, RESOURCE_PACKS_FOLDER_NAME, resourcePackNames[i], $"{DATA_FILE_NAME}.json");
                if (!File.Exists(absPath))
                    resourcePackNames.RemoveAt(i);
            }

            if (resourcePackNames.Count() > 0)
            {
                ResourcePackFullPaths = resourcePackNames.ToDictionary(x => x, x => Path.Combine(Application.persistentDataPath, RESOURCE_PACKS_FOLDER_NAME, x, $"{DATA_FILE_NAME}.json"));

                PrefManager = new PreferenceSystemManager(MOD_GUID, MOD_NAME);

                PrefManager
                    .AddLabel("Resource Pack")
                    .AddOption<string>(
                        SELECTED_RESOURCE_PACK_NAME_ID,
                        resourcePackNames.First(),
                        resourcePackNames.ToArray(),
                        resourcePackNames.ToArray()
                    )
                    .AddSpacer()
                    .AddButtonWithConfirm("Load", $"Confirm Load Resource Pack?", delegate (GenericChoiceDecision decision)
                    {
                        if (decision == GenericChoiceDecision.Accept)
                        {
                            string selectedResourcePackName = PrefManager.Get<string>(SELECTED_RESOURCE_PACK_NAME_ID);
                            LoadResourcePack(selectedResourcePackName);
                        }
                    })
                    .AddButtonWithConfirm("Reset To Default", "Confirm reset all resources to default?", delegate (GenericChoiceDecision decision)
                    {
                        if (decision == GenericChoiceDecision.Accept)
                        {
                            ResourcePack.ResetToDefaults();
                            LoadedPacksCache.Clear();
                            SaveLoadedPacksCache();
                        }
                    })
                    .AddSpacer()
                    .AddSpacer();

                PrefManager.RegisterMenu(PreferenceSystemManager.MenuType.PauseMenu);
            }
        }

        public override void PreInject()
        {
            string templatePath = Path.Combine(ResourcePacksRootPath, $"{DATA_FILE_NAME}.json");
            if (!File.Exists(templatePath))
            {
                ResourcePack rp = ResourcePack.GetTemplate();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(JsonConvert.SerializeObject(rp, Formatting.Indented));
                File.WriteAllText(templatePath, sb.ToString());
            }

            string cachePath = Path.Combine(ResourcePacksRootPath, $"{LOADED_PACKS_FILE_NAME}.json");
            if (File.Exists(cachePath))
            {
                try
                {
                    string json = File.ReadAllText(cachePath);
                    List<string> loadedPacks = JsonConvert.DeserializeObject<List<string>>(json);
                    for (int i = loadedPacks.Count - 1; i > -1; i--)
                    {
                        LoadResourcePack(loadedPacks[i], writeLoadCache: false);
                    }
                }
                catch
                {
                    Main.LogError($"Failed to reload Resource Packs from cache. Skipping");
                }
            }
        }

        private void LoadResourcePack(string name, bool writeLoadCache = true)
        {
            if (ResourcePackFullPaths.TryGetValue(name, out string absPath))
            {
                try
                {
                    string json = File.ReadAllText(absPath);
                    JsonConvert.DeserializeObject<ResourcePack>(json)?.Apply(name);
                    if (writeLoadCache)
                    {
                        int index = LoadedPacksCache.IndexOf(name);
                        if (index != -1)
                            LoadedPacksCache.RemoveAt(index);
                        LoadedPacksCache.Add(name);
                        SaveLoadedPacksCache();
                    }
                }
                catch
                {
                    Main.LogError($"Failed to deserialize Resource Pack - {name}");
                }
            }
        }

        private void SaveLoadedPacksCache()
        {
            string cachePath = Path.Combine(ResourcePacksRootPath, $"{LOADED_PACKS_FILE_NAME}.json");
            File.WriteAllText(cachePath, JsonConvert.SerializeObject(LoadedPacksCache, Formatting.Indented));
        }

        public override void PostInject()
        {
        }

        #region Logging
        public static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        public static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }
        #endregion
    }
}
