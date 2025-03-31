using UnityEngine;

public class CDOptionsClickHandler : MonoBehaviour
{
    private CDOptionsCarousel carousel; // Reference to the carousel script

    void Start()
    {
        carousel = GetComponent<CDOptionsCarousel>(); // Get the carousel script
        if (carousel == null)
        {
            Debug.LogError("CDOptionsClickHandler: Could not find CDOptionsCarousel on this GameObject.");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Detect left mouse click
        {
            DetectSideButtonClick();
        }
    }

    void DetectSideButtonClick()
    {
        if (carousel == null || carousel.optionButtons.Count < 3) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {

            int clickedIndex = carousel.optionButtons.FindIndex(btn => 
                btn.clickCollider != null && btn.clickCollider.gameObject == hit.collider.gameObject
            );

            if (clickedIndex != -1 && clickedIndex != carousel.centerIndex) // Ignore center button
            {
                carousel.ShiftButtons(clickedIndex); // Pass the clicked index instead of a bool
            }
        }
    }


}
