using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class CamTextureRawImage : MonoBehaviour
{
    public TextureReceiver receiver;

    private RawImage _rawImage;

    private void Awake()
    {
        _rawImage = GetComponent<RawImage>();
    }

    private void Update()
    {
        _rawImage.texture = receiver.receivedTexture;
    }
}
