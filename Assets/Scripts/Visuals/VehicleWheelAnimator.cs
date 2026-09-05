using UnityEngine;

namespace TrafficTown2D.Visuals
{
    public sealed class VehicleWheelAnimator : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D observedBody;
        [SerializeField] private Transform frontWheel;
        [SerializeField] private Transform rearWheel;
        [SerializeField, Min(0f)] private float degreesPerUnit = 420f;

        private void Awake()
        {
            if (observedBody == null) observedBody = GetComponent<Rigidbody2D>();
            if (frontWheel == null) frontWheel = FindChild("FrontWheel");
            if (rearWheel == null) rearWheel = FindChild("RearWheel");
        }

        private void Update()
        {
            if (observedBody == null)
            {
                return;
            }

            float rotation = -observedBody.linearVelocity.x * degreesPerUnit * Time.deltaTime;
            if (frontWheel != null) frontWheel.Rotate(0f, 0f, rotation, Space.Self);
            if (rearWheel != null) rearWheel.Rotate(0f, 0f, rotation, Space.Self);
        }

        private Transform FindChild(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < children.Length; index++)
            {
                if (children[index].name == childName)
                {
                    return children[index];
                }
            }

            return null;
        }
    }
}
