using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class CanvasFixer : MonoBehaviour
{
    [SerializeField] int sortingOrder = 1000;

    void Awake()
    {
        var canvas = GetComponent<Canvas>();

        // 1) Asegura overlay y display correcto
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = 0; // Display 1 en el Game View
        canvas.sortingOrder = sortingOrder;

        // 2) Si hay CanvasGroup, asegúralo visible y clicable
        var groups = GetComponentsInChildren<CanvasGroup>(true);
        foreach (var g in groups)
        {
            g.alpha = 1f;
            g.interactable = true;
            g.blocksRaycasts = true;
        }

        // 3) Asegura que haya algo que renderizar (alpha > 0)
        var imgs = GetComponentsInChildren<Image>(true);
        foreach (var im in imgs)
        {
            var c = im.color;
            if (c.a < 0.99f) { c.a = 1f; im.color = c; }
            im.enabled = true;
        }

        var texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts) t.enabled = true;

        // 4) Asegura que el panel BackGround tenga Image
        var bg = transform.Find("BackGround");
        if (bg != null && bg.GetComponent<Image>() == null)
        {
            var img = bg.gameObject.AddComponent<Image>();
            var col = new Color(0, 0, 0, 0.5f); // semitransparente para ver que está
            img.color = col;
            var rt = (RectTransform)bg.transform;
            rt.anchorMin = Vector2.zero;  // stretch a toda pantalla
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}