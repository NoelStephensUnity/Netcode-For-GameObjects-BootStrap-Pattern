#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
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

        private List<SceneEntry> m_ShutdownListeners = new List<SceneEntry>();

        /// <summary>
        /// Called by the <see cref="BootStrapSceneLoader"/>
        /// Sets the currently active scene
        /// </summary>
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


        /// <summary>
        /// We have a slightly different method for unloading in order to check to see if
        /// we should shutdown the NetworkManager.
        /// </summary>
        public void UpdateSceneUnloaded()
        {
            // If we are a client and this is invoked by an unload SceneEvent or we are a server and it is invoked by game logic
            // then we handle the shutdown sequence
            if (ShutdownNetworkManagerOnUnload)
            {
                ProcessSceneState = IsSceneLoaded() ? ProcessSceneStates.Loaded : ProcessSceneStates.Unloaded;
                if (ProcessSceneState != ProcessSceneStates.Unloaded)
                {
                    throw new System.Exception($"{nameof(UpdateSceneUnloaded)} expected the scene to have an unloaded status but it currently has a loaded status!");
                }
                m_ShutdownListeners.Clear();

                // Find all SceneEntries that are registered for this SceneEntry's OnUnloadedTriggerEvents
                for (int i = 0; i < OnUnloadedTriggerEvents.GetPersistentEventCount(); i++)
                {
                    var listenerObject = OnUnloadedTriggerEvents.GetPersistentTarget(i) as SceneEntry;
                    if (listenerObject != null)
                    {
                        m_ShutdownListeners.Add(listenerObject);
                    }
                }
                // Invoke the OnUnloadedTriggerEvents
                OnUnloadedTriggerEvents.Invoke();

                // Start the shutdown processing coroutine
                NetworkManager.Singleton.StartCoroutine(OnShutdownNetworkManager());
            }
            else
            {
                UpdateSceneState(LoadedScene);
            }
        }

        /// <summary>
        /// This is invoked when a scene is unloaded or loaded
        /// </summary>
        /// <param name="scene"></param>
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

        /// <summary>
        /// Method to be invoked when forcing a scene to be unloaded.
        /// This can be triggered by other SceneEntry trigger events or
        /// other things like a Button or the like.
        /// </summary>
        public void SceneUnloadTriggered()
        {
            // Special case where client is forcing the exit, we need to handle this locally
            if (ShutdownNetworkManagerOnUnload && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            {
                // This disables receiving any more event messages
                m_CurrentSceneLoader.ReceiveSceneEventMessages(false);
                // Shutdown the client
                NetworkManager.Singleton.Shutdown();
                // Handle any pre-unloading events (i.e. other levels to unloaded etc)
                PreUnloadingTriggerEvents.Invoke();

                // Now unload the scene
                var asyncOp = SceneManager.UnloadSceneAsync(LoadedScene);
                // We have a few things we need to do once the scene is unloaded.
                // Subscribe to the asyncOP completed event.
                asyncOp.completed += ClientExit_UnloadCompleted;

            }
            else // Otherwise, let the normal unloading process occur
            {
                PreUnloadingTriggerEvents.Invoke();
                OnProcessScene(ProcessSceneStates.Unloading);
            }
        }

        /// <summary>
        /// When a client exists locally, we want to set the ProcessState to Unloaded
        /// and then we invoke the SceneEntry's unloaded trigger events.
        /// </summary>
        /// <param name="obj"></param>
        private void ClientExit_UnloadCompleted(AsyncOperation obj)
        {
            ProcessSceneState = ProcessSceneStates.Unloaded;
            OnUnloadedTriggerEvents.Invoke();
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

        /// <summary>
        /// Coroutine to handle the shutdown sequence.
        /// Wait for all SceneEntries in the ShutdownListeners list to
        /// finish processing their states before completely exiting the
        /// network session.
        /// </summary>
        /// <returns></returns>
        IEnumerator OnShutdownNetworkManager()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                bool AllListenerTargetsComplete = false;
                while (!AllListenerTargetsComplete)
                {
                    // Give it a few frames for events and messages to process
                    var FramesToWait = Time.frameCount + 10;
                    yield return new WaitUntil(() => { return FramesToWait < Time.frameCount; });
                    AllListenerTargetsComplete = true;
                    foreach (var listener in m_ShutdownListeners)
                    {
                        if (!listener.IsSceneProcessed())
                        {
                            AllListenerTargetsComplete = false;
                            continue;
                        }
                    }

                    // Server waits for all clients to be disconnected before disconnecting itself
                    if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.ConnectedClientsList.Count > 1)
                    {
                        continue;
                    }
                }
                // Stop receiving scene event messages
                m_CurrentSceneLoader.ReceiveSceneEventMessages(false);
                // Shutdown/disconnect the NetworkManager session
                NetworkManager.Singleton.Shutdown();
                m_ShutdownListeners.Clear();
            }
            yield return null;
        }
    }
}