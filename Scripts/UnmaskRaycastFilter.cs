using System;
using UnityEngine;

namespace Oultrox.UIExtensions
{
    [AddComponentMenu("UI/Unmask/UnmaskRaycastFilter", 2)]
    public class UnmaskRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        //################################
        // Serialize Members.
        //################################
        [Tooltip("Target unmask components. The ray passes through the unmasked rectangles.")]
        [SerializeField] private Unmask[] m_TargetUnmasks = new Unmask[0];
        bool _hasProcessedTouch;
        Unmask _lastTouchedUnmask;
        Action _processedTouchEvent;
        
        
        //################################
        // Public Members.
        //################################
        /// <summary>
        /// Target unmask components. Ray through the unmasked rectangles.
        /// </summary>
        public Unmask[] targetUnmasks { get { return m_TargetUnmasks; } set { m_TargetUnmasks = value; } }

        
        /// <summary>
        /// Assign Callback touch event for any creative means. This will get invoked once per unmask touch.
        /// </summary>
        /// <param name="callback">Invoke Callback.</param>
        public void AssignTouchUnmaskCallback(Action callback)
        {
            _processedTouchEvent = callback;
        }
        
        /// <summary>
        /// Clear all touch unmask once callbacks.
        /// </summary>
        public void ClearTouchUnmaskCallback()
        {
            _processedTouchEvent = null;
        }

        /// <summary>
        /// Given a point and a camera, is the raycast valid.
        /// </summary>
        /// <returns>Valid.</returns>
        /// <param name="sp">Screen position.</param>
        /// <param name="eventCamera">Raycast camera.</param>
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (!isActiveAndEnabled || m_TargetUnmasks == null || m_TargetUnmasks.Length == 0)
            {
                // Reset the flag when not processing raycast
                _hasProcessedTouch = false;
                return true;
            }

            foreach (Unmask targetUnmask in m_TargetUnmasks)
            {
                if (targetUnmask && targetUnmask.isActiveAndEnabled)
                {
                    if (eventCamera)
                    {
                        if (RectTransformUtility.RectangleContainsScreenPoint((targetUnmask.transform as RectTransform), sp, eventCamera))
                        {
                            // Trigger the event only if it hasn't been processed in the current frame
                            if (!_hasProcessedTouch || _lastTouchedUnmask != targetUnmask)
                            {
                                _hasProcessedTouch = true;
                                _processedTouchEvent?.Invoke();
                            }

                            // Update the last touched Unmask
                            _lastTouchedUnmask = targetUnmask;

                            return false; // Raycast is not valid if inside any of the Unmask components.
                        }
                    }
                    else
                    {
                        if (RectTransformUtility.RectangleContainsScreenPoint((targetUnmask.transform as RectTransform), sp))
                        {
                            // Trigger the event only if it hasn't been processed in the current frame
                            if (!_hasProcessedTouch || _lastTouchedUnmask != targetUnmask)
                            {
                                _hasProcessedTouch = true;
                                _processedTouchEvent?.Invoke();
                            }

                            // Update the last touched Unmask
                            _lastTouchedUnmask = targetUnmask;

                            return false; // Raycast is not valid if inside any of the Unmask components.
                        }
                    }
                }
            }

            // Reset the flag when not inside any Unmask component
            _hasProcessedTouch = false;

            // Clear the last touched Unmask when not inside any Unmask component
            _lastTouchedUnmask = null;

            return true; // Raycast is valid if not inside any of the Unmask components.
        }
        
        //################################
        // Private Members.
        //################################
        void OnEnable()
        {
            // Reset the flag when the object becomes enabled
            _hasProcessedTouch = false;
        }

        void OnDisable()
        {
            ClearTouchUnmaskCallback();
        }
    }
}
