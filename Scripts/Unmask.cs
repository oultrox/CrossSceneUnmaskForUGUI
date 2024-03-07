using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;


namespace Oultrox.UIExtensions
{
    /// <summary>
    /// Reverse masking for parent Mask component. Based on Coffee.UnMask solution.
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("UI/Unmask/Unmask", 1)]
    public class Unmask : MonoBehaviour, IMaterialModifier
    {
        //################################
        // Constant or Static Members.
        //################################
        private static readonly Vector2 s_Center = new Vector2(0.5f, 0.5f);
        private Canvas m_OwnCanvas;

        //################################
        // Serialize Members.
        //################################
        [Header("Target Fit (It's one or another)")]
        [Tooltip("Fit graphic's transform to target transform. Remember: it's either transform OR string name.")]
        [SerializeField] private RectTransform m_FitTarget;

        [FormerlySerializedAs("m_FitCanvasName")]
        [Header("Fit Target Via Cross Scene Feature  (It's one or another)")]
        [Tooltip("Name of the (UNIQUE) GameObject to fit the graphic's transform to. Remember: it's either transform OR string name.")]
        [SerializeField] private string m_FitTargetCanvasName;
        
        [Tooltip("Name of the (UNIQUE) GameObject to fit the graphic's transform to. Remember: it's either transform OR string name.")]
        [SerializeField] private string m_FitTargetName;
        
        [Header("Fit Type")]
        [SerializeField] private bool m_FitPosition = true;
        [SerializeField] private bool m_FitSize = true;
        
        [Header("Offset Values")]
        [SerializeField] private Vector2 sizeOffset = Vector2.zero;
        [SerializeField] private Vector2 positionOffset = Vector2.zero;

        [Header("Standard Properties")]
        [Tooltip("Fit graphic's transform to target transform on LateUpdate every frame.")]
        [SerializeField] private bool m_FitOnLateUpdate;

        [Tooltip("Unmask affects only for children.")]
        [SerializeField] private bool m_OnlyForChildren = false;

        [Tooltip("Show the graphic that is associated with the unmask render area.")]
        [SerializeField] private bool m_ShowUnmaskGraphic = false;

        [Tooltip("Edge smoothing.")]
        [Range(0f, 1f)]
        [SerializeField] private float m_EdgeSmoothing = 0f;

        [Tooltip("Shows console logs for targets.")]
        [SerializeField] private bool m_isDebugMode = false;



        //################################
        // Public Members.
        //################################
        /// <summary>
        /// The graphic associated with the unmask.
        /// </summary>
        public MaskableGraphic graphic { get { return _graphic ?? (_graphic = GetComponent<MaskableGraphic>()); } }

        /// <summary>
        /// Fit graphic's transform to target transform.
        /// </summary>
        public RectTransform fitTarget
        {
            get { return m_FitTarget; }
            set
            {
                m_FitTarget = value;
                FitTo(m_FitTarget);
            }
        }
        
        /// <summary>
        /// Name of the object to fit the graphic's transform to.
        /// </summary>
        public string fitTargetName
        {
            get { return m_FitTargetName; }
            set
            {
                m_FitTargetName = value;
                if (!string.IsNullOrEmpty(m_FitTargetName) && !string.IsNullOrEmpty(m_FitTargetCanvasName))
                {
                    FitTo(m_FitTargetCanvasName,m_FitTargetName);
                }
            }
        }
        
        /// <summary>
        /// Name of the object to fit the graphic's transform to.
        /// </summary>
        public string FitTargetTargetCanvasName
        {
            get { return m_FitTargetCanvasName; }
            set
            {
                m_FitTargetName = value;
                if (!string.IsNullOrEmpty(m_FitTargetName) && !string.IsNullOrEmpty(m_FitTargetCanvasName))
                {
                    FitTo(m_FitTargetCanvasName,m_FitTargetName);
                }
            }
        }

        /// <summary>
        /// Fit graphic's transform to target transform on LateUpdate every frame.
        /// </summary>
        public bool fitOnLateUpdate { get { return m_FitOnLateUpdate; } set { m_FitOnLateUpdate = value; } }

        /// <summary>
        /// Show the graphic that is associated with the unmask render area.
        /// </summary>
        public bool showUnmaskGraphic
        {
            get { return m_ShowUnmaskGraphic; }
            set
            {
                m_ShowUnmaskGraphic = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Unmask affects only for children.
        /// </summary>
        public bool onlyForChildren
        {
            get { return m_OnlyForChildren; }
            set
            {
                m_OnlyForChildren = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Edge smooting.
        /// </summary>
        public float edgeSmoothing
        {
            get { return m_EdgeSmoothing; }
            set { m_EdgeSmoothing = value; }
        }

        /// <summary>
        /// Perform material modification in this function.
        /// </summary>
        /// <returns>Modified material.</returns>
        /// <param name="baseMaterial">Configured Material.</param>
        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled)
            {
                return baseMaterial;
            }

            Transform stopAfter = MaskUtilities.FindRootSortOverrideCanvas(transform);
            var stencilDepth = MaskUtilities.GetStencilDepth(transform, stopAfter);
            var desiredStencilBit = 1 << stencilDepth;

            StencilMaterial.Remove(_unmaskMaterial);
            _unmaskMaterial = StencilMaterial.Add(baseMaterial, desiredStencilBit - 1, StencilOp.Invert, CompareFunction.Equal, m_ShowUnmaskGraphic ? ColorWriteMask.All : (ColorWriteMask)0, desiredStencilBit - 1, (1 << 8) - 1);

            // Unmask affects only for children.
            var canvasRenderer = graphic.canvasRenderer;
            if (m_OnlyForChildren)
            {
                StencilMaterial.Remove(_revertUnmaskMaterial);
                _revertUnmaskMaterial = StencilMaterial.Add(baseMaterial, (1 << 7), StencilOp.Invert, CompareFunction.Equal, (ColorWriteMask)0, (1 << 7), (1 << 8) - 1);
                canvasRenderer.hasPopInstruction = true;
                canvasRenderer.popMaterialCount = 1;
                canvasRenderer.SetPopMaterial(_revertUnmaskMaterial, 0);
            }
            else
            {
                canvasRenderer.hasPopInstruction = false;
                canvasRenderer.popMaterialCount = 0;
            }

            return _unmaskMaterial;
        }

        /// <summary>
        /// Fit to target transform.
        /// </summary>
        /// <param name="target">Target transform.</param>
        public void FitTo(RectTransform target)
        {
            var rt = transform as RectTransform;

            if (m_FitPosition)
            {
                rt.pivot = target.pivot;
                rt.position = target.position;
                rt.rotation = target.rotation;
            }

            if (m_FitSize)
            {
                var s1 = target.lossyScale;
                var s2 = rt.parent.lossyScale;
                rt.localScale = new Vector3(s1.x / s2.x, s1.y / s2.y, s1.z / s2.z);
                rt.sizeDelta = target.rect.size;
                rt.anchorMax = rt.anchorMin = s_Center;
            }
        }
        
        /// <summary>
        /// Fit to target transform.
        /// </summary>
        /// <param name="canvasName">Name of the Canvas where the target object lives.</param>
        /// <param name="targetName">Name of the target object which can be inside the target.</param>
        public void FitTo(string canvasName, string targetName)
        {
            if (m_FitTarget != null)
            {
                if(!m_isDebugMode) return;
                Debug.LogWarning("FitTarget already set.");
                return;
            }

            RectTransform rt = transform as RectTransform;
            GameObject canvasObject = GameObject.Find(canvasName);
            RectTransform target = GameObject.Find(targetName)?.GetComponent<RectTransform>();

            if (canvasObject == null)
            {
                if(!m_isDebugMode) return;
                Debug.LogWarning($"Canvas with name {canvasName} not found.");
                return;
            }

            if (target == null)
            {
                if(!m_isDebugMode) return;
                Debug.LogWarning($"Target object with name {targetName} not found.");
                return;
            }

            if (m_OwnCanvas == null)
            {
                if(!m_isDebugMode) return;
                Debug.LogWarning($"Own Canvas object not found.");
                return;
            }

            // Get the canvases
            Canvas targetCanvas = canvasObject.GetComponent<Canvas>();

            // Check if the target and current canvases exist
            if (targetCanvas == null || m_OwnCanvas == null || rt == null)
            {
                if(!m_isDebugMode) return;
                Debug.LogWarning("Target or current Canvas not found.");
                return;
            }

            ConvertRelativePosition(targetCanvas, target, rt);
            ConvertRelativeDimensionSize(targetCanvas, rt, target);
        }
        
        
        //################################
        // Private Members.
        //################################
        private Material _unmaskMaterial;
        private Material _revertUnmaskMaterial;
        private MaskableGraphic _graphic;
        
        private void ConvertRelativePosition(Canvas targetCanvas, RectTransform target, RectTransform rt)
        {
            if (!m_FitPosition) return;

            // Convert target position to local canvas space
            Vector2 localPosition = RectTransformUtility.WorldToScreenPoint(targetCanvas.worldCamera, target.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_OwnCanvas.transform as RectTransform, localPosition,
                m_OwnCanvas.worldCamera, out localPosition);

            // Apply adjustments to Unmask RectTransform
            rt.localPosition = localPosition;
            rt.rotation = target.rotation;
        }
        
        private void ConvertRelativeDimensionSize(Canvas targetCanvas, RectTransform rt, RectTransform target)
        {
            if (!m_FitSize) return;

            float ownCanvasScaleFactor = m_OwnCanvas.GetComponent<CanvasScaler>().scaleFactor;
            float targetCanvasScaleFactor = targetCanvas.GetComponent<CanvasScaler>().scaleFactor;

            // Calculate relative scale factors
            float relativeScaleX = targetCanvasScaleFactor / ownCanvasScaleFactor;
            float relativeScaleY = targetCanvasScaleFactor / ownCanvasScaleFactor;

            // Apply relative scale adjustments
            rt.localScale = new Vector3(
                target.localScale.x * relativeScaleX,
                target.localScale.y * relativeScaleY,
                target.localScale.z
            );

            // Calculate the sizeDelta based on the target's rect and the relative scale factors
            Vector2 targetSizeDelta = target.rect.size;

            // For both Overlay and Camera modes, convert target position to screen space and then to local canvas space
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(targetCanvas.worldCamera, target.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_OwnCanvas.transform as RectTransform, screenPos, m_OwnCanvas.worldCamera, out Vector2 localPos);

            // Set sizeDelta based on the target's rect and the relative scale factors
            rt.sizeDelta = new Vector2(
                targetSizeDelta.x / (ownCanvasScaleFactor / targetCanvasScaleFactor),
                targetSizeDelta.y / (ownCanvasScaleFactor / targetCanvasScaleFactor)
            );

            rt.localPosition = localPos;

            rt.sizeDelta += sizeOffset; // Apply size offset
            rt.anchorMax = rt.anchorMin = target.pivot;
        }



        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        private void OnEnable()
        { 
            // Search for the nearest Canvas component in the parent hierarchy
            m_OwnCanvas = transform.GetComponentInParent<Canvas>();
            if (m_FitTarget)
            {
                FitTo(m_FitTarget);
            }
            else if (!string.IsNullOrEmpty(m_FitTargetName))
            {
                FitTo(m_FitTargetCanvasName,m_FitTargetName);
            }
            SetDirty();
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled () or inactive.
        /// </summary>
        private void OnDisable()
        {
            StencilMaterial.Remove(_unmaskMaterial);
            StencilMaterial.Remove(_revertUnmaskMaterial);
            _unmaskMaterial = null;
            _revertUnmaskMaterial = null;

            if (graphic)
            {
                var canvasRenderer = graphic.canvasRenderer;
                canvasRenderer.hasPopInstruction = false;
                canvasRenderer.popMaterialCount = 0;
                graphic.SetMaterialDirty();
            }
            SetDirty();
        }

        /// <summary>
        /// LateUpdate is called every frame, if the Behaviour is enabled.
        /// </summary>
        private void LateUpdate()
        {
#if UNITY_EDITOR
            if ((m_FitTarget || (!string.IsNullOrEmpty(m_FitTargetName) && !string.IsNullOrEmpty(m_FitTargetCanvasName))) && (m_FitOnLateUpdate || !Application.isPlaying))
#else
			if ((m_FitTarget || !string.IsNullOrEmpty(m_FitTargetName)) && m_FitOnLateUpdate)
#endif
            {
                if (m_FitTarget)
                {
                    FitTo(m_FitTarget);
                }
                else if (!string.IsNullOrEmpty(m_FitTargetName) && !string.IsNullOrEmpty(m_FitTargetCanvasName))
                {
                    FitTo(m_FitTargetCanvasName,m_FitTargetName);
                }
            }
            Smoothing(graphic, m_EdgeSmoothing);
        }

#if UNITY_EDITOR
        /// <summary>
        /// This function is called when the script is loaded or a value is changed in the inspector (Called in the editor only).
        /// </summary>
        private void OnValidate()
        {
            SetDirty();
        }
#endif

        /// <summary>
        /// Mark the graphic as dirty.
        /// </summary>
        void SetDirty()
        {
            if (graphic)
            {
                graphic.SetMaterialDirty();
            }
        }

        private static void Smoothing(MaskableGraphic graphic, float smooth)
        {
            if (!graphic) return;

            Profiler.BeginSample("[Unmask] Smoothing");
            var canvasRenderer = graphic.canvasRenderer;
            var currentColor = canvasRenderer.GetColor();
            var targetAlpha = 1f;
            if (graphic.maskable && 0 < smooth)
            {
                var currentAlpha = graphic.color.a * canvasRenderer.GetInheritedAlpha();
                if (0 < currentAlpha)
                {
                    targetAlpha = Mathf.Lerp(0.01f, 0.002f, smooth) / currentAlpha;
                }
            }

            if (!Mathf.Approximately(currentColor.a, targetAlpha))
            {
                currentColor.a = Mathf.Clamp01(targetAlpha);
                canvasRenderer.SetColor(currentColor);
            }

            Profiler.EndSample();
        }
    }
}
