using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Movement")]
    public Transform textTransform;
    public float hoverDistance = 50f;
    public float moveSpeed = 10f;
    public bool goUpDownInsteadOfLeftRight;

    [Header("Click")]
    public bool canClick = true;

    private Vector3 originalPos;
    private bool hovering;

    void Start()
    {
        if (textTransform == null && transform.childCount > 0)
            textTransform = transform.GetChild(0);

        if (textTransform != null)
            originalPos = textTransform.localPosition;
    }

    void OnEnable()
    {
        ResetHoverState();
    }

    void OnDisable()
    {
        ResetHoverState();
    }

    void Update()
    {
        if (textTransform == null) return;

        Vector3 dir = goUpDownInsteadOfLeftRight ? Vector3.up : Vector3.right;
        Vector3 target = hovering ? originalPos + dir * hoverDistance : originalPos;

        textTransform.localPosition = Vector3.Lerp(
            textTransform.localPosition,
            target,
            Time.unscaledDeltaTime * moveSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!canClick) return;

        hovering = true;

        if (MenuAudioManager.Instance != null)
            MenuAudioManager.Instance.PlayHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!canClick) return;

        if (MenuAudioManager.Instance != null)
            MenuAudioManager.Instance.PlayClick();
    }

    public void ResetHoverState()
    {
        hovering = false;

        if (textTransform != null)
            textTransform.localPosition = originalPos;
    }

    public void SetHoverEnabled(bool enabled)
    {
        canClick = enabled;
        hovering = false;
        this.enabled = true;
    }
}