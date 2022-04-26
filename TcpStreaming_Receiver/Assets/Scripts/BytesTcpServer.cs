using System;
using System.Collections.Generic;
using System.Linq;
using System.Net; 
using System.Net.Sockets; 
using System.Threading;
using UnityEngine;  

public class BytesTcpServer : MonoBehaviour {
	public Action<byte[]> onRecv;
	public float streamDelay;
	public bool isConnected = false;

	private readonly int messageByteLength = 24;
	private readonly int _port = 56666;
	private byte[] _recvBytes;
	
	private TcpListener _tcpListener; 
	private TcpClient _connectedClient;
	private Thread _listenThread;
	private Queue<float> _readDelayQueue = new Queue<float>();
	private int _readDelayCheckCountMax = 10;

	private void Awake()
	{
		Loom.Initialize();
		BeginServer();
	}

	private void BeginServer()
	{
		QuitTcpServer();
		_listenThread = new Thread (ReadLoop); 		
		_listenThread.IsBackground = true; 		
		_listenThread.Start();
		Debug.Log($"◆◇◇(1)미러링TCP - Thread Start (Thread상태:{_listenThread.ThreadState}(port:{_port}))");
	}

	private void ReadLoop () { 		
		try {
			_tcpListener = new TcpListener(IPAddress.Any, _port); 			
			_tcpListener.Start();     

			while (true)
			{
				Debug.Log($"◆◆◇(2)미러링TCP - 클라이언트 접속을 대기 중...(port:{_port})");
				isConnected = false;
				_connectedClient = _tcpListener.AcceptTcpClient();
				isConnected = true;
				Debug.Log($"◆◆◆(3)미러링TCP - 클라이언트 연결 완료...(port:{_port})");

				while (true)
				{
					try
					{
						float streamReadTime = 0;
						Loom.QueueOnMainThread(() => streamReadTime = Time.time );
							
						//Read Image Count
						int imageSize = ReadImageByteSize(messageByteLength, _connectedClient);
						if (imageSize == -1)
							break;
						
						//Read Image Bytes and Display it
						ReadFrameByteArray(imageSize, _connectedClient);
					
						Loom.QueueOnMainThread(() =>
						{
							_readDelayQueue.Enqueue(Time.time - streamReadTime);
							if (_readDelayQueue.Count > _readDelayCheckCountMax)
								_readDelayQueue.Dequeue();
							streamDelay = _readDelayQueue.Average();
							//Sender쪽에서는 30fps, 즉 30/60 = 0.05f; 초에 한 번 보냄
							//대략 streamReadDelay = 0.05초에 하나씩 받게 되고;
							//즉 0.05초 초과된 시간이 Write or Read의 딜레이가 포함된 시간
							streamDelay =Mathf.Clamp(streamDelay- 0.05f, 0, 10);
						});
					}
					catch (Exception e)
					{
						Debug.Log($"◇◇◇(0)미러링TCP - 연결이 끊기거나, Data를 못 받은 상태(detail:{e}))");
						break;
					}
				}
			} 		
		} 		
		catch (SocketException socketException) { 			
			Debug.Log("◇◇◇(-1)미러링TCP - SocketException " + socketException);
		}     
	}  	
	
	private int ReadImageByteSize(int size, TcpClient client) {
		bool disconnected = false;

		NetworkStream serverStream = client.GetStream();
		
		byte[] imageBytesCount = new byte[size];
		var total = 0;
		
		do {
			var read = serverStream.Read(imageBytesCount, total, size - total);
			if (read == 0)
			{
				disconnected = true;
				break;
			}
			total += read;
		} while (total != size);
		
		int byteLength;

		if (disconnected) {
			byteLength = -1;
		} else {
			byteLength = FrameByteArrayToByteLength(imageBytesCount);
		}
		return byteLength;
	}
	
	private void ReadFrameByteArray(int size, TcpClient client)
	{
		if (size < 100) return;
		
		bool disconnected = false;

		NetworkStream serverStream = client.GetStream();

		byte[] imageBytes = new byte[size];
		var total = 0;
		do {
			var read = serverStream.Read(imageBytes, total, size - total); 
			if (read == 0)
			{
				disconnected = true;
				break;
			}
			total += read;
		} while (total != size);

		//Display Image
		if (!disconnected) {
			_recvBytes = imageBytes;
			Loom.QueueOnMainThread(() => onRecv?.Invoke(_recvBytes));
		}
	}

	//Converts the byte array to the data size and returns the result
	int FrameByteArrayToByteLength(byte[] frameBytesLength) {
		int byteLength = BitConverter.ToInt32(frameBytesLength, 0);
		return byteLength;
	}

	private void QuitTcpServer()
	{
		try
		{
			_connectedClient?.Close();
			_tcpListener?.Stop();
			_listenThread?.Abort();
		}
		catch (Exception e)
		{
			Debug.LogError(e);
		}
	}
	
	private void OnApplicationQuit()
	{
		QuitTcpServer();
	}
}