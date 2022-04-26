using System;
using System.Collections;
using UnityEngine;

public class CamTextureSender : MonoBehaviour
{
    //TCPClient
    private BytesTcpClient _bytesTcpClient;
    
    [Header("Settings")]
    public Camera renderCam;
    public int quality = 70;
    public float streamFPS = 30;
    public float resolutionMulti = 1f;
    public ResolutionType defaultResolutionType = ResolutionType.p720;

    private Vector2 _customRes = new Vector2(1080, 720);
    private Vector2 _currentRes = new Vector2(1080, 720);
    
    private float _interval = 0.05f;
    private float _nextUpdateTime = 0f;

    private bool _needUpdateTexture = false;
    private bool _encodingTexture = false;
    
    private RenderTexture _rt;
    private Texture2D _captureTexture;
    private byte[] _dataBytes;

    private void Awake()
    {
        if (_bytesTcpClient == null)
            _bytesTcpClient = gameObject.AddComponent<BytesTcpClient>();
    }

    private void OnEnable()
    {
        StartCoroutine(UpdateAndSendBytes());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public void BeginSender(Camera cam, string serverIp, ResolutionType resType = ResolutionType.p720, float customResWidth = 100, float customResHeight = 100)
    {
        defaultResolutionType = resType;
        renderCam = cam;

        _customRes = new Vector2(customResWidth, customResHeight);
        _bytesTcpClient.ConnectToServer(serverIp);
    }
    
    IEnumerator UpdateAndSendBytes()
    {
        while (true) {
            if (Time.realtimeSinceStartup > _nextUpdateTime) {
                try
                {
                    if (streamFPS > 0) {
                        _interval = 1f / streamFPS;
                        _nextUpdateTime = Time.realtimeSinceStartup + _interval;
                        
                        if (!_encodingTexture && resolutionMulti > 0) {
                            _needUpdateTexture = true;

                            SetResolution();
                            UpdateResolution();
                            UpdateTextures();
                            
                            StartCoroutine(RenderTextureRefresh());
                            
                            if(_dataBytes == null) continue;

                            _bytesTcpClient.SetBytes(_dataBytes);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
                yield return null;
            }
            yield return null;
        }
    }

    private void SetResolution()
    {
        if (defaultResolutionType == ResolutionType.Custom)
            _currentRes = _customRes;
        else
            _currentRes = defaultResolutionType.GetResolution() * resolutionMulti;
    }

    IEnumerator RenderTextureRefresh() {
        if (_needUpdateTexture && !_encodingTexture) {
            _needUpdateTexture = false;
            _encodingTexture = true;

            yield return new WaitForEndOfFrame();

            if (renderCam != null) {
                renderCam.targetTexture = _rt;
                renderCam.Render();

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = _rt;
                StartCoroutine(ProcessCapturedTexture());
                RenderTexture.active = previous;
            }
            else {
                _encodingTexture = false;
            }
        }
    }
    
    IEnumerator ProcessCapturedTexture() {
        _captureTexture.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
        _captureTexture.Apply();
        StartCoroutine(EncodeBytes());
        yield break;
    }
    
    IEnumerator EncodeBytes() {
        if (_captureTexture != null) {
            _dataBytes = _captureTexture.EncodeToJPG(quality);
        }
        _encodingTexture = false;
        yield break;
    }

    private void UpdateResolution() {
        _currentRes.x = Mathf.RoundToInt(_currentRes.x);
        _currentRes.y = Mathf.RoundToInt(_currentRes.y);
        
        if (_currentRes.x == 0)
            _currentRes.x = 1;
        if (_currentRes.y == 0)
            _currentRes.y = 1;
    }

    private void UpdateTextures()
    {
        if (_rt == null) {
            _rt = new RenderTexture(Mathf.RoundToInt(_currentRes.x), Mathf.RoundToInt(_currentRes.y), 16, RenderTextureFormat.ARGB32);
        }
        else {
            if (_rt.width != Mathf.RoundToInt(_currentRes.x) || _rt.height != Mathf.RoundToInt(_currentRes.y)) {
                Destroy(_rt);
                _rt = new RenderTexture(Mathf.RoundToInt(_currentRes.x), Mathf.RoundToInt(_currentRes.y), 16, RenderTextureFormat.ARGB32);
            }
        }

        if (_captureTexture == null) {
            _captureTexture = new Texture2D(Mathf.RoundToInt(_currentRes.x), Mathf.RoundToInt(_currentRes.y), TextureFormat.RGB24, false);
        }
        else {
            if (_captureTexture.width != Mathf.RoundToInt(_currentRes.x) || _captureTexture.height != Mathf.RoundToInt(_currentRes.y)) {
                Destroy(_captureTexture);
                _captureTexture = new Texture2D(_rt.width, _rt.height, TextureFormat.RGB24, false);
            }
        }
    }
}
