using System;
using UnityEngine;
using MelonLoader;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using System.Linq;
using System.Diagnostics;

namespace AmongUsSpeedrun
{
    public static class BuildInfo
    {
        public const string Name = "AmongUsSpeedrun";
        public const string Author = "trev";
        public const string Company = null;
        public const string Version = "1.0.0";
        public const string DownloadLink = null;
    }

    public class AmongUsSpeedrun : MelonMod
    {
        public GameObject timerPrefab;
        public Text timerText;
        public static bool timerActive;
        private static bool hasClosedLaptop;
        private static readonly Stopwatch stopwatch = new Stopwatch();

        private static MelonPreferences_Entry<bool> toggleAllTasks;

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("hi");
            LoadAssets();
            RegisterPrefs();
            HarmonyInstance.Patch(typeof(TaskAdderGame).GetMethod("Begin"), null, typeof(AmongUsSpeedrun).GetMethod("TaskAdderGamePrefix").ToNewHarmonyMethod());
            HarmonyInstance.Patch(typeof(Minigame).GetMethod("Close", new Type[] { }), null, typeof(AmongUsSpeedrun).GetMethod("MinigameClose").ToNewHarmonyMethod());
            HarmonyInstance.Patch(typeof(FreeplayPopover).GetMethod("PlayMap"), null, typeof(AmongUsSpeedrun).GetMethod("FreeplayPlayMap").ToNewHarmonyMethod());
            HarmonyInstance.Patch(typeof(DialogueBox).GetMethod("Show"), typeof(AmongUsSpeedrun).GetMethod("ShowDialogueBox2").ToNewHarmonyMethod());
        }

        public void LoadAssets()
        {
            Stream stream = Assembly.GetManifestResourceStream("AmongUsSpeedrun.speedruntimer");

            byte[] data;
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                data = ms.ToArray();
            }

            Il2CppAssetBundle bundle = Il2CppAssetBundleManager.LoadFromMemory(data);
            timerPrefab = bundle.LoadAsset("TimerCanvas.prefab").Cast<GameObject>();
            timerPrefab.hideFlags = HideFlags.DontUnloadUnusedAsset;


            // fixes a bug where it breaks the game if unityexplorer is installed, and if not, the timer being pink
            if (!File.Exists(Path.Combine(MelonHandler.ModsDirectory, "UnityExplorer.ML.IL2CPP.dll")))
            {
                Stream stream2 = Assembly.GetManifestResourceStream("AmongUsSpeedrun.unityexplorer_fixes");

                byte[] data2;
                using (var ms = new MemoryStream())
                {
                    stream2.CopyTo(ms);
                    data2 = ms.ToArray();
                }

                Il2CppAssetBundle bundle2 = Il2CppAssetBundleManager.LoadFromMemory(data2);
                Shader backupShader = bundle2.LoadAsset("DefaultUI").Cast<Shader>();
                Graphic.defaultGraphicMaterial.shader = backupShader;
            }
        }

        public void RegisterPrefs()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("Speedrun");
            toggleAllTasks = category.CreateEntry("ToggleAllTasks", true);
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "SplashIntro" || sceneName == "MainMenu") return;
            hasClosedLaptop = false;

            timerText = GameObject.Instantiate(timerPrefab).GetComponentInChildren<Text>();
            timerText.fontSize /= 2;
            timerActive = false;
            stopwatch.Reset();
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                if (!stopwatch.IsRunning)
                {
                    timerActive = true;
                    stopwatch.Restart();
                }
                else
                {
                    timerActive = false;
                    stopwatch.Stop();
                }
            }

            if (timerActive)
            {
                try
                {
                    string elapsed = stopwatch.Elapsed.ToString(@"mm\:ss\.fff");
                    timerText.text = elapsed;
                }
                catch { timerActive = false; }
            }
            else
            {
                if (PlayerControl.LocalPlayer == null) return;
                Vector2 velocity = PlayerControl.LocalPlayer.MyPhysics.body.velocity;
                if (hasClosedLaptop && (velocity.x != 0 || velocity.y != 0))
                {
                    MelonLogger.Msg("starting the timer!");
                    timerActive = true;
                    stopwatch.Restart();
                }
            }
        }

        public static void TaskAdderGamePrefix(TaskAdderGame __instance)
        {
            if (!toggleAllTasks.Value) return;

            bool temp_isDebug = false;
            MelonLogger.Msg("hello from " + __instance.name);

            List<PlayerTask> tasks = new List<PlayerTask>();
            List<PlayerTask> divertTasks = new List<PlayerTask>();
            foreach (TaskFolder folder in __instance.Root.SubFolders)
                tasks.AddRange(folder.Children.ToArray());

            MelonLogger.Msg("found " + tasks.Count + " tasks!");

            PlayerControl.LocalPlayer.ClearTasks();
            foreach (PlayerTask task in tasks)
            {
                if (temp_isDebug && task.name.ToLower().Contains("code"))
                    AddTask(task);
                if (temp_isDebug) continue;

                if (task.GetScriptClassName() == "DivertPowerTask")
                {
                    divertTasks.Add(task);
                    continue;
                }
                AddTask(task);
            }

            // jank but idk if there is a better way...
            if (!temp_isDebug)
            {
                if (AmongUsClient.Instance.TutorialMapId == 0) // skeld
                {
                    AddTask(divertTasks.Single((t) => t.name.Contains("RightEngine")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("LeftEngine")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Weapon")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Shield")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("NavPower")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Comms")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("LifeSupp")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Security")));
                }
                else if (AmongUsClient.Instance.TutorialMapId == 1) // mira
                {
                    AddTask(divertTasks.Single((t) => t.name.Contains("LaunchPad")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Medbay")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("HqComms")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Office")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Lab")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Greenhouse")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Admin")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Cafe")));
                }
                else
                    divertTasks.ForEach((t) => AddTask(t));
            }

            MelonLogger.Msg("finished adding tasks.");

            void AddTask(PlayerTask task)
            {
                TaskAddButton taskAddButton = GameObject.Instantiate(__instance.TaskPrefab);
                taskAddButton.MyTask = task;
                taskAddButton.AddTask();
                GameObject.Destroy(taskAddButton.gameObject, 1f);
            }
        }

        public static void MinigameClose(Minigame __instance)
        {
            if (__instance.name.Contains("TaskAddMinigame"))
            {
                MelonLogger.Msg("closed laptop");
                hasClosedLaptop = true;
            }
        }

        public static void ShowDialogueBox2(string dialogue)
        {
            timerActive = false;
            stopwatch.Stop();
            hasClosedLaptop = false;
        }

        public static void FreeplayPlayMap(int i)
        {
            MelonLogger.Msg("playing freemode map " + i);
        }
    }
}
