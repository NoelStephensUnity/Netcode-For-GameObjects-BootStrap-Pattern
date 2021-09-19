using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Unity.Netcode;

namespace NetcodeForGameObjects.SceneManagement.GoldenPath
{
    [ExecuteInEditMode]
    public class NetcodeButton : NetworkBehaviour
    {
        /// <summary>
        /// See the <see cref="GenericButtonScript"/> for details on this
        /// </summary>
#if UNITY_EDITOR
        Text TextComponent;
        private void Update() { UpdateButtonName(); }
        private void OnValidate() { UpdateButtonName(); }

        private void UpdateButtonName()
        {
            if (TextComponent == null) { TextComponent = GetComponentInChildren<Text>(); }
            if (TextComponent.text != name) { TextComponent.text = name; }
        }
#endif
        public enum NetcodeButtonActionTypes
        {
            ServerAndClient, // Visible to both and can be used by both
            ServerOnly,      // Visible to server and can only be used by server
            ClientOnly,      // Visible to client and can only be used by client
        }
        [Tooltip("What actions will be invoked remotely and/or locally")]
        public NetcodeButtonActionTypes ButtonActionType;
        [Tooltip("When set to true, this will always invoke the OnNetcodeButtonActions registered locally (the default setting)")]
        public bool InvokeButtonActionsLocally = true;
        public UnityEvent OnNetcodeButtonAction;

        private Button ButtonComponent;
        private bool IsNetworkSessionActive() { return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening; }
        private void Awake()
        {
            ButtonComponent = GetComponentInChildren<Button>();
            if (ButtonComponent != null)
            {
                ButtonComponent.onClick.AddListener(new UnityAction(ButtonClicked));
            }
        }

        private bool ButtonIsAvailable()
        {
            if (!IsNetworkSessionActive()) return false;
            return ButtonActionType == NetcodeButtonActionTypes.ServerOnly && IsServer ||
                ButtonActionType == NetcodeButtonActionTypes.ClientOnly && !IsServer ||
                ButtonActionType == NetcodeButtonActionTypes.ServerAndClient;
        }

        public override void OnNetworkSpawn()
        {
            // Default to invisible
            ButtonComponent.gameObject.SetActive(ButtonIsAvailable());
            base.OnNetworkSpawn();
        }

        [ServerRpc(RequireOwnership = false)]
        private void OnButtonClickedServerRpc()
        {
            OnButtonClicked();
        }

        [ClientRpc]
        private void OnButtonClickedClientRpc(ClientRpcParams clientParameters)
        {
            OnButtonClicked();
        }

        private void OnButtonClicked()
        {
            OnNetcodeButtonAction.Invoke();
        }

        private void InvokeButtonClicked()
        {
            if (IsServer == true)
            {
                if (!InvokeButtonActionsLocally)
                {
                    var sendParms = new ClientRpcSendParams() { TargetClientIds = NetworkManager.ConnectedClientsIds.Where(c => c != NetworkManager.LocalClientId).ToArray() };
                    if (sendParms.TargetClientIds.Count() > 0)
                    {
                        OnButtonClickedClientRpc(new ClientRpcParams() { Send = sendParms });
                    }
                }
                else
                {
                    OnButtonClickedClientRpc(default);
                }
            }
            else
            {
                OnButtonClickedServerRpc();
            }
            if(InvokeButtonActionsLocally ||
                ButtonActionType == NetcodeButtonActionTypes.ServerAndClient)
            {
                OnButtonClicked();
            }
        }

        public void ButtonClicked()
        {
            if (!ButtonIsAvailable())
            {
                return;
            }
            InvokeButtonClicked();
        }
    }
}