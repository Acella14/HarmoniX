using UnityEngine;
using System.Collections;

public class SpectatorCameraController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float lookSpeed = 2f;

    private float yaw = 0f;
    private float pitch = 0f;

    public GameObject originalPlayer;
    public KeyCode switchKey = KeyCode.Return;

    public float waitTime = 5f; // Time in seconds to wait before switching to the player

    private bool hasSwitched = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StartCoroutine(SwitchToPlayerAfterDelay());
    }

    void Update()
    {
        if (hasSwitched)
            return;

        // Movement controls
        float moveForwardBackward = Input.GetAxis("Vertical");
        float moveLeftRight = Input.GetAxis("Horizontal");

        Vector3 movement = new Vector3(moveLeftRight, 0, moveForwardBackward);
        movement = transform.TransformDirection(movement);
        transform.position += movement * moveSpeed * Time.deltaTime;

        // Mouse look controls
        yaw += lookSpeed * Input.GetAxis("Mouse X");
        pitch += lookSpeed * Input.GetAxis("Mouse Y");
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        transform.eulerAngles = new Vector3(pitch, yaw, 0f);

        if (Input.GetKeyDown(switchKey))
        {
            StopAllCoroutines();
            SwitchToOriginalPlayer();
        }
    }

    IEnumerator SwitchToPlayerAfterDelay()
    {
        yield return new WaitForSeconds(waitTime);
        SwitchToOriginalPlayer();
    }

    void SwitchToOriginalPlayer()
    {
        if (hasSwitched)
            return;

        hasSwitched = true;

        // Enable the original player
        if (originalPlayer != null)
        {
            originalPlayer.SetActive(true);

            // Deactivate the spectator camera
            gameObject.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Debug.LogError("Original player is not assigned in SpectatorCameraController.");
        }
    }
}
