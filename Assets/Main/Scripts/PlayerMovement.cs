using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Transform cam;
    public Rigidbody rb;
    public GameObject jystk;
    public FloatingJoystick joystick;
    public float speed;
    public float yLock;
    public PlayerRelicDetector relicDetector;

    float xIn, zIn;

    private void FixedUpdate()
    {
        if (relicDetector != null && relicDetector.IsInspecting) jystk.SetActive(false);
        else jystk.SetActive(true);


        xIn = joystick.Horizontal * speed;
        zIn = joystick.Vertical * speed;

        Vector3 move = new Vector3(xIn, 0, zIn);
        rb.MovePosition(rb.position + transform.TransformDirection(move) * Time.fixedDeltaTime);

        cam.transform.position = new Vector3(transform.position.x, yLock, transform.position.z);
    }
}
