using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace NetcodeForGameObjects.SceneManagement.GoldenPath
{
    public class NetcodeSceneLoader : MonoBehaviour
    {
        public List<SceneEntry> SceneEntryList = new List<SceneEntry>();

        [HideInInspector]
        public bool IsProcessingScenes { get; internal set; }

        [HideInInspector]
        public SceneEntry SceneBeingProcessed { get; internal set; }

        private bool ShouldLoadCurrentScene() { return SceneBeingProcessed.ProcessSceneState == SceneEntry.ProcessSceneStates.Loading; }
        private bool IsNetworkSessionActive() { return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening; }
        private bool IsListeningAndNotServer() { return IsNetworkSessionActive() && !NetworkManager.Singleton.IsServer; }

        private void OnEnable()
        {
            InitializeSceneEntries();
        }

        private void InitializeSceneEntries()
        {
            if (IsListeningAndNotServer())
            {
                return;
            }

            foreach (var entry in SceneEntryList)
            {
                entry.Initialize(this);
            }
        }

        void Start()
        {
            StartSceneLoadingForState(SceneEntry.LoadSceneWhenStates.Started);
        }

        public void StartProcessingScenes()
        {
            if (!IsProcessingScenes)
            {
                IsProcessingScenes = true;
                StartCoroutine(LoadScenesCoroutine());
            }
        }

        private void StartSceneLoadingForState(SceneEntry.LoadSceneWhenStates state)
        {
            foreach (var entry in SceneEntryList.Where(c => c.LoadSceneWhen == state))
            {
                entry.OnProcessScene(SceneEntry.ProcessSceneStates.Loading);
            }
            StartProcessingScenes();
        }

        private bool ProcessNextScene()
        {
            if (SceneBeingProcessed == null)
            {
                return true;
            }
            return SceneBeingProcessed.IsSceneProcessed();
        }

        private int ScenesLeftToProcess()
        {
            return SceneEntryList.Where(c => !c.IsSceneProcessed()).Count();
        }

        private IEnumerator LoadScenesCoroutine()
        {
            // Register for scene loading notifications
            SetSceneLoadingNotifications();

            while (IsProcessingScenes)
            {
                if (!ProcessNextScene())
                {
                    yield return new WaitForSeconds(0.25f);
                }
                else if (ScenesLeftToProcess() > 0)
                {
                    SceneBeingProcessed = SceneEntryList.Where(c => !c.IsSceneProcessed()).First();
                    ProcessScene();
                }
                else
                {
                    IsProcessingScenes = false;
                    Debug.Log("Scene loading complete.");
                }
            }
            SceneBeingProcessed = null;
            // De-register for scene loading notifications
            SetSceneLoadingNotifications(false);
            yield return null;
        }

        private void ProcessScene()
        {
            if (!IsNetworkSessionActive())
            {
                if (ShouldLoadCurrentScene())
                {
                    SceneManager.LoadSceneAsync(SceneBeingProcessed.SceneNameToLoad, LoadSceneMode.Additive);
                }
                else
                {
                    SceneManager.UnloadSceneAsync(SceneBeingProcessed.LoadedScene);
                }
            }
            else
            {
                if (IsListeningAndNotServer()) { return; }
                var loadSceneStatus = ShouldLoadCurrentScene() ? NetworkManager.Singleton.SceneManager.LoadScene(
                    SceneBeingProcessed.SceneNameToLoad, LoadSceneMode.Additive) :
                    NetworkManager.Singleton.SceneManager.UnloadScene(SceneBeingProcessed.LoadedScene);

                if (loadSceneStatus != SceneEventProgressStatus.Started)
                {
                    Debug.LogError($"Failed to start loading scene {name} due to {loadSceneStatus}");
                }
            }
        }

        private void SetSceneLoadingNotifications(bool shouldAddEvent = true)
        {
            if (!IsNetworkSessionActive())
            {
                if (shouldAddEvent)
                {
                    SceneManager.sceneLoaded += SceneManager_sceneLoaded;
                    SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
                }
                else
                {
                    SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
                    SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;
                }
            }
            else if (shouldAddEvent)
            {
                ReceiveSceneEventMessages(shouldAddEvent);
            }
        }

        private bool m_IsAlreadyRegistered;
        public void ReceiveSceneEventMessages(bool shouldAddEvent)
        {
            if(shouldAddEvent && !m_IsAlreadyRegistered)
            {
                NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
                NetworkManager.Singleton.SceneManager.VerifySceneBeforeLoading = VerifySceneBeforeLoad;
                m_IsAlreadyRegistered = true;
            }
            else if (!shouldAddEvent && m_IsAlreadyRegistered)
            {
                m_IsAlreadyRegistered = false;
                if (NetworkManager.Singleton.SceneManager != null)
                {
                    NetworkManager.Singleton.SceneManager.OnSceneEvent -= SceneManager_OnSceneEvent;
                    NetworkManager.Singleton.SceneManager.VerifySceneBeforeLoading = null;
                }
            }
        }

        private bool VerifySceneBeforeLoad(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
        {
            var entries = SceneEntryList.Where(c => sceneName == c.SceneNameToLoad);
            if (entries.Count() > 0)
            {
                var entry = entries.First();
                return entry.ServerSynchronizedOnly || entry.DefaultActiveScene;
            }
            return false;
        }

        private void SceneManager_sceneUnloaded(Scene sceneUnloaded)
        {
            if (sceneUnloaded.name == SceneBeingProcessed.SceneNameToLoad)
            {
                Debug.Log($"SM-EVENT: {sceneUnloaded.name} unloaded.");
                if (!SceneBeingProcessed.IsSceneProcessed())
                {
                    SceneBeingProcessed.UpdateSceneUnloaded();
                }
            }
        }

        private void SceneManager_sceneLoaded(Scene sceneLoaded, LoadSceneMode loadSceneMode)
        {
            Debug.Log($"SM-EVENT: {sceneLoaded.name} loaded.");
            if (sceneLoaded.name == SceneBeingProcessed.SceneNameToLoad)
            {
                SceneBeingProcessed.UpdateSceneState(sceneLoaded);
            }
        }

        private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType == SceneEventData.SceneEventTypes.C2S_LoadComplete || sceneEvent.SceneEventType == SceneEventData.SceneEventTypes.C2S_UnloadComplete)
            {
                if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
                {
                    var sceneToCheck = SceneEntryList.Where(c => sceneEvent.SceneName == c.SceneNameToLoad).First();

                    if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId
                        && sceneEvent.SceneName == sceneToCheck.SceneNameToLoad)
                    {
                        if (sceneEvent.SceneEventType == SceneEventData.SceneEventTypes.C2S_LoadComplete)
                        {
                            Debug.Log($"Scene Event {sceneEvent.SceneEventType} for scene {sceneEvent.SceneName} processing for clientId {sceneEvent.ClientId}.");
                            sceneToCheck.UpdateSceneState(sceneEvent.Scene);
                        }
                        else if (sceneEvent.SceneEventType == SceneEventData.SceneEventTypes.C2S_UnloadComplete)
                        {
                            Debug.Log($"Scene Event {sceneEvent.SceneEventType} for scene {sceneEvent.SceneName} processing for clientId {sceneEvent.ClientId}.");
                            sceneToCheck.UpdateSceneUnloaded();
                        }
                    }
                }
            }
        }
    }
}