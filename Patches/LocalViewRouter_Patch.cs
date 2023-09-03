using HarmonyLib;
using Kitchen;
using Kitchen.Components;
using KitchenResourcePackLoader.Utils;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KitchenResourcePackLoader.Patches
{
    [HarmonyPatch]
    internal static class LocalViewRouter_Patch
    {
        static FieldInfo f_FootstepSound = typeof(PlayerView).GetField("FootstepSound", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo f_PickupSound = typeof(PlayerView).GetField("PickupSound", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo f_DropSound = typeof(PlayerView).GetField("DropSound", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo f_InteractionSound = typeof(PlayerView).GetField("InteractionSound", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo f_Sounds = typeof(PlayerView).GetField("Sounds", BindingFlags.NonPublic | BindingFlags.Instance);

        private static bool DefaultPlayerAudioClipsInit = false;
        private static AudioClip DefaultFootstepSound = null;
        internal static AudioClip PendingFootstepSound = null;
        private static AudioClip DefaultPickupSound = null;
        internal static AudioClip PendingPickupSound = null;
        private static AudioClip DefaultDropSound = null;
        internal static AudioClip PendingDropSound = null;
        private static AudioClip DefaultInteractionSound = null;
        internal static AudioClip PendingInteractionSound = null;
        internal static List<PlayerView.ProcessAudioLookup> DefaultProcessSounds = null;
        internal static List<PlayerView.ProcessAudioLookup> PendingProcessSounds = null;

        internal static void PlayerSoundsResetToDefault()
        {
            PendingFootstepSound = DefaultFootstepSound;
            PendingPickupSound = DefaultPickupSound;
            PendingDropSound = DefaultDropSound;
            PendingInteractionSound = DefaultInteractionSound;
            PendingProcessSounds = DefaultProcessSounds;
        }

        private static bool HasPlayerViewUpdate => PendingFootstepSound != null || PendingPickupSound != null || PendingDropSound != null || PendingInteractionSound != null || PendingProcessSounds != null;

        [HarmonyPatch(typeof(LocalViewRouter), "GetPrefab")]
        [HarmonyPostfix]
        static void GetPrefab_Postfix(ViewType view_type, ref GameObject __result)
        {
            if (view_type == ViewType.Player && HasPlayerViewUpdate && __result != null)
            {
                PlayerView playerView = __result.GetComponent<PlayerView>();
                if (playerView != null)
                {
                    UpdateClip(f_FootstepSound, PendingFootstepSound, ref DefaultFootstepSound);
                    UpdateClip(f_PickupSound, PendingPickupSound, ref DefaultPickupSound);
                    UpdateClip(f_DropSound, PendingDropSound, ref DefaultDropSound);
                    UpdateClip(f_InteractionSound, PendingInteractionSound, ref DefaultInteractionSound);

                    if (!PendingProcessSounds.IsNullOrEmpty())
                    {
                        foreach (PlayerView.ProcessAudioLookup processAudioLookup in PendingProcessSounds)
                        {
                            UpdateProcessAudio(f_Sounds, processAudioLookup, ref DefaultProcessSounds);
                        }
                    }

                    DefaultPlayerAudioClipsInit = true;

                    void UpdateClip(FieldInfo fieldInfo, AudioClip clip, ref AudioClip defaultClip)
                    {
                        if (fieldInfo == null)
                        {
                            Main.LogError("PlayerView Field Info is null!");
                            return;
                        }

                        object obj = fieldInfo.GetValue(playerView);
                        if (obj == null || clip == null)
                            return;

                        if (!(obj is SoundSource soundSource))
                        {
                            Main.LogError("Object is not a sound source!");
                            return;
                        }

                        if (!DefaultPlayerAudioClipsInit)
                            defaultClip = soundSource.Clip;
                        soundSource.Clip = clip;
                    }

                    void UpdateProcessAudio(FieldInfo fieldInfo, PlayerView.ProcessAudioLookup audioLookup, ref List<PlayerView.ProcessAudioLookup> defaultSounds)
                    {
                        if (fieldInfo == null)
                        {
                            Main.LogError("PlayerView Field Info is null!");
                            return;
                        }

                        object obj = fieldInfo.GetValue(playerView);
                        if (obj == null || audioLookup.Value == null)
                            return;

                        if (!(obj is List<PlayerView.ProcessAudioLookup> sounds))
                        {
                            Main.LogError("Object is not a List<PlayerView.ProcessAudioLookup>!");
                            return;
                        }

                        if (!DefaultPlayerAudioClipsInit)
                            defaultSounds = new List<PlayerView.ProcessAudioLookup>(sounds);

                        for (int i = 0; i < sounds.Count; i++)
                        {
                            if (sounds[i].Key.ID == audioLookup.Key.ID)
                            {
                                sounds[i] = audioLookup;
                            }
                        }
                    }
                }
            }
        }
    }
}
