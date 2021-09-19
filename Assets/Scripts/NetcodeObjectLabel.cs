using UnityEngine;
using Unity.Netcode;

namespace NetcodeForGameObjects.SceneManagement.GoldenPath
{
    public class NetcodeObjectLabel : NetworkBehaviour
    {
        private TextMesh m_ObjectLabel;
        private MeshRenderer m_Renderer;
        private bool IsSpawned;

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

        public override void OnNetworkSpawn()
        {
            SetNetCodeLabel(NetworkObject.IsPlayerObject ? $"Player-{NetworkObject.OwnerClientId}"
                : m_ObjectLabel.text = $"NID-{NetworkObject.NetworkObjectId}");
            IsSpawned = true;
        }

        public override void OnNetworkDespawn()
        {
            if (m_ObjectLabel == null)
            {
                m_ObjectLabel = GetComponent<TextMesh>();
            }
            SetNetCodeLabel(string.Empty);
            IsSpawned = false;
        }

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