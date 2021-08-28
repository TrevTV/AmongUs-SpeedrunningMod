using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AmongUsSpeedrun
{
    public static class BuildInfo
    {
        public const string Name = "Speedrunning_Tools";
        public const string Author = "trev";
        public const string Company = null;
        public const string Version = "1.0.0";
        public const string DownloadLink = "https://github.com/trevtv/AmongUs-SpeedrunningMod";
    }

    public class SpeedrunningTools : MelonMod
    {
        public GameObject timerPrefab;
        public Text timerText;
        public static bool timerActive;
        private static bool hasClosedLaptop;
        private static readonly Stopwatch stopwatch = new Stopwatch();

        private static MelonPreferences_Entry<bool> toggleAllTasks;
        private static MelonPreferences_Entry<bool> defaultTasksTrigger;

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("hi");
            LoadAssets();
            RegisterPrefs();
            HarmonyInstance.Patch(typeof(TaskAdderGame).GetMethod("Begin"), null, typeof(SpeedrunningTools).GetMethod("TaskAdderGamePrefix").ToNewHarmonyMethod());
            HarmonyInstance.Patch(typeof(Minigame).GetMethod("Close", new Type[] { }), null, typeof(SpeedrunningTools).GetMethod("MinigameClose").ToNewHarmonyMethod());
            HarmonyInstance.Patch(typeof(FreeplayPopover).GetMethod("PlayMap"), null, typeof(SpeedrunningTools).GetMethod("FreeplayPlayMap").ToNewHarmonyMethod());
            HarmonyInstance.Patch(typeof(DialogueBox).GetMethod("Show"), typeof(SpeedrunningTools).GetMethod("ShowDialogueBox2").ToNewHarmonyMethod());
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
            defaultTasksTrigger = category.CreateEntry("DefaultTasksTrigger", false);
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

            if (Input.GetKeyDown(KeyCode.Alpha9))
                MelonCoroutines.Start(ReloadScene());

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
                if ((hasClosedLaptop || defaultTasksTrigger.Value) && (velocity.x != 0 || velocity.y != 0))
                {
                    MelonLogger.Msg("starting the timer!");
                    timerActive = true;
                    stopwatch.Restart();
                }
            }
        }

        private IEnumerator ReloadScene()
        {
            int id = AmongUsClient.Instance.TutorialMapId;
            if (AmongUsClient.Instance)
                AmongUsClient.Instance.ExitGame(InnerNet.DisconnectReasons.ExitGame);
            SceneChanger.ChangeScene("MainMenu");
            while (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
                yield return null;
            yield return null;
            HostGameButton hgb = GameObject.FindObjectOfType<HostGameButton>();
            AmongUsClient.Instance.TutorialMapId = id;
            hgb.OnClick();
        }

        public static void TaskAdderGamePrefix(TaskAdderGame __instance)
        {
            if (!toggleAllTasks.Value) return;

            MelonLogger.Msg("hello from " + __instance.name);

            List<PlayerTask> tasks = new List<PlayerTask>();
            List<PlayerTask> nodeTasks = new List<PlayerTask>();
            List<PlayerTask> divertTasks = new List<PlayerTask>();
            foreach (TaskFolder folder in __instance.Root.SubFolders)
                tasks.AddRange(folder.Children.ToArray());

            MelonLogger.Msg("found " + tasks.Count + " tasks!");

            PlayerControl.LocalPlayer.ClearTasks();
            foreach (PlayerTask task in tasks)
            {
                if (task.GetScriptClassName() == "DivertPowerTask")
                {
                    divertTasks.Add(task);
                    continue;
                }

                if (task.GetScriptClassName() == "WeatherNodeTask")
                {
                    nodeTasks.Add(task);
                    continue;
                }

                AddTask(task);
            }

            if (false)
            {
                nodeTasks.ForEach((t) => MelonLogger.Msg("NodeTask: " + t.name));
                divertTasks.ForEach((t) => MelonLogger.Msg("DivertTask: " + t.name));
            }

            // jank but idk if there is a better way...           
            switch (AmongUsClient.Instance.TutorialMapId)
            {
                case 0: // skeld
                    AddTask(divertTasks.Single((t) => t.name.Contains("RightEngine")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("LeftEngine")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Weapon")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Shield")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("NavPower")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Comms")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("LifeSupp")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Security")));
                    divertTasks.ForEach((t) => AddTask(t));
                    nodeTasks.ForEach((t) => AddTask(t));
                    break;
                case 1: // mira
                    AddTask(divertTasks.Single((t) => t.name.Contains("LaunchPad")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Medbay")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("HqComms")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Office")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Lab")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Greenhouse")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Admin")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Cafe")));
                    divertTasks.ForEach((t) => AddTask(t));
                    nodeTasks.ForEach((t) => AddTask(t));
                    break;
                case 2: // polus
                    AddTask(nodeTasks.Single((t) => t.name.Contains("CA")));
                    AddTask(nodeTasks.Single((t) => t.name.Contains("TB")));
                    AddTask(nodeTasks.Single((t) => t.name.Contains("Iro")));
                    AddTask(nodeTasks.Single((t) => t.name.Contains("Pd")));
                    AddTask(nodeTasks.Single((t) => t.name.Contains("Gi")));
                    AddTask(nodeTasks.Single((t) => t.name.Contains("Mlg")));
                    divertTasks.ForEach((t) => AddTask(t));
                    nodeTasks.ForEach((t) => AddTask(t));
                    break;
                case 4: // airship
                    AddTask(divertTasks.Single((t) => t.name.Contains("Armory")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Meeting")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Engine")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Hall")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Gap")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Cockpit")));
                    AddTask(divertTasks.Single((t) => t.name.Contains("Showers")));
                    divertTasks.ForEach((t) => AddTask(t));
                    nodeTasks.ForEach((t) => AddTask(t));
                    break;
            }

            MelonLogger.Msg("finished adding tasks.");

            void AddTask(PlayerTask task)
            {
                TaskAddButton taskAddButton = GameObject.Instantiate(__instance.TaskPrefab);
                taskAddButton.MyTask = task;
                taskAddButton.AddTask();
                GameObject.Destroy(taskAddButton.gameObject, 1f);
                if (divertTasks.Contains(task))
                    divertTasks.Remove(task);
                if (nodeTasks.Contains(task))
                    nodeTasks.Remove(task);
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