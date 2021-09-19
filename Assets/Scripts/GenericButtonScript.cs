using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NetcodeForGameObjects.SceneManagement.GoldenPath
{
    [ExecuteInEditMode]
    public class GenericButtonScript : MonoBehaviour
    {
        protected virtual void OnButtonClicked() { }
        [Tooltip("Automatically registers with the button's onClick listeners.  If disabled you have add your own OnClick event.")]
        public bool AutoRegister = true;
        [Tooltip("When set to true, this button will exit play mode or the application if running as stand alone build.")]
        public bool ExitApplication;

        /// <summary>
        /// This will automatically handle renaming the button text to the name of the button
        /// </summary>
#if UNITY_EDITOR
        Text TextComponent;


        /// <summary>
        /// Only invoked once per hierarchy update (i.e. you rename the button and the hierarchy updates)
        /// </summary>
        private void Update() { UpdateButtonName(); }

        /// <summary>
        /// Assures the button name is applied when building a stand alone
        /// </summary>
        private void OnValidate() { UpdateButtonName(); }


        private void UpdateButtonName()
        {
            if (TextComponent == null) { TextComponent = GetComponentInChildren<Text>(); }
            if (TextComponent.text != name) { TextComponent.text = name; }
        }
#endif

        private void Awake()
        {
            if (AutoRegister)
            {
                var button = GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(new UnityAction(ButtonClicked));
                }
            }
        }

        public void ButtonClicked()
        {
            OnButtonClicked();
            if (ExitApplication)
            {
#if UNITY_EDITOR
                EditorApplication.ExitPlaymode();
#else
                Application.Quit();
#endif
            }
        }
    }
}