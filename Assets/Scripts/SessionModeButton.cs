using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

namespace NetcodeForGameObjects.SceneManagement.GoldenPath
{
    public class SessionModeButton : GenericButtonScript
    {
        public enum SessionModes
        {
            Client,
            Host,
            Server,
            None
        }
        public delegate SocketTasks StartSessionModeDelegateHandler();
        [Tooltip("Will start a specific session mode or if set to None will act like a normal button.")]
        public SessionModes SessionMode;

        public UnityEvent<SessionModes> OnSessionModeAction;
        private Dictionary<SessionModes, StartSessionModeDelegateHandler> SessionModeActions;

        protected override void OnButtonClicked()
        {
            if (CanInvokeSessioinModeAction())
            {
                if (SessionModeActions == null)
                {
                    InitializeSessionModeActions();
                }
                InvokeSessionModeAction();
            }
        }

        protected bool CanInvokeSessioinModeAction()
        {
            return NetworkManager.Singleton && (SessionMode == SessionModes.None ||
               (!NetworkManager.Singleton.IsListening && SessionMode != SessionModes.None));
        }

        private void InvokeSessionModeAction()
        {
            if (NetworkManager.Singleton != null)
            {
                if (SessionMode != SessionModes.None && !NetworkManager.Singleton.IsListening)
                {
                    SessionModeActions[SessionMode].Invoke();
                    NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(UnityEngine.SceneManagement.LoadSceneMode.Additive);
                    NetworkManager.Singleton.SceneManager.DisableValidationWarnings(true);
                }
                OnSessionModeAction.Invoke(SessionMode);
            }
        }

        private void InitializeSessionModeActions()
        {
            SessionModeActions = new Dictionary<SessionModes, StartSessionModeDelegateHandler>();
            SessionModeActions.Add(SessionModes.Client, NetworkManager.Singleton.StartClient);
            SessionModeActions.Add(SessionModes.Host, NetworkManager.Singleton.StartHost);
            SessionModeActions.Add(SessionModes.Server, NetworkManager.Singleton.StartServer);
        }
    }
}