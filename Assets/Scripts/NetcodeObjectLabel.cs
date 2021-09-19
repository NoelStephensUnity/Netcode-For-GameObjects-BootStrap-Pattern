using UnityEngine;
using Unity.Netcode;

namespace NetcodeForGameObjects.SceneManagement.GoldenPath
{
    public class NetcodeObjectLabel : NetworkBehaviour
    {
        private TextMesh m_ObjectLabel;
        private MeshRenderer m_Renderer;

        private void SetNetCodeLabel(string label)
        {
            if (m_ObjectLabel == null)
            {
                m_ObjectLabel = GetComponent<TextMesh>();
            }
            m_ObjectLabel.text = label;
        }

        private void SetVisible(bool isVisible)
        {
            if (m_Renderer == null)
            {
                m_Renderer = GetComponent<MeshRenderer>();
            }

            if (m_Renderer != null)
            {
                m_Renderer.enabled = true;
            }
        }

        private bool IsVisible() { return m_Renderer ? m_Renderer.enabled : false; }

        private void OnEnable()
        {
            SetVisible(true);
        }

        private void OnDisable()
        {
            SetVisible(false);
        }

        /// <summary>
        /// When we spawn we either set the label to the player id or NetworkObjectId
        /// </summary>
        public override void OnNetworkSpawn()
        {
            SetNetCodeLabel(NetworkObject.IsPlayerObject ? $"Player-{NetworkObject.OwnerClientId}"
                : m_ObjectLabel.text = $"NID-{NetworkObject.NetworkObjectId}");
        }

        /// <summary>
        /// When we despawn, we set the label to nothing
        /// </summary>
        public override void OnNetworkDespawn()
        {
            SetNetCodeLabel(string.Empty);
        }

        /// <summary>
        /// Add the ability to toggle the labels
        /// </summary>
        private void Update()
        {
            if (IsSpawned)
            {
                if (Input.GetKey(KeyCode.L))
                {
                    SetVisible(!IsVisible());
                }
            }
        }
    }
}