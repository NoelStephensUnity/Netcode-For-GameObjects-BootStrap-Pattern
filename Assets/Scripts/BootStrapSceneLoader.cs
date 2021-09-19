#if !UNITY_EDITOR
using System;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetcodeForGameObjects.SceneManagement.GoldenPath
{
    public class BootStrapSceneLoader : MonoBehaviour
    {
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod]
        private static void OnFirstLoad()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex != 0)
            {
                Debug.Log($"Bootstrap scene is Loading...");
                SceneManager.LoadScene(0);
            }
        }
#endif
        [Tooltip("Horizontal window resolution size")]
        public int HorizontalResolution = 1280;

        [Tooltip("Vertical window resolution size")]
        public int VerticalResolution = 720;

        public SceneEntry FirstSceneToLoad;

        private void Awake()
        {
            Screen.SetResolution(HorizontalResolution, VerticalResolution, false);
        }

        // Load the first scene when this component starts
        void Start()
        {
            if (FirstSceneToLoad != null)
            {
                SceneManager.sceneLoaded += SceneManager_sceneLoaded;
                SceneManager.LoadScene(FirstSceneToLoad.SceneNameToLoad, LoadSceneMode.Additive);
            }
            else
            {
                BootStrapError($"{nameof(FirstSceneToLoad)} is not set! Set a valid {nameof(SceneEntry)}" +
                    $" for {nameof(FirstSceneToLoad)} before proceeding.");
            }
        }

        private void SceneManager_sceneLoaded(Scene sceneLoaded, LoadSceneMode loadMode)
        {
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            var errorStatus = FirstSceneToLoad.SetAsActiveScene(sceneLoaded);
            if (errorStatus != string.Empty)
            {
                BootStrapError($"{nameof(SceneEntry.SetAsActiveScene)} failed with the error [{errorStatus}]! " +
                    $"{SceneManager.GetActiveScene().name} is still the active scene!");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void BootStrapError(string errorMSG)
        {
#if UNITY_EDITOR
            Debug.LogError(errorMSG);
#else
            throw new Exception(errorMSG);
#endif
        }
    }
}