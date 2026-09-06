using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// ================= 运行时 UGUI 根（代码生成，无需在编辑器里搭 Canvas） =================
// ScreenSpaceOverlay + 1920x1080 参考分辨率 CanvasScaler，解决旧 IMGUI 在不同分辨率被拉伸发糊的问题。
// 本工程 activeInputHandler=1(仅新输入系统)，因此 EventSystem 必须配 InputSystemUIInputModule。
public class UIRoot : MonoBehaviour
{
    public static UIRoot I;
    RectTransform _root;
    public RectTransform Root { get { return _root; } }
    public Canvas Canvas { get; private set; }

    public static UIRoot Ensure()
    {
        if (I != null) return I;
        var go = new GameObject("[UIRoot]");
        DontDestroyOnLoad(go);
        I = go.AddComponent<UIRoot>();
        I.Build();
        return I;
    }

    void Build()
    {
        Canvas = gameObject.AddComponent<Canvas>();
        Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Canvas.sortingOrder = 1000; // 压在游戏与 OnGUI 之上

        var sc = gameObject.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        sc.matchWidthOrHeight = 0.5f;
        sc.referencePixelsPerUnit = 100;

        gameObject.AddComponent<GraphicRaycaster>();
        _root = gameObject.GetComponent<RectTransform>();
        _root.anchorMin = Vector2.zero; _root.anchorMax = Vector2.one;
        _root.offsetMin = Vector2.zero; _root.offsetMax = Vector2.zero;

        EnsureEventSystem();
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        DontDestroyOnLoad(es);
        es.AddComponent<EventSystem>();
        var input = es.AddComponent<InputSystemUIInputModule>();
        try { input.AssignDefaultActions(); } catch { /* 版本差异兜底：不阻断 */ }
    }
}
