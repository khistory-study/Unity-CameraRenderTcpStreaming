using System;
using System.Collections;
using UnityEngine;

public class TextureSender : MonoBehaviour
{
    //TCPClient
    private BytesTcpClient _bytesTcpClient;
    
    [Header("Network")]
    public string ip = "127.0.0.1";
    public int port = 56666;
    public float streamDelayBoundary = 0.1f;
    public float streamDelayCheckSeconds = 0.5f;

    [Header("Settings")] 
    public int quality = 70;
    public float streamFPS = 30;
    public float resolutionMulti = 1f;
    public TargetCamType targetCamType = TargetCamType.MainCam;
    public ResolutionType defaultResolutionType = ResolutionType.p720;
    public Camera specificCam;
    
    private Camera mainMirrorCam;

    private Vector2 _customRes = new Vector2(1080, 720);
    private Vector2 _currentRes = new Vector2(1080, 720);

    private RenderTexture _rt;
    private Texture2D _captureTexture;
    private byte[] _dataBytes;
    
    private void OnEnable()
    {
        BeginSender(specificCam, ip, port, defaultResolutionType);
        
        StartCoroutine(UpdateTextureLoop());
        StartCoroutine(StreamDelayCheckLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public void BeginSender(Camera cam, string targetIp, int targetPort, ResolutionType resType = ResolutionType.p720, float customResWidth = 100, float customResHeight = 100)
    {
        defaultResolutionType = resType;
        specificCam = cam;

        _customRes = new Vector2(customResWidth, customResHeight);
        if (_bytesTcpClient == null)
            _bytesTcpClient = gameObject.AddComponent<BytesTcpClient>();
        _bytesTcpClient.ConnectToServer(targetIp, targetPort);
    }
    
    IEnumerator UpdateTextureLoop()
    {
        float nextUpdateTime = 0f;
        float interval = 0;
        
        while (true) {
            yield return null;
            if (Time.realtimeSinceStartup > nextUpdateTime) {
                if (streamFPS > 0) {
                    interval = 1f / streamFPS;
                    nextUpdateTime = Time.realtimeSinceStartup + interval;
                    
                    if(resolutionMulti <= 0) continue;
                    
                    yield return new WaitForEndOfFrame();
                    
                    try
                    {
                        UpdateResolution();

                        switch (targetCamType)
                        {
                            case TargetCamType.MainCam:
                                ScreenProcess();
                                break;
                            case TargetCamType.SpecificCam:
                                CameraProcess();
                                break;
                        }
        
                        EncodeBytes();
                        
                        _bytesTcpClient.SetBytes(_dataBytes);
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e);
                    }
                }
                yield return null;
            }
        }
    }

    IEnumerator StreamDelayCheckLoop()
    {
        while (true)
        {
            if (_bytesTcpClient.StreamDelay >= streamDelayBoundary)
                resolutionMulti -= 0.01f;
            else
                resolutionMulti += 0.01f;
            
            resolutionMulti = Mathf.Clamp(resolutionMulti, 0.1f, 1);
            yield return new WaitForSeconds(streamDelayCheckSeconds);
        }
    }

    private void ScreenProcess()
    {
        if (Camera.main == null) return;
        if (mainMirrorCam == null)
        {
            //create mirrorCam
            GameObject camObj = new GameObject("sender's mainCam");
            camObj.transform.SetParent(Camera.main.transform);
            camObj.transform.localPosition = Vector3.zero;
            Camera cam = camObj.AddComponent<Camera>();
            cam.enabled = false;
            mainMirrorCam = cam;
        }

        mainMirrorCam.targetTexture = _rt;
        mainMirrorCam.Render();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = _rt;
        _captureTexture  = new Texture2D( _rt.width, _rt.height, TextureFormat.RGB24, false);
        _captureTexture.ReadPixels(new Rect(0, 0,  _rt.width, _rt.height), 0, 0, false);
        _captureTexture.Apply();
        RenderTexture.active = previous;

    }
    
    private void CameraProcess()
    {
        if (specificCam == null) return;
        
        specificCam.targetTexture = _rt;
        specificCam.Render();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = _rt;
        _captureTexture.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
        _captureTexture.Apply();
        RenderTexture.active = previous;
    }



    private void EncodeBytes()
    {
        if (_captureTexture == null) return;
        
        _dataBytes = _captureTexture.EncodeToJPG(quality);
    }

    private void UpdateResolution()
    {
        SetResolution();
        
        _currentRes.x = Mathf.RoundToInt(_currentRes.x);
        _currentRes.y = Mathf.RoundToInt(_currentRes.y);
        
        if (_currentRes.x == 0)
            _currentRes.x = 1;
        if (_currentRes.y == 0)
            _currentRes.y = 1;
        
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
    
    private void SetResolution()
    {
        if (defaultResolutionType == ResolutionType.Custom)
            _currentRes = _customRes;
        else
            _currentRes = defaultResolutionType.GetResolution() * resolutionMulti;
    }

}
