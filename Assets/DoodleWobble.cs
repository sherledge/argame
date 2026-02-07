using UnityEngine;

public class DoodleWobble : MonoBehaviour
{
    [Header("Doodle Settings")]
    public float jitterAmount = 0.05f; // How much it shakes
    public float rotationJitter = 2f;  // How much it tilts
    public float fps = 10f;            // Low FPS = more authentic doodle feel

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private float timer;

    void Start()
    {
        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Only update the position based on the "fps" variable
        // This creates that "choppy" hand-drawn look
        if (timer >= (1f / fps))
        {
            // Random Position
            float posX = Random.Range(-jitterAmount, jitterAmount);
            float posY = Random.Range(-jitterAmount, jitterAmount);
            transform.localPosition = originalPosition + new Vector3(posX, posY, 0);

            // Random Rotation (Tilting)
            float rotZ = Random.Range(-rotationJitter, rotationJitter);
            transform.localEulerAngles = new Vector3(0, 0, rotZ);

            // Random Scale (Pulsing)
            float scaleFixed = Random.Range(0.98f, 1.02f);
            transform.localScale = originalScale * scaleFixed;

            timer = 0;
        }
    }
}