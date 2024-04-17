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
    CanvasScaler m_OwnCanvasScaler;

    //################################
    // Serialize Members.
    //################################
    
    [FormerlySerializedAs("m_FitTarget")]
    [Header("Same Scene Canvas Target Fit (Choose 1 Fit style)")]
    [Tooltip("Name of the (UNIQUE) GameObject to fit the graphic's transform to.")]
    [SerializeField]
    private string m_SameSceneFitTargetName;
    
    [Header("Fit Canvas Target Via Cross Scene Feature (Choose 1 Fit style)")]
    [Tooltip(
        "Name of the (UNIQUE) GameObject's Canvas to fit the graphic's transform to.")]
    [SerializeField]
    private string m_FitTargetCanvasName;

    [Tooltip(
        "Name of the (UNIQUE) GameObject to fit the graphic's transform to.")]
    [SerializeField]
    private string m_FitTargetName;
    
    [Header("Fit Non-Canvas Target Via Cross Scene Feature (Choose 1 Fit style)")]
    [Tooltip(
        "Name of the (UNIQUE) 3D GameObject to fit the graphic's transform to.")]
    [SerializeField]
    string m_FitNonCanvasTargetName;
    
    [Tooltip(
        "Name of the (UNIQUE) Camera to fit the graphic's transform to.")]
    [SerializeField] string m_FitNonCanvasTargetCameraName;

    [Header("Fit Type")] 
    [SerializeField] private bool m_FitPosition = true;
    [SerializeField] private bool m_FitSize = true;

    [Header("Offset Values")] [SerializeField]
    private Vector2 sizeOffset = Vector2.zero;

    [SerializeField] private Vector2 positionOffset = Vector2.zero;

    [Header("Standard Properties")]
    [Tooltip("Fit graphic's transform to target transform on LateUpdate every frame.")]
    [SerializeField]
    private bool m_FitOnLateUpdate;

    [Tooltip("Unmask affects only for children.")] [SerializeField]
    private bool m_OnlyForChildren = false;

    [Tooltip("Show the graphic that is associated with the unmask render area.")] [SerializeField]
    private bool m_ShowUnmaskGraphic = false;

    [Tooltip("Edge smoothing.")] [Range(0f, 1f)] [SerializeField]
    private float m_EdgeSmoothing = 0f;

    [Tooltip("Shows console logs for targets.")] [SerializeField]
    private bool m_isDebugMode = false;



    //################################
    // Public Members.
    //################################
    /// <summary>
    /// The graphic associated with the unmask.
    /// </summary>
    public MaskableGraphic graphic
    {
        get { return _graphic ?? (_graphic = GetComponent<MaskableGraphic>()); }
    }

    /// <summary>
    /// Fit graphic's transform to target transform.
    /// </summary>
    public string SameSceneFitTargetName
    {
        get { return m_SameSceneFitTargetName; }
        set
        {
            m_SameSceneFitTargetName = value;
            FitTo(m_SameSceneFitTargetName);
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
                FitTo(m_FitTargetCanvasName, m_FitTargetName);
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
                FitTo(m_FitTargetCanvasName, m_FitTargetName);
            }
        }
    }

    /// <summary>
    /// Fit graphic's transform to target transform on LateUpdate every frame.
    /// </summary>
    public bool fitOnLateUpdate
    {
        get { return m_FitOnLateUpdate; }
        set { m_FitOnLateUpdate = value; }
    }

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
    /// Edge smoothing.
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
        _unmaskMaterial = StencilMaterial.Add(baseMaterial, desiredStencilBit - 1, StencilOp.Invert,
            CompareFunction.Equal, m_ShowUnmaskGraphic ? ColorWriteMask.All : (ColorWriteMask)0, desiredStencilBit - 1,
            (1 << 8) - 1);

        // Unmask affects only for children.
        var canvasRenderer = graphic.canvasRenderer;
        if (m_OnlyForChildren)
        {
            StencilMaterial.Remove(_revertUnmaskMaterial);
            _revertUnmaskMaterial = StencilMaterial.Add(baseMaterial, (1 << 7), StencilOp.Invert, CompareFunction.Equal,
                (ColorWriteMask)0, (1 << 7), (1 << 8) - 1);
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
    /// Fit to target transform same scene.
    /// </summary>
    /// <param name="targetName">Target transform.</param>
    public void FitTo(string targetName)
    {
        if ((!string.IsNullOrEmpty(m_FitTargetName) && !string.IsNullOrEmpty(m_FitTargetCanvasName)) ||
            (!string.IsNullOrEmpty(m_FitNonCanvasTargetName) && !string.IsNullOrEmpty(m_FitNonCanvasTargetCameraName)))
        {
            if (!m_isDebugMode) return;
            Debug.LogWarning("Another Fit already set.");
            return;
        }
        
        m_OwnCanvasScaler.matchWidthOrHeight = 0.5f;
        
        var rt = transform as RectTransform;
        var anchorsAndPivotVector = new Vector2(0.5f, 0.5f);
        rt.anchorMax = anchorsAndPivotVector;
        rt.anchorMin = anchorsAndPivotVector;
        rt.pivot = anchorsAndPivotVector;
        
        RectTransform target = GameObject.Find(targetName)?.GetComponent<RectTransform>();
        
        if (target == null)
        {
            if (!m_isDebugMode) return;
            Debug.LogWarning($"Target object with name {targetName} not found.");
            return;
        }
        
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
        if (!string.IsNullOrEmpty(m_SameSceneFitTargetName) || (!string.IsNullOrEmpty(m_FitNonCanvasTargetName) && !string.IsNullOrEmpty(m_FitNonCanvasTargetCameraName)))
        {
            if (!m_isDebugMode) return;
            Debug.LogWarning("Another Fit already set.");
            return;
        }
        
        m_OwnCanvasScaler.matchWidthOrHeight = 0.5f;

        RectTransform rt = transform as RectTransform;
        var anchorsAndPivotVector = new Vector2(0.5f, 0.5f);
        rt.anchorMax = anchorsAndPivotVector;
        rt.anchorMin = anchorsAndPivotVector;
        rt.pivot = anchorsAndPivotVector;
        
        GameObject canvasObject = GameObject.Find(canvasName);
        RectTransform target = GameObject.Find(targetName)?.GetComponent<RectTransform>();

        if (canvasObject == null)
        {
            if (!m_isDebugMode) return;
            Debug.LogWarning($"Canvas with name {canvasName} not found.");
            return;
        }

        if (target == null)
        {
            if (!m_isDebugMode) return;
            Debug.LogWarning($"Target object with name {targetName} not found.");
            return;
        }

        if (m_OwnCanvas == null)
        {
            if (!m_isDebugMode) return;
            Debug.LogWarning($"Own Canvas object not found.");
            return;
        }

        // Get the canvases
        Canvas targetCanvas = canvasObject.GetComponent<Canvas>();

        // Check if the target and current canvases exist
        if (targetCanvas == null || m_OwnCanvas == null || rt == null)
        {
            if (!m_isDebugMode) return;
            Debug.LogWarning("Target or current Canvas not found.");
            return;
        }

        ConvertRelativePosition(targetCanvas, target, rt);
        ConvertRelativeDimensionSize(targetCanvas, rt, target);
    }
    
    /// <summary>
    /// Fit Unmask cutout to non-canvas GameObject in another scene
    /// </summary>
    /// <param name="targetGameObjectName"> String name of target Non-canvas GameObject in other scene</param>
    /// <param name="cameraGameObjectName"> String name of Main Camera GameObject in other scene</param>
    public void FitTo3DObject(string targetGameObjectName, string cameraGameObjectName)
    {
        if (!string.IsNullOrEmpty(m_SameSceneFitTargetName) || (!string.IsNullOrEmpty(m_FitTargetName) && !string.IsNullOrEmpty(m_FitTargetCanvasName)))
        {
            if (!m_isDebugMode) return;
            Debug.LogWarning("Another Fit already set.");
            return;
        }
        
        var unmaskRectTransform = transform as RectTransform;
        if (unmaskRectTransform == null) return;
        
        unmaskRectTransform.anchorMax = Vector2.zero;
        unmaskRectTransform.anchorMin = Vector2.zero;
        unmaskRectTransform.pivot = Vector2.zero;

        // Find the transform GameObject and Camera in the other scene
        var transformGameObject = GameObject.Find(targetGameObjectName);
        var targetCamera = GameObject.Find(cameraGameObjectName).GetComponent<Camera>();
        if (transformGameObject == null)
        {
            Debug.LogError("Transform GameObject not found in the other scene.");
            return;
        }

        if (targetCamera == null)
        {
            Debug.LogError("Camera not found in the other scene.");
            return;
        }

        var targetObjectRect = Convert3DObjectToRect(targetCamera, transformGameObject);

        CovertToRelativeSize(targetObjectRect, unmaskRectTransform);
        ConvertRelativePosition(targetObjectRect, unmaskRectTransform);
    }

    //################################
    // Private Members.
    //################################
    private Material _unmaskMaterial;
    private Material _revertUnmaskMaterial;
    private MaskableGraphic _graphic;

    private void ConvertRelativePosition(Canvas targetCanvas, RectTransform target, RectTransform unmaskRectTransform)
    {
        if (!m_FitPosition) return;

        // Convert target position to local canvas space
        Vector2 localPosition = RectTransformUtility.WorldToScreenPoint(targetCanvas.worldCamera, target.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(m_OwnCanvas.transform as RectTransform, localPosition,
            m_OwnCanvas.worldCamera, out localPosition);

        // Apply adjustments to Unmask RectTransform
        unmaskRectTransform.localPosition = localPosition;
        unmaskRectTransform.rotation = target.rotation;
    }

    private void ConvertRelativePosition(Rect target, RectTransform unmaskRectTransform)
    {
        if (!m_FitPosition) return;

        // Convert target position to local canvas space
        var localPosition = target.position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(m_OwnCanvas.transform as RectTransform, localPosition,
            m_OwnCanvas.worldCamera, out localPosition);
        
        // Apply adjustments to Unmask RectTransform
        Vector3 unmaskPosition = localPosition;
        unmaskPosition += (Vector3)positionOffset;
        unmaskRectTransform.localPosition = unmaskPosition;
    }

    private void ConvertRelativeDimensionSize(Canvas targetCanvas, RectTransform unmaskRectTransform,
        RectTransform target)
    {
        if (!m_FitSize) return;

        float ownCanvasScaleFactor = m_OwnCanvas.GetComponent<CanvasScaler>().scaleFactor;
        float targetCanvasScaleFactor = targetCanvas.GetComponent<CanvasScaler>().scaleFactor;

        // Calculate relative scale factors
        float relativeScaleX = targetCanvasScaleFactor / ownCanvasScaleFactor;
        float relativeScaleY = targetCanvasScaleFactor / ownCanvasScaleFactor;

        // Apply relative scale adjustments
        unmaskRectTransform.localScale = new Vector3(
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
        unmaskRectTransform.sizeDelta = new Vector2(
            targetSizeDelta.x / (ownCanvasScaleFactor / targetCanvasScaleFactor),
            targetSizeDelta.y / (ownCanvasScaleFactor / targetCanvasScaleFactor)
        );

        unmaskRectTransform.localPosition = localPos;

        unmaskRectTransform.sizeDelta += sizeOffset; // Apply size offset
        unmaskRectTransform.anchorMax = unmaskRectTransform.anchorMin = target.pivot;
    }
    
    /// <summary>
    /// Using the GameObject and Camera from the other scene calculate a Rect object from the Renderer's bounds
    /// </summary>
    /// <param name="targetCamera">Camera in other scene</param>
    /// <param name="targetObject">GameObject in other scene</param>
    /// <returns>Rect object in screen space</returns>
    static Rect Convert3DObjectToRect(Camera targetCamera, GameObject targetObject)
    {
        var renderer = targetObject.GetComponent<Renderer>();
        var bounds = renderer.bounds;
        var center = bounds.center;
        var extents = bounds.extents;
        
        var extentPoints = GetExtentScreenPoints(targetCamera, center, extents);
        
        var min = extentPoints[0];
        var max = extentPoints[0];
        
        //Find min and max extent points for Rect dimensions
        foreach(var vector in extentPoints)
        {
            min = new Vector2(Mathf.Min(min.x, vector.x), Mathf.Min(min.y, vector.y));
            max = new Vector2(Mathf.Max(max.x, vector.x), Mathf.Max(max.y, vector.y));
        }
        
        return new Rect(min.x, min.y, max.x-min.x, max.y-min.y);
    }
    
    /// <summary>
    /// Transfers the bounds extents from World points to Screen points in a Vector2 array
    /// </summary>
    static Vector2[] GetExtentScreenPoints(Camera targetCamera, Vector3 center, Vector3 extent)
    {
        Vector2[] extentPoints = {
            targetCamera.WorldToScreenPoint(new Vector3(center.x-extent.x, center.y-extent.y, center.z-extent.z)),
            targetCamera.WorldToScreenPoint(new Vector3(center.x+extent.x, center.y-extent.y, center.z-extent.z)),
            targetCamera.WorldToScreenPoint(new Vector3(center.x-extent.x, center.y-extent.y, center.z+extent.z)),
            targetCamera.WorldToScreenPoint(new Vector3(center.x+extent.x, center.y-extent.y, center.z+extent.z)),
            targetCamera.WorldToScreenPoint(new Vector3(center.x-extent.x, center.y+extent.y, center.z-extent.z)),
            targetCamera.WorldToScreenPoint(new Vector3(center.x+extent.x, center.y+extent.y, center.z-extent.z)),
            targetCamera.WorldToScreenPoint(new Vector3(center.x-extent.x, center.y+extent.y, center.z+extent.z)),
            targetCamera.WorldToScreenPoint(new Vector3(center.x+extent.x, center.y+extent.y, center.z+extent.z))
        };
        return extentPoints;
    }
    
    /// <summary>
    /// Adjust target Rect size by canvas scaler to account for different screen sizes and aspect ratios
    /// </summary>
    void CovertToRelativeSize(Rect targetObjectRect, RectTransform unmaskRectTransform)
    {
        var scaleFactor = GetScaleFactor();
        var sizeDelta = new Vector2(targetObjectRect.size.x * scaleFactor, targetObjectRect.size.y * scaleFactor);

        sizeDelta += sizeOffset;
        unmaskRectTransform.sizeDelta = sizeDelta;
    }
    
    /// <summary>
    /// Calculates a scale factor float based off the devices aspect ratio, using its height
    /// </summary>
    float GetScaleFactor()
    {
        m_OwnCanvasScaler.matchWidthOrHeight = 1;
        var referenceSize = m_OwnCanvasScaler.referenceResolution.y;
        var scaleFactor = referenceSize / Screen.height;
        return scaleFactor;
    }


    /// <summary>
    /// This function is called when the object becomes enabled and active.
    /// </summary>
    private void OnEnable()
    {
        // Search for the nearest Canvas component in the parent hierarchy
        m_OwnCanvas = transform.GetComponentInParent<Canvas>();
        m_OwnCanvasScaler = m_OwnCanvas.GetComponent<CanvasScaler>();
        if (!string.IsNullOrEmpty(m_SameSceneFitTargetName))
        {
            FitTo(m_SameSceneFitTargetName);
        }
        else if (!string.IsNullOrEmpty(m_FitTargetName))
        {
            FitTo(m_FitTargetCanvasName, m_FitTargetName);
        }
        else if (!string.IsNullOrEmpty(m_FitNonCanvasTargetName))
        {
            FitTo3DObject(m_FitNonCanvasTargetName, m_FitNonCanvasTargetCameraName);
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
        if ((!string.IsNullOrEmpty(m_SameSceneFitTargetName) || (!string.IsNullOrEmpty(m_FitTargetName) && !string.IsNullOrEmpty(m_FitTargetCanvasName)) || !string.IsNullOrEmpty(m_FitNonCanvasTargetName)) 
            && (m_FitOnLateUpdate || !Application.isPlaying))
#else
			if ((!string.IsNullOrEmpty(m_SameSceneFitTargetName) || !string.IsNullOrEmpty(m_FitTargetName) || !string.IsNullOrEmpty(m_FitNonCanvasTargetName)) && m_FitOnLateUpdate)
#endif
        {
            if (!string.IsNullOrEmpty(m_SameSceneFitTargetName))
            {
                FitTo(m_SameSceneFitTargetName);
            }
            else if (!string.IsNullOrEmpty(m_FitTargetName) && !string.IsNullOrEmpty(m_FitTargetCanvasName))
            {
                FitTo(m_FitTargetCanvasName, m_FitTargetName);
            }
            else if (!string.IsNullOrEmpty(m_FitNonCanvasTargetName))
            {
                FitTo3DObject(m_FitNonCanvasTargetName, m_FitNonCanvasTargetCameraName);
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
