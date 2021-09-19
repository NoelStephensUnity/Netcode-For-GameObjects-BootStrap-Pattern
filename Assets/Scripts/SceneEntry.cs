#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace NetcodeForGameObjects.SceneManagement.GoldenPath
{
    [CreateAssetMenu(fileName = nameof(SceneEntry), menuName = "NetCodeForGameObjects/" + nameof(SceneEntry))]
    public class SceneEntry : ScriptableObject
    {
#if UNITY_EDITOR
        public SceneAsset SceneAssetToLoad;

        private void OnValidate()
        {
            if (SceneAssetToLoad != null)
            {
                SceneNameToLoad = SceneAssetToLoad.name;
            }
        }
#endif

        public enum LoadSceneWhenStates
        {
            Started,
            Triggered,
        }

        public enum ProcessSceneStates
        {
            Unloaded,
            Loading,
            Loaded,
            Unloading
        }

        public LoadSceneWhenStates LoadSceneWhen;
        [Tooltip("When true, only the server will handle loading and unloading of this scene")]
        public bool ServerSynchronizedOnly;

        public bool DefaultActiveScene { get; internal set; }

        public UnityEvent OnLoadedTriggerEvents;

        public UnityEvent PreUnloadingTriggerEvents;

        [Tooltip("When true, this will shutdown the NetworkManager upon this scene being unloaded.")]
        public bool ShutdownNetworkManagerOnUnload;
        public UnityEvent OnUnloadedTriggerEvents;

        [HideInInspector]
        public string SceneNameToLoad;

        [HideInInspector]
        public Scene LoadedScene { get; internal set; }

        [HideInInspector]
        public ProcessSceneStates ProcessSceneState { get; internal set; }

        private NetcodeSceneLoader m_CurrentSceneLoader;

        public string SetAsActiveScene(Scene scene)
        {
            if (scene.name == SceneNameToLoad)
            {
                LoadedScene = scene;
                var originalActiveScene = SceneManager.GetActiveScene();
                SceneManager.SetActiveScene(LoadedScene);
                SceneManager.UnloadSceneAsync(originalActiveScene);
                ProcessSceneState = ProcessSceneStates.Loaded;
                DefaultActiveScene = true;
                return string.Empty;
            }
            else
            {
                return $"{scene.name} does not match this {nameof(SceneEntry)}'s scene: {SceneNameToLoad}!";
            }
        }

        public bool IsSceneLoaded()
        {
            return LoadedScene.IsValid() && LoadedScene.isLoaded;
        }

        public bool IsSceneProcessed()
        {
            return !(ProcessSceneState == ProcessSceneStates.Loading || ProcessSceneState == ProcessSceneStates.Unloading);
        }

        public void EnableSceneObjects(bool isEnabled)
        {
            if (IsSceneProcessed() && IsSceneLoaded())
            {
                foreach (var gameObject in LoadedScene.GetRootGameObjects())
                {
                    gameObject.SetActive(isEnabled);
                }
            }
        }

        public void UpdateSceneUnloaded()
        {
            // If we are a client and this is invoked by an unload SceneEvent or we are a server and it is invoked by game logic
            // then we handle the shutdown sequence
            if (ShutdownNetworkManagerOnUnload)
            {
                ProcessSceneState = IsSceneLoaded() ? ProcessSceneStates.Loaded : ProcessSceneStates.Unloaded;
                (ProcessSceneState == ProcessSceneStates.Loaded ? OnLoadedTriggerEvents : OnUnloadedTriggerEvents).Invoke();

                NetworkManager.Singleton.StartCoroutine(OnShutdownNetworkManager());
            }
            else
            {
                UpdateSceneState(LoadedScene);
            }
        }

        public void UpdateSceneState(Scene scene)
        {
            LoadedScene = scene;
            if (!IsSceneProcessed())
            {
                ProcessSceneState = IsSceneLoaded() ? ProcessSceneStates.Loaded : ProcessSceneStates.Unloaded;
                (ProcessSceneState == ProcessSceneStates.Loaded ? OnLoadedTriggerEvents : OnUnloadedTriggerEvents).Invoke();
            }
        }

        public void OnProcessScene(ProcessSceneStates state)
        {
            if (ProcessSceneState != state)
            {
                ProcessSceneState = state;
                m_CurrentSceneLoader.StartProcessingScenes();
            }
        }

        public void SceneLoadTriggered()
        {
            OnProcessScene(ProcessSceneStates.Loading);
        }

        public void SceneUnloadTriggered()
        {
            // Special case where client is forcing the exit, we handle everything in one pass
            if (ShutdownNetworkManagerOnUnload && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            {
                m_CurrentSceneLoader.ReceiveSceneEventMessages(false);
                NetworkManager.Singleton.Shutdown();
                PreUnloadingTriggerEvents.Invoke();
                NetworkManager.Singleton.StartCoroutine(OnClientExiting());
            }
            else
            {
                PreUnloadingTriggerEvents.Invoke();
                OnProcessScene(ProcessSceneStates.Unloading);
            }
        }

        private IEnumerator OnClientExiting()
        {
            yield return new WaitForSeconds(0.25f);
            var asyncOp = SceneManager.UnloadSceneAsync(LoadedScene);
            asyncOp.completed += AsyncOp_completed;
            while (!asyncOp.isDone)
            {
                yield return new WaitForSeconds(0.25f);
            }
            yield return null;
        }

        private void AsyncOp_completed(AsyncOperation obj)
        {
            ProcessSceneState = IsSceneLoaded() ? ProcessSceneStates.Loaded : ProcessSceneStates.Unloaded;
            (ProcessSceneState == ProcessSceneStates.Loaded ? OnLoadedTriggerEvents : OnUnloadedTriggerEvents).Invoke();
        }

        public void Initialize(NetcodeSceneLoader netcodeSceneLoader)
        {
            m_CurrentSceneLoader = netcodeSceneLoader;
            if (IsSceneLoaded())
            {
                ProcessSceneState = ProcessSceneStates.Loaded;
            }
            else
            {
                ProcessSceneState = ProcessSceneStates.Unloaded;
            }
        }

        IEnumerator OnShutdownNetworkManager()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                // Give a little time for any final outgoing and incoming messages.
                yield return new WaitForSeconds(0.25f);
                m_CurrentSceneLoader.ReceiveSceneEventMessages(false);
                NetworkManager.Singleton.Shutdown();
            }
            yield return null;
        }
    }
}