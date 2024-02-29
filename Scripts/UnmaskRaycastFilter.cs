using UnityEngine;

namespace Oultrox.UIExtensions
{
    /// <summary>
    /// Unmask Raycast Filter.
    /// The ray passes through the unmasked rectangle. Based on Coffee.UnMask solution.
    /// </summary>
    [AddComponentMenu("UI/Unmask/UnmaskRaycastFilter", 2)]
    public class UnmaskRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        //################################
        // Serialize Members.
        //################################
        [Tooltip("Target unmask components. The ray passes through the unmasked rectangles.")]
        [SerializeField] private Unmask[] m_TargetUnmasks = new Unmask[0];


        //################################
        // Public Members.
        //################################
        /// <summary>
        /// Target unmask components. Ray through the unmasked rectangles.
        /// </summary>
        public Unmask[] targetUnmasks { get { return m_TargetUnmasks; } set { m_TargetUnmasks = value; } }

        /// <summary>
        /// Given a point and a camera, is the raycast valid.
        /// </summary>
        /// <returns>Valid.</returns>
        /// <param name="sp">Screen position.</param>
        /// <param name="eventCamera">Raycast camera.</param>
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            // Skip if deactivated or no target unmask components.
            if (!isActiveAndEnabled || m_TargetUnmasks == null || m_TargetUnmasks.Length == 0)
            {
                return true;
            }

            // Check inside for each Unmask component in the array.
            foreach (Unmask targetUnmask in m_TargetUnmasks)
            {
                if (targetUnmask && targetUnmask.isActiveAndEnabled)
                {
                    if (eventCamera)
                    {
                        if (RectTransformUtility.RectangleContainsScreenPoint((targetUnmask.transform as RectTransform), sp, eventCamera))
                        {
                            return false; // Raycast is not valid if inside any of the Unmask components.
                        }
                    }
                    else
                    {
                        if (RectTransformUtility.RectangleContainsScreenPoint((targetUnmask.transform as RectTransform), sp))
                        {
                            return false; // Raycast is not valid if inside any of the Unmask components.
                        }
                    }
                }
            }

            return true; // Raycast is valid if not inside any of the Unmask components.
        }


        //################################
        // Private Members.
        //################################

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        void OnEnable()
        {
            // Initialization code if needed.
        }
    }
}
