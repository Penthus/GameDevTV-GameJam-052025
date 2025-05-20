using UnityEngine;
using UnityEngine.Rendering.Universal;



public class CameraZoom : MonoBehaviour
{
    [SerializeField] PixelPerfectCamera pixelPerfectCamera;

    int[] zoomLevels = { 16, 32, 64, 128 };
    int currentZoomLevel = 0;


    void Start()
    {
        pixelPerfectCamera.assetsPPU = 32;
        currentZoomLevel = 1; // Start with the second zoom level (32 PPU)
    }

    public void IncreaseZoomLevel()
    {
        if (currentZoomLevel >= zoomLevels.Length - 1)
        {
            Debug.Log("Max zoom level reached");
            return;
        }
        currentZoomLevel++;
        pixelPerfectCamera.assetsPPU = zoomLevels[currentZoomLevel];
    }

    public void DecreaseZoomLevel()
    {
        if (currentZoomLevel <= 0)
        {
            Debug.Log("Min zoom level reached");
            return;
        }
        currentZoomLevel--;
        pixelPerfectCamera.assetsPPU = zoomLevels[currentZoomLevel];
    }
}
