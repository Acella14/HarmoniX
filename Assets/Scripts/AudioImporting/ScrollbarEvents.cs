using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollbarEvents : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public AudioImporter audioImporter;
    private Scrollbar scrollbar;
    private bool isDragging;

    void Start()
    {
        scrollbar = GetComponent<Scrollbar>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        audioImporter.GetCustomScrollRect().StopInertia();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        audioImporter.OnEndDrag();
    }
}
