/*
 *           C#Like
 * Copyright © 2022-2024 RongRong. All right reserved.
 */
using System;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Security.Cryptography;
using UnityEngine.Events;

namespace CSharpLike
{
    public class CSL_ClientSocket : MonoBehaviour
    {
        /// <summary>
        /// The action will be call when connect success.
        /// </summary>
        public UnityAction onOpen;
        /// <summary>
        /// The action will be call when WebSocket closed.
        /// </summary>
        public UnityAction<string> onClose;
        /// <summary>
        /// The action will be call when received string message.
        /// </summary>
        public UnityAction<JSONData> onMessage;
        /// <summary>
        /// The action will be call when received error(Connect/Close/Send/Abort action may cause some errors).
        /// </summary>
        public UnityAction<string> onError;

        void Start()
        {
            DontDestroyOnLoad(gameObject);//make sure don't destroy while load scene
        }

        public string Host { get; set; }
        public int Port { get; set; }
        public int SendTimeout { get; set; }
        public string RSAPublicKey { get; set; }
        public enum SocketState
        {
            None,
            Connecting,
            Connected,
        }
        public SocketState socketState = SocketState.None;
        TcpClient tcpClient;
        NetworkStream networkStream;
        StreamWriter streamWriter;
        ICryptoTransform encryptor;
        ICryptoTransform decryptor;
        Thread acceptMsgThread;
        //NetworkReachability internetReachability = NetworkReachability.NotReachable;
        void Disconnect(string strMsg = "")
        {
            Debug.Log("Disconnect......");
            //internetReachability = NetworkReachability.NotReachable;
            socketState = SocketState.None;
            networkStream = null;
            streamWriter = null;
            encryptor = null;
            decryptor = null;
            if (tcpClient != null)
                tcpClient.Close();
            tcpClient = null;
            if (!clientClose)
                EnqueueMsg(new Msg(MsgType.Close, strMsg));
            clientClose = false;
        }
        void OnDecode(byte[] data, int offset, int lenght)
        {
            if (decryptor != null)
                EnqueueMsg(new Msg(MsgType.Message, System.Text.Encoding.UTF8.GetString(decryptor.TransformFinalBlock(data, offset, lenght))));
            else
                EnqueueMsg(new Msg(MsgType.Message, System.Text.Encoding.UTF8.GetString(data, offset, lenght)));
        }

        byte[] _finalBuff;
        int _lenght = 0;
        void Decode(byte[] data, int offset, int lenght)
        {
            if (_lenght == 0)
            {
                _lenght = BitConverter.ToInt32(data, offset);
                if (_lenght <= 4 || _lenght > 10485760)//1024 * 1024 * 10 = 10MB
                {
                    Console.WriteLine("received invalid size : " + _lenght);
                    Clear();
                    return;
                }
                if (_lenght == lenght - 4)//just in one packet
                {
                    OnDecode(data, offset + 4, lenght - 4);
                    //if (decryptor != null)
                    //    EnqueueMsg(new Msg(MsgType.Message, System.Text.Encoding.UTF8.GetString(decryptor.TransformFinalBlock(data, offset + 4, lenght - 4))));
                    //else
                    //    EnqueueMsg(new Msg(MsgType.Message, System.Text.Encoding.UTF8.GetString(data, offset + 4, lenght - 4)));
                    Clear();
                    return;
                }
                //wait complete
                _finalBuff = new byte[_lenght];
                _lenght = lenght - 4;
                Array.Copy(data, offset + 4, _finalBuff, 0, lenght - 4);
            }
            else
            {
                int sumLenght = _lenght + lenght;
                if (sumLenght < _finalBuff.Length)//still not finish
                {
                    Array.Copy(data, offset, _finalBuff, _lenght, lenght);
                    _lenght += lenght;
                }
                else if (sumLenght == _finalBuff.Length)//normal finish
                {
                    Array.Copy(data, offset, _finalBuff, _lenght, lenght);
                    OnDecode(_finalBuff, 0, _finalBuff.Length);
                    //if (decryptor != null)
                    //    EnqueueMsg(new Msg(MsgType.Message, System.Text.Encoding.UTF8.GetString(decryptor.TransformFinalBlock(_finalBuff, 0, _finalBuff.Length))));
                    //else
                    //    EnqueueMsg(new Msg(MsgType.Message, System.Text.Encoding.UTF8.GetString(_finalBuff)));
                    Clear();
                }
                else//packet splicing
                {
                    //finish current packet
                    Array.Copy(data, offset, _finalBuff, _lenght, _finalBuff.Length - _lenght);
                    OnDecode(_finalBuff, 0, _finalBuff.Length);
                    //if (decryptor != null)
                    //    EnqueueMsg(new Msg(MsgType.Message, System.Text.Encoding.UTF8.GetString(decryptor.TransformFinalBlock(_finalBuff, 0, _finalBuff.Length))));
                    //else
                    //    EnqueueMsg(new Msg(MsgType.Message, System.Text.Encoding.UTF8.GetString(_finalBuff)));

                    //process next packet data
                    offset += _finalBuff.Length - _lenght;
                    lenght = sumLenght - _finalBuff.Length;
                    Clear();

                    Decode(data, offset, lenght);
                }
            }
        }
        void Clear()
        {
            _finalBuff = null;
            _lenght = 0;
        }
        void AcceptMsg()
        {
            try
            {
                Debug.Log("Start setupSocket");
                tcpClient = new TcpClient(Host, Port);

                networkStream = tcpClient.GetStream();
                tcpClient.SendTimeout = SendTimeout * 1000; //10 second to timeout
                streamWriter = new StreamWriter(networkStream);

                Debug.Log("AcceptMsg:Finish setupSocket");
            }
            catch (Exception e)
            {
                Debug.LogError("AcceptMsg:Socket error:" + e.ToString());
                Disconnect(e.ToString());
                EnqueueMsg(new Msg(MsgType.Error, e.Message));
                return;
            }
            byte[] buff = new byte[1024];
            int readSize;

            if (!string.IsNullOrEmpty(RSAPublicKey))
            {
                Thread.Sleep(20);
                byte[] key = new byte[32];
                byte[] iv = new byte[16];
                System.Random r = new System.Random();
                r.NextBytes(key);
                r.NextBytes(iv);
                RijndaelManaged rijndaelManaged = new RijndaelManaged();
                encryptor = rijndaelManaged.CreateEncryptor(key, iv);
                decryptor = rijndaelManaged.CreateDecryptor(key, iv);
                RSACryptoServiceProvider crypt = new RSACryptoServiceProvider(2048);
                crypt.FromXmlString(RSAPublicKey);
                byte[] finalBuff = new byte[key.Length + iv.Length];
                Array.Copy(key, 0, finalBuff, 0, key.Length);
                Array.Copy(iv, 0, finalBuff, key.Length, iv.Length);
                finalBuff = crypt.Encrypt(finalBuff, false);
                networkStream.Write(finalBuff, 0, finalBuff.Length);
                streamWriter.Flush();
                Thread.Sleep(20);
            }

            socketState = SocketState.Connected;
            EnqueueMsg(new Msg(MsgType.Open, ""));

            while (socketState == SocketState.Connected)
            {
                try
                {
                    readSize = networkStream.Read(buff, 0, buff.Length);
                    if (readSize == 0)
                    {
                        Thread.Sleep(5);
                        continue;
                    }
                    Decode(buff, 0, readSize);
                }
                catch (IOException e)
                {
                    Debug.LogError("AcceptMsg:Error: System.IO.IOException:" + e.ToString());
                    Disconnect(e.ToString());
                    EnqueueMsg(new Msg(MsgType.Error, e.Message));
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogError("AcceptMsg:Error: System.Exception:" + e.ToString());
                    Disconnect(e.ToString());
                    EnqueueMsg(new Msg(MsgType.Error, e.Message));
                    return;
                }
            }
            Debug.Log("End Socket");
        }

        float nextMessageTime = float.MaxValue;
        void CheckSendHearbeat()
        {
            if (socketState == SocketState.Connected
                && nextMessageTime < Time.unscaledTime
                && Application.internetReachability != NetworkReachability.NotReachable)
            {
                MarkSendHearbeat(false);
                AddToSendQueue("{}");//"{}" mean heartbeat
            }
        }
        void MarkSendHearbeat(bool bDisable)
        {
            nextMessageTime = bDisable ? float.MaxValue : (Time.unscaledTime + 100f);
        }
        byte[] sendBuff = new byte[10240];//10KB
        /// <summary>
        /// Callback the action in main thread using LateUpdate.
        /// </summary>
        void LateUpdate()
        {
            Msg msg;
            while (msgs.Count > 0)
            {
                msg = DequeueMsg();
                switch (msg.type)
                {
                    case MsgType.Message: MarkSendHearbeat(false); onMessage?.Invoke(KissJson.ToJSONData(msg.str)); break;
                    case MsgType.Open: MarkSendHearbeat(false); onOpen?.Invoke(); break;
                    case MsgType.Close: MarkSendHearbeat(true); onClose?.Invoke(msg.str); break;
                    case MsgType.Error: onError?.Invoke(msg.str); break;
                }
            }
            if (socketState == SocketState.Connected && sendingMsg.Count > 0 && Application.internetReachability != NetworkReachability.NotReachable)
            {
                do
                {
                    string strSend = sendingMsg.Dequeue();
                    try
                    {
                        int sendBuffLenght = sendBuff.Length;
                        byte[] buff = System.Text.Encoding.UTF8.GetBytes(strSend);
                        if (encryptor != null)
                            buff = encryptor.TransformFinalBlock(buff, 0, buff.Length);
                        if (buff.Length <= sendBuffLenght - 4)// less then 10KB
                        {
                            Array.Copy(BitConverter.GetBytes(buff.Length), 0, sendBuff, 0, 4);
                            Array.Copy(buff, 0, sendBuff, 4, buff.Length);
                            networkStream.Write(sendBuff, 0, buff.Length + 4);
                            streamWriter.Flush();
                        }
                        else
                        {
                            byte[] sendBuffTemp = new byte[buff.Length + 4];
                            Array.Copy(BitConverter.GetBytes(buff.Length), 0, sendBuffTemp, 0, 4);
                            Array.Copy(buff, 0, sendBuffTemp, 4, buff.Length);
                            int count = sendBuffTemp.Length / sendBuffLenght - 1;
                            if (sendBuffTemp.Length % sendBuffLenght != 0)
                                ++count;
                            int lenght = 0;
                            for (int i=0; i<=count; i++)
                            {
                                if (i != count)
                                    networkStream.Write(sendBuffTemp, lenght, sendBuffLenght);
                                else
                                    networkStream.Write(sendBuffTemp, lenght, sendBuffTemp.Length - sendBuffLenght*count);
                                lenght += sendBuffLenght;
                                streamWriter.Flush();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        onClose?.Invoke(e.ToString());
                        Disconnect();
                    }
                }
                while (sendingMsg.Count > 0);
                MarkSendHearbeat(false);
            }
            CheckSendHearbeat();
        }

        void OnDestroy()
        {
            sendingMsg.Clear();
            Close();
        }
        enum MsgType : byte
        {
            Open,
            Close,
            Error,
            Message,
        }
        struct Msg
        {
            public Msg(MsgType type)
            {
                this.type = type;
                str = null;
            }
            public Msg(MsgType type, string str)
            {
                this.type = type;
                this.str = str;
            }
            public MsgType type;
            public string str;
        }
        Queue<Msg> msgs = new Queue<Msg>();
        object lockObj = new object();
        void EnqueueMsg(Msg msg)
        {
            lock (lockObj)
            {
                msgs.Enqueue(msg);
            }
        }
        Msg DequeueMsg()
        {
            lock (lockObj)
            {
                return msgs.Dequeue();
            }
        }

        public void Connect()
        {
            clientClose = false;
            if (networkStream == null || !networkStream.CanRead)
            {
                socketState = SocketState.Connecting;
                ThreadStart threadNetwork = new ThreadStart(AcceptMsg);
                acceptMsgThread = new Thread(threadNetwork);
                acceptMsgThread.Start();
            }
        }
        /// <summary>
        /// Whether the WebSocket connected
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return socketState == SocketState.Connected;
            }
        }
        bool clientClose = false;

        public void Close()
        {
            Debug.LogWarning("start Close");
            clientClose = true;
            MarkSendHearbeat(true);
            Disconnect();
        }
        public void Send(JSONData jsonData)
        {
            AddToSendQueue(jsonData.ToJson());
        }
        Queue<string> sendingMsg = new Queue<string>();
        public void AddToSendQueue(string msg)
        {
            if (socketState == SocketState.None)
                Connect();
            sendingMsg.Enqueue(msg);
        }
        public void ClearSendQueue()
        {
            sendingMsg.Clear();
        }
    }
}