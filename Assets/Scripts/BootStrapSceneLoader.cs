#if !UNITY_EDITOR
using System;
#else
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;


namespace NetcodeForGameObjects.SceneManagement.GoldenPath
{
#if UNITY_EDITOR

    /// <summary>
    /// The custom editor for the <see cref="AsteroidObject"/> component.
    /// </summary>
    [CustomEditor(typeof(BootStrapSceneLoader), true)]
    public class BootStrapSceneLoaderEditor : Editor
    {
        private SerializedProperty m_SetResolution;
        private SerializedProperty m_Fullscreen;
        private SerializedProperty m_HorizontalResolution;
        private SerializedProperty m_VerticalResolution;

        public virtual void OnEnable()
        {
            m_SetResolution = serializedObject.FindProperty(nameof(BootStrapSceneLoader.SetResolution));
            m_Fullscreen = serializedObject.FindProperty(nameof(BootStrapSceneLoader.Fullscreen));
            m_HorizontalResolution = serializedObject.FindProperty(nameof(BootStrapSceneLoader.HorizontalResolution));
            m_VerticalResolution = serializedObject.FindProperty(nameof(BootStrapSceneLoader.VerticalResolution));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_SetResolution);
            var bootStrapSceneLoader = target as BootStrapSceneLoader;
            if (bootStrapSceneLoader.SetResolution)
            {
                EditorGUILayout.PropertyField(m_Fullscreen);
                EditorGUILayout.PropertyField(m_HorizontalResolution);
                EditorGUILayout.PropertyField(m_VerticalResolution);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif


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
        [Tooltip("When enabled, you can override your project's current resoltuion settings (for development purposes)")]
        public bool SetResolution = true;

        [Tooltip("Determines if the application will render in fullscreen or windowed mode")]
        public bool Fullscreen;

        [Tooltip("Horizontal window resolution size")]
        public int HorizontalResolution = 1280;

        [Tooltip("Vertical window resolution size")]
        public int VerticalResolution = 720;

        public SceneEntry FirstSceneToLoad;

        private void Awake()
        {
            if (SetResolution)
            {
                Screen.SetResolution(HorizontalResolution, VerticalResolution, Fullscreen);
            }
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