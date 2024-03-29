﻿using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Il2CppSystem.IO;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using Il2CppMonomiPark.SlimeRancher.UI;
using Il2CppMonomiPark.SlimeRancher.UI.MainMenu;
using Il2CppTMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Il2CppInterop.Runtime.Injection;
using SR2E.Saving;
using Il2CppKinematicCharacterController;
using MelonLoader.Utils;
using Il2CppMonomiPark.SlimeRancher.Damage;
using Il2CppMonomiPark.SlimeRancher.World.Teleportation;
using UnityEngine.Localization;
using SR2E.Library.Buttons;
using SR2E.Library.SaveExplorer;
using SR2E.SaveEditor;
using SR2E.Library.Storage;
using UnityEngine;

namespace SR2E
{
    public static class BuildInfo
    {
        public const string Name = "SR2E"; // Name of the Mod.  (MUST BE SET)
        public const string Description = "Essentials for Slime Rancher 2"; // Description for the Mod.  (Set as null if none)
        public const string Author = "ThatFinn"; // Author of the Mod.  (MUST BE SET)
        public const string Company = null; // Company that made the Mod.  (Set as null if none)
        public const string Version = "2.3.0"; // Version of the Mod.  (MUST BE SET)
        public const string DownloadLink = "https://www.nexusmods.com/slimerancher2/mods/60"; // Download Link for the Mod.  (Set as null if none)
    }

    public class SR2EEntryPoint : MelonMod
    {
        public static bool mSRMLIsInstalled => _mSRMLIsInstalled;
        internal static bool _mSRMLIsInstalled = false;
        internal static SR2EEntryPoint instance;
        internal static TMP_FontAsset SR2Font;
        internal static bool infEnergyInstalled = false;
        internal static bool infHealthInstalled = false;
        internal static bool consoleFinishedCreating = false;
        internal static bool mainMenuLoaded = false;

        internal static AssetBundle bundle;

        internal static IdentifiableType[] identifiableTypes { get { return GameContext.Instance.AutoSaveDirector.identifiableTypes.GetAllMembers().ToArray().Where(identifiableType => !string.IsNullOrEmpty(identifiableType.ReferenceId)).ToArray(); } }
        internal static IdentifiableType getIdentifiableByName(string name)
        {
            foreach (IdentifiableType type in identifiableTypes)
                if (type.name.ToUpper() == name.ToUpper()) return type;
            return null;
        }
        internal static IdentifiableType getIdentifiableByLocalizedName(string name)
        {
            foreach (IdentifiableType type in identifiableTypes) 
                try { if (type.LocalizedName.GetLocalizedString().ToUpper().Replace(" ","") == name.ToUpper()) return type; }
                catch {}
            return null;
        }
        internal static MelonPreferences_Category prefs;
        internal static float noclipAdjustSpeed { get { return prefs.GetEntry<float>("noclipAdjustSpeed").Value; } }
        static string onSaveLoadCommand { get { return prefs.GetEntry<string>("onSaveLoadCommand").Value; } }
        static string onMainMenuLoadCommand { get { return prefs.GetEntry<string>("onMainMenuLoadCommand").Value; } }
        internal static bool syncConsole { get { return prefs.GetEntry<bool>("doesConsoleSync").Value; } }
        internal static bool skipEngagementPrompt { get { return prefs.GetEntry<bool>("skipEngagementPrompt").Value; } }
        internal static bool consoleUsesSR2Font { get { return prefs.GetEntry<bool>("consoleUsesSR2Font").Value; } }
        internal static bool debugLogging { get { return prefs.GetEntry<bool>("debugLogging").Value; } }
        internal static bool devMode { get { return prefs.GetEntry<bool>("experimentalStuff").Value; } }
        internal static bool showUnityErrors { get { return prefs.GetEntry<bool>("showUnityErrors").Value; } }
        internal static float noclipSpeedMultiplier { get { return prefs.GetEntry<float>("noclipSprintMultiply").Value; } }
        internal static bool enableDebugDirector { get { return prefs.GetEntry<bool>("enableDebugDirector").Value; } }

        internal static void RefreshPrefs()
        {
            prefs.DeleteEntry("noclipFlySpeed");
            prefs.DeleteEntry("noclipFlySprintSpeed");

            if (!prefs.HasEntry("noclipAdjustSpeed"))
                prefs.CreateEntry("noclipAdjustSpeed", (float)235f, "NoClip scroll speed", false).disableWarning();
            if (!prefs.HasEntry("consoleUsesSR2Font"))
                prefs.CreateEntry("consoleUsesSR2Font", (bool)false, "Console uses SR2 font", false).disableWarning((System.Action)(
                    () =>
                    {
                        SetupFonts(); 
                    }));

            if (!prefs.HasEntry("showUnityErrors"))
                prefs.CreateEntry("showUnityErrors", (bool)false, "Shows unity errors in the console", true);
            
            if (!prefs.HasEntry("enableDebugDirector"))
                prefs.CreateEntry("enableDebugDirector", (bool)false, "Enable Debug Menu", false).disableWarning((System.Action)(
                    () => { SR2EDebugDirector.isEnabled = enableDebugDirector; }));
            
            if (!prefs.HasEntry("doesConsoleSync"))
                prefs.CreateEntry("doesConsoleSync", (bool)false, "Console sync with ML log", false).disableWarning((System.Action)(
                    () =>
                    {
                        if (consoleUsesSR2Font != SR2EConsole.syncedSetuped)
                        {
                            if(SR2EConsole.syncedSetuped) SR2EConsole.SetupConsoleSync();
                            else SR2EConsole.UnSetupConsoleSync();
                        }
                        SetupFonts();
                    }));
            if (!prefs.HasEntry("skipEngagementPrompt"))
                prefs.CreateEntry("skipEngagementPrompt", (bool)false, "Skip the engagement prompt", false).disableWarning();
            if (!prefs.HasEntry("debugLogging"))
                prefs.CreateEntry("debugLogging", (bool)false, "Log debug info", false).disableWarning();
            if (!prefs.HasEntry("experimentalStuff"))
                prefs.CreateEntry("experimentalStuff", (bool)false, "Enable experimental stuff", true);
            if (!prefs.HasEntry("onSaveLoadCommand"))
                prefs.CreateEntry("onSaveLoadCommand", (string)"", "Execute command when save is loaded", false).disableWarning();
            if (!prefs.HasEntry("onMainMenuLoadCommand"))
                prefs.CreateEntry("onMainMenuLoadCommand", (string)"", "Execute command when main menu is loaded", false).disableWarning();
            if (!prefs.HasEntry("noclipSprintMultiply"))
                prefs.CreateEntry("noclipSprintMultiply", 2f, "Noclip sprint speed multiplier", false).disableWarning();
        }

        private static bool throwErrors = false;
        
        public override void OnLateInitializeMelon()
        {
            foreach (MelonBase melonBase in MelonBase.RegisteredMelons)
            {
                if (melonBase is SR2EMod)
                {
                    SR2EMod mod = melonBase as SR2EMod;
                    mods.Add(mod);
                    MelonLogger.Msg("SR2ELibrary registered mod: " + mod.MelonAssembly.Assembly.FullName);
                }
            }
            if (Get<GameObject>("SR2ELibraryROOT")) { rootOBJ = Get<GameObject>("SR2ELibraryROOT"); }
            else
            {
                rootOBJ = new GameObject();
                rootOBJ.SetActive(false);
                rootOBJ.name = "SR2ELibraryROOT";
                Object.DontDestroyOnLoad(rootOBJ);
            }
        }
        //Logging code from KomiksPL
        private static void AppLogUnity(string message, string trace, LogType type)
        {
            if (message.Equals(string.Empty))
                return;

            string toDisplay = message;
            if (!trace.Equals(string.Empty))
                toDisplay += "\n" + trace;
            toDisplay = Regex.Replace(toDisplay, @"\[INFO]\s|\[ERROR]\s|\[WARNING]\s", "");

            switch (type)
            {
                case LogType.Assert:
                    MelonLogger.Error("[Unity] "+toDisplay);
                    break;
                case LogType.Exception:
                    MelonLogger.Error("[Unity] "+toDisplay+trace);
                    break;
                case LogType.Log:
                    MelonLogger.Msg("[Unity] "+toDisplay);
                    break;
                case LogType.Error:
                    MelonLogger.Error("[Unity] "+toDisplay+trace);
                    break;
                case LogType.Warning:
                    MelonLogger.Warning("[Unity] "+toDisplay);
                    break;
            }
        }
        public override void OnInitializeMelon()
        {
            instance = this;
            prefs = MelonPreferences.CreateCategory("SR2Essentials","SR2Essentials");
            RefreshPrefs();
            if (showUnityErrors)
                Application.add_logMessageReceived(new Action<string, string, LogType>(AppLogUnity));
            
            string path = Path.Combine(MelonEnvironment.ModsDirectory, "SR2EssentialsMod.dll");
            if (File.Exists(path))
                throwErrors = true;
            foreach (MelonBase melonBase in MelonBase.RegisteredMelons)
                switch (melonBase.Info.Name)
                {
                    case "InfiniteEnergy":
                        infEnergyInstalled = true;
                        break;
                    case "InfiniteHealth":
                        infHealthInstalled = true;
                        break;
                    case "mSRML":
                        _mSRMLIsInstalled = true;
                        break;
                }
        }

        public override void OnApplicationQuit()
        {
            if (SystemContext.Instance.SceneLoader.IsCurrentSceneGroupGameplay())
            {
                GameContext.Instance.AutoSaveDirector.SaveGame();
            }
        }

        public static Damage KillDamage => killDamage;
        internal static Damage killDamage;
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {

            switch (sceneName)
            {
                case "SystemCore":
                    System.IO.Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SR2E.srtwoessentials.assetbundle");
                    byte[] buffer = new byte[16 * 1024];
                    System.IO.MemoryStream ms = new System.IO.MemoryStream();
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        ms.Write(buffer, 0, read);

                    bundle = AssetBundle.LoadFromMemory(ms.ToArray());
                    foreach (var obj in bundle.LoadAllAssets())
                        if (obj != null)
                            if (obj.name == "AllMightyMenus")
                            {
                                Object.Instantiate(obj);
                                if (skipEngagementPrompt)
                                {

                                    var logoImage = Get<AssetBundle>("56edcc1f1a2084c913ac2ec89d09b725.bundle").LoadAsset("Assets/UI/Textures/MainMenu/logoSR2.png").Cast<Texture2D>();
                                    var logoSprite = Sprite.Create(logoImage, new Rect(0f, 0f, logoImage.width, logoImage.height), new Vector2(0.5f, 0.5f), 1f);

                                    GameObject skipMessage = Get<GameObject>("EngagementSkipMessage");
                                    skipMessage.SetActive(true);
                                    skipMessage.getObjRec<Image>("logo").sprite = logoSprite;
                                }
                            }
                    
                    var saveEditor = Object.Instantiate(bundle.LoadAsset("assets/SaveEditor.prefab"));
                    saveEditor.Cast<GameObject>().AddComponent<SR2ESaveEditor>();
                    
                    foreach (MelonBase baseMelonBase in MelonBase.RegisteredMelons)
                        if (baseMelonBase is SR2EMod)
                            (baseMelonBase as SR2EMod).OnSystemSceneLoaded();
                    break;
                case "MainMenuUI":
                    
                    if (!System.String.IsNullOrEmpty(onMainMenuLoadCommand)) SR2EConsole.ExecuteByString(onMainMenuLoadCommand);
                    SaveCountChanged = false;
                    Time.timeScale = 1f;
                    break;
                case "LoadScene":
                    if (skipEngagementPrompt)
                        SR2EConsole.transform.getObjRec<GameObject>("EngagementSkipMessage").SetActive(false);
                    break;
                case "StandaloneEngagementPrompt":
                    PlatformEngagementPrompt prompt = Object.FindObjectOfType<PlatformEngagementPrompt>();
                    Object.FindObjectOfType<CompanyLogoScene>().StartLoadingIndicator();
                    prompt.EngagementPromptTextUI.SetActive(false);
                    prompt.OnInteract(new InputAction.CallbackContext());
                    break;
                case "GameCore":
                    killDamage = new Damage { Amount = 99999999, DamageSource = ScriptableObject.CreateInstance<DamageSourceDefinition>(), };
                    killDamage.DamageSource.hideFlags |= HideFlags.HideAndDontSave;
                    AutoSaveDirector autoSaveDirector = GameContext.Instance.AutoSaveDirector;
                    autoSaveDirector.saveSlotCount = 50;
                    
                    foreach (ParticleSystemRenderer particle in Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>())
                    {
                        var pname = particle.gameObject.name.Replace(' ', '_');
                        if (!FXLibrary.ContainsKey(particle.gameObject))
                            FXLibrary.AddItems(particle.gameObject, particle, pname);
                        if (!FXLibraryReversable.ContainsKey(pname))
                            FXLibraryReversable.AddItems(pname, particle, particle.gameObject);
                    }

                    foreach (MelonBase baseMelonBase in MelonBase.RegisteredMelons)
                        if (baseMelonBase is SR2EMod) (baseMelonBase as SR2EMod).OnGameCoreLoaded();
                    break;
                case "UICore":
                    if (!System.String.IsNullOrEmpty(onSaveLoadCommand)) SR2EConsole.ExecuteByString(onSaveLoadCommand);
                    if(SceneContext.Instance.Player.GetComponent<SR2EDebugDirector>()==null)
                        SceneContext.Instance.Player.AddComponent<SR2EDebugDirector>();

                    var obj2 = Object.Instantiate(bundle.LoadAsset("assets/SaveExplorer.prefab"));
                    obj2.Cast<GameObject>().AddComponent<SaveExplorerRoot>();
                    
                    
                    break;
                case "zoneCore":
                    foreach (MelonBase baseMelonBase in MelonBase.RegisteredMelons)
                        if (baseMelonBase is SR2EMod) (baseMelonBase as SR2EMod).OnZoneCoreLoaded();
                    break;
                case "PlayerCore":
                    NoClipComponent.playerSettings = Get<KCCSettings>("");
                    NoClipComponent.player = SceneContext.Instance.player.transform;
                    NoClipComponent.playerController = NoClipComponent.player.GetComponent<SRCharacterController>();
                    NoClipComponent.playerMotor = NoClipComponent.player.GetComponent<KinematicCharacterMotor>();
                    player = Get<GameObject>("PlayerControllerKCC");
                    
                    foreach (MelonBase baseMelonBase in MelonBase.RegisteredMelons)
                        if (baseMelonBase is SR2EMod) (baseMelonBase as SR2EMod).OnPlayerSceneLoaded();
                    break;
                
            }
            
            SR2EConsole.OnSceneWasLoaded(buildIndex, sceneName);
        }

        internal static TMP_FontAsset defaultFont;
        internal static void SetupFonts()
        {
            if(SR2Font==null)
                SR2Font = Get<AssetBundle>("bee043ef39f15a1d9a10a5982c708714.bundle").LoadAsset("Assets/UI/Font/HemispheresCaps2/Runsell Type - HemispheresCaps2 (Latin).asset").Cast<TMP_FontAsset>();
            
            if (consoleUsesSR2Font)
                foreach (var text in SR2EConsole.gameObject.getAllChildrenOfType<TMP_Text>())
                {
                    if(defaultFont==null)
                        defaultFont= text.font;
                    text.font = SR2Font;
                }
            else if(defaultFont!=null)
                foreach (var text in SR2EConsole.gameObject.getAllChildrenOfType<TMP_Text>())
                    text.font = defaultFont;
            foreach (var text in SR2ESaveEditor.instance.gameObject.getAllChildrenOfType<TMP_Text>())
            {
                text.font = SR2Font;
                MelonLogger.Msg("LOG");
            }
            foreach (var text in SR2EConsole.gameObject.getObjRec<GameObject>("modMenu").getAllChildrenOfType<TMP_Text>())
                text.font = SR2Font;
            foreach (var text in SR2EConsole.gameObject.getObjRec<GameObject>("EngagementSkipMessage").getAllChildrenOfType<TMP_Text>()) 
                text.font = SR2Font;
            
        }

        internal static void OnSaveDirectorLoading(AutoSaveDirector autoSaveDirector)
        {
            }

        internal static void SaveDirectorLoaded()
        {
            LocalizedString label = AddTranslation("Mods", "b.button_mods_sr2e", "UI");
            new CustomMainMenuButton(label, LoadSprite("modsMenuIcon"), 2, (System.Action)(() => { SR2EModMenu.Open(); }));
            new CustomPauseMenuButton(label, 3, (System.Action)(() => { SR2EModMenu.Open(); }));
            //new CustomPauseMenuButton(AddTranslation("Save Edit", "b.button_save_edit_sr2e", "UI"), 4, (System.Action)(() => { SR2ESaveEditor.Open(); }));

            if (devMode) new CustomPauseMenuButton( AddTranslation("Debug Player", "b.debug_player_sr2e", "UI"), 3, (System.Action)(() => { LibraryDebug.TogglePlayerDebugUI();}));

        }
        public override void OnSceneWasInitialized(int buildindex, string sceneName)
        {
            switch (sceneName)
            {
                case "SystemCore":
                    break;
                case "MainMenuUI":
                    
                    mainMenuLoaded = true;
                    break;
            }
        }
        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            switch (sceneName)
            {
                case "MainMenuUI":
                    mainMenuLoaded = false;
                    break;
                case "LoadScene":
                    SR2EWarps.OnSceneLoaded(sceneName);
                    break;
            }
        }
        internal static bool SaveCountChanged = false;
        public override void OnUpdate()
        {
            if(throwErrors)
            {
                for (int i = 0; i < 5; i++) 
                    MelonLogger.BigError("SR2E", "DELETE THE OLD SR2EssentialsMod.dll!!");
                if (Screen.fullScreen)
                    Screen.SetResolution(0,0,FullScreenMode.Windowed);
            }
            if (mainMenuLoaded)
            {
                if (!SaveCountChanged)
                {
                    SaveGamesRootUI ui = GameObject.FindObjectOfType<SaveGamesRootUI>();
                    if (ui != null)
                    {
                        GameObject scrollView = GameObject.Find("ButtonsScrollView");
                        if (scrollView != null)
                        {
                            ScrollRect rect = scrollView.GetComponent<ScrollRect>();
                            rect.vertical = true;
                            Scrollbar scrollBar = GameObject.Instantiate(SR2EConsole.transform.getObjRec<Scrollbar>("saveFilesSlider"), rect.transform);
                            rect.verticalScrollbar = scrollBar;
                            rect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                            scrollBar.GetComponent<RectTransform>().localPosition += new Vector3(Screen.width/250f, 0, 0);
                            SaveCountChanged = true;
                        }
                    }
                    
                }
            }

            if (!consoleFinishedCreating)
            {
                GameObject obj = GameObject.FindGameObjectWithTag("Respawn");
                if (obj != null)
                {
                    consoleFinishedCreating = true;
                    SR2EConsole.gameObject = obj;
                    obj.name = "SR2Console";
                    GameObject.DontDestroyOnLoad(obj);
                    SR2EConsole.transform = obj.transform;
                    SR2EConsole.Start();
                }
            }
            SR2EConsole.Update();
            if(devMode)
                LibraryDebug.Update();
        }
        
    }
}