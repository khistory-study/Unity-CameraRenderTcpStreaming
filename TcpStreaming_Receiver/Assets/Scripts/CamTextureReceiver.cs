using System.Collections;
using UnityEngine;

public class CamTextureReceiver : MonoBehaviour
{
    [Header("ReceivedTexture")]
    public Texture2D receivedTexture;
    private BytesTcpServer _bytesTcpServer;
    private float _defaultResScale = 1; 
    private float _thresholdToReduceRes = 0.2f; //0.2초 딜레이 발생시 해상도 낮추기

    private void Start()
    {
        BeginReceiver();
    }

    private void BeginReceiver()
    {
        if (_bytesTcpServer == null)
            _bytesTcpServer = gameObject.AddComponent<BytesTcpServer>();
        
        _bytesTcpServer.onRecv = ProcessImageData; 
        
        //server의 데이터 읽는 속도를 체크하여 화질 저하
        StartCoroutine(ServerDelayCheckLoop());
    }

    private void ProcessImageData(byte[] byteData)
    {
        StartCoroutine(CoProcessImageData(byteData));
    }

    IEnumerator CoProcessImageData(byte[] byteData) {
        if (byteData == null || byteData.Length == 0)
            yield break;
        
        if (receivedTexture == null)
            receivedTexture = new Texture2D(0, 0);

        receivedTexture.LoadImage(byteData);
        yield return null;
    }
    
    IEnumerator ServerDelayCheckLoop()
    {
        while (true)
        {
            CheckServerDelay();
            yield return new WaitForSeconds(3);
        }
    }
    
    private void CheckServerDelay()
    {
        if (_bytesTcpServer.streamDelay > _thresholdToReduceRes)
            _defaultResScale -= 0.05f;
        else
            _defaultResScale += 0.05f;
            
        _defaultResScale = Mathf.Clamp(_defaultResScale, 0.1f, 1f);
        //resScale을 어떻게 알려줄것인지 추가바람
        //이건 아마도... Texture Sender에서 읽어들어서 수정해야할듯함
    }
}
