using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace NetcodeForGameObjects.SceneManagement.GoldenPath
{
    public class SessionModeButton : GenericButtonScript
    {
        public NumericField IPAddress;
        public NumericField Port;

        public enum SessionModes
        {
            Client,
            Host,
            Server,
            None
        }
        public delegate bool StartSessionModeDelegateHandler();
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
                    if (!SessionModeActions[SessionMode].Invoke())
                    {
                        Debug.LogWarning($"Failed to start {SessionMode}!");
                        return;
                    }
                    NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(UnityEngine.SceneManagement.LoadSceneMode.Additive);
                    NetworkManager.Singleton.SceneManager.DisableValidationWarnings(true);
                }
                OnSessionModeAction.Invoke(SessionMode);
            }
        }

        private void InitializeSessionModeActions()
        {
            SessionModeActions = new Dictionary<SessionModes, StartSessionModeDelegateHandler>();
            SessionModeActions.Add(SessionModes.Client, StartClient);
            SessionModeActions.Add(SessionModes.Host, StartHost);
            SessionModeActions.Add(SessionModes.Server, StartServer);
        }

        private void SetConnectionInfo()
        {
            var unityTransport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            unityTransport.SetConnectionData(IPAddress.IPAddress, Port.Port, IPAddress.IPAddress);
        }

        private bool StartClient()
        {
            SetConnectionInfo();
            return NetworkManager.Singleton.StartClient();
        }

        private bool StartServer()
        {
            SetConnectionInfo();
            return NetworkManager.Singleton.StartServer();
        }

        private bool StartHost()
        {
            SetConnectionInfo();
            return NetworkManager .Singleton.StartHost();
        }
    }
}