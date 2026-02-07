using UnityEngine;

public class LoadingAnimCallback : MonoBehaviour
{
    public DetectionManager detectionManager;

    // This WILL appear in Animation Event list
  public void OnDetectionLoadingFinished()
{
    if (detectionManager != null)
        detectionManager.OnDetectionLoadingFinished();
}

}
