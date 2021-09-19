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
        /// <summary>
        /// This will assure that when in the editor you will
        /// always load scenes in the same sequence as they would
        /// be loaded when running in a stand alone build
        /// </summary>
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

        /// <summary>
        /// Once the bootstrap scene is loaded, we do a quick validation
        /// check and then if everything checks out we delete this game object
        /// </summary>
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

        /// <summary>
        /// Handles errors for when in editor mode and stand alone
        /// When in stand alone an exception will be thrown for any errors
        /// </summary>
        /// <param name="errorMSG"></param>
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