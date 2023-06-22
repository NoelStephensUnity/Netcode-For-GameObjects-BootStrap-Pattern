using UnityEngine;
using Unity.Netcode.Components;

namespace NetcodeForGameObjects.SceneManagement.GoldenPath
{
    public class PlayerMotion : NetworkTransform
    {
        [Range(1.0f, 20.0f)]
        public float Radius = 10.0f;

        [Range(1.0f, 30.0f)]
        public float Speed = 5.0f;

        [Tooltip("When set to true the player uses owner authoritative transform updates.")]
        public bool OwnerAuthoritative;

        private float m_CurrentPi;
        private float m_Increment = 0.25f;
        private float m_ClockWise = 1.0f;
        private Rigidbody m_RigidBody;

        protected override bool OnIsServerAuthoritative()
        {
            return OwnerAuthoritative;
        }

        public override void OnNetworkSpawn()
        {
            // Always invoked base when deriving from NetworkTransform
            base.OnNetworkSpawn();

            m_RigidBody = GetComponent<Rigidbody>();
            if (CanCommitToTransform)
            {
                m_CurrentPi = Random.Range(-Mathf.PI, Mathf.PI);
                m_ClockWise = Random.Range(-1.0f, 1.0f);
                m_ClockWise = m_ClockWise / Mathf.Abs(m_ClockWise);
                if (!IsOwner)
                {
                    Radius += Random.Range(-2.0f, 2.0f);
                }
            }
        }

        private void FixedUpdate()
        {
            if (IsSpawned && CanCommitToTransform)
            {
                m_CurrentPi += m_ClockWise * (Speed * m_Increment * Time.fixedDeltaTime);
                var offset = new Vector3(Radius * Mathf.Cos(m_CurrentPi), transform.position.y, Radius * Mathf.Sin(m_CurrentPi));
                m_RigidBody.MovePosition(Vector3.Lerp(transform.position, offset, Speed * 0.1f * Time.fixedDeltaTime));
            }
        }
    }
}