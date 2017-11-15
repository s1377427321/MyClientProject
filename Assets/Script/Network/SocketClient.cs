using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System;

public class SocketClient  {

    private const int head_flag = 78963;
    private const int MAX_READ = 8192;
    private const int PackHeadLen = 32;

    private byte[] byteBuffer = new byte[MAX_READ];

    private TcpClient client = null;

    private MemoryStream memStream;
    private BinaryReader reader;

    public SocketClient() {
        memStream = new MemoryStream();
        reader = new BinaryReader(memStream);
    }

    /// <summary>
    /// 创建链接
    /// </summary>
    /// <param name="host"></param>
    /// <param name="port"></param>
    void ConnectServer(string host, int port)
    {
        Close();
        client = null;
        client = new TcpClient();
        client.SendTimeout = 1000;
        client.ReceiveTimeout = 1000;
        client.NoDelay = true;

        try
        {
            client.BeginConnect(host, port, new AsyncCallback(OnConnect), null);
        }
        catch (Exception e)
        {
            Close();
            Debug.LogError(e.Message);
        }


    }

    /// <summary>
    /// 连接上服务器，在这里就收到数据，然后循环监听
    /// </summary>
    void OnConnect(IAsyncResult ar)
    {
        Debug.Log("连接上服务器");
        if (client.Connected)
        {

        }
        client.GetStream().BeginRead(byteBuffer, 0, MAX_READ, new AsyncCallback(OnRead), null);
    }

    /// <summary>
    /// 读取消息
    /// </summary>
    void OnRead(IAsyncResult asr)
    {
        int bytesRead = 0;
        try
        {
            //等待读取完后获取读取字节数
            lock (client.GetStream())
            {
                bytesRead = client.GetStream().EndRead(asr);
            }

            //处理接收到的消息
            OnReceive(byteBuffer, bytesRead);

            //清理数据，再次监听
            lock (client.GetStream()) {
                Array.Clear(byteBuffer, 0, bytesRead);
                client.GetStream().BeginRead(byteBuffer, 0, MAX_READ, new AsyncCallback(OnRead), null);
            }
        }
        catch (Exception e)
        {
            OnDisconnected(e.Message);
        }
    }

    /// <summary>
    /// 接收到消息
    /// </summary>
    void OnReceive(byte[] bytes, int length)
    {
        memStream.Seek(0, SeekOrigin.End);
        memStream.Write(bytes, 0, length);

        while (true)
        {
            memStream.Seek(0, SeekOrigin.Begin);
            //长度不够。继续读。
            if ((memStream.Length - memStream.Position) < 4)
            {
                memStream.Position = memStream.Length;      //pos指向流尾部。
                return;
            }

            //TODO包头确定。
            int flag = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            if (flag != head_flag)
            {
                memStream.Position = 0;
                memStream.SetLength(0);
                return;
            }

            //和服务器约定的类容
            int seqid = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            int cmd = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            int uid = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            int sid = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            int len = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            int extend_a = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            int extend_b = IPAddress.NetworkToHostOrder(reader.ReadInt32());

            //数据包未读完。进入读状态
            if ((memStream.Length - memStream.Position) < len)  
            {
                //pos指向流尾部
                memStream.Position = memStream.Length;
                return;
            }

            byte[] message = reader.ReadBytes(len);

            //清理已读数据。
            byte[] unread_bytes = reader.ReadBytes((int)(memStream.Length - memStream.Position));
            memStream.Position = 0;
            memStream.SetLength(0);
            memStream.Write(unread_bytes, 0, unread_bytes.Length);

            //再然后是对数据进行解析


        }
    }


        /// <summary>
        /// 写数据
        /// </summary>
        bool WriteMessage(byte[] message)
    {
        bool ret = false;
        MemoryStream ms = null;
        using (ms = new MemoryStream())
        {
            ms.Position = 0;
            BinaryWriter writer = new BinaryWriter(ms);
            ushort msglen = (ushort)message.Length;
            writer.Write(message);
            writer.Flush();

            if (client != null && client.Connected)
            {
                byte[] payload = ms.ToArray();
                //写进去，自然得就发送出去给服务器了
                client.GetStream().BeginWrite(payload, 0, payload.Length, new AsyncCallback(OnWrite), null);
                ret = true;
            }
            else
            {
                Debug.LogError("client.connected----->>false");
                ret = false;
            }
        }

        return ret;
    }


    /// <summary>
    /// 向链接写入数据流
    /// </summary>
    void OnWrite(IAsyncResult r)
    {
        try
        {
            //等待挂起异步写完
            client.GetStream().EndWrite(r);
        }
        catch (Exception ex)
        {
            Debug.LogError("OnWrite--->>>" + ex.Message);
        }
    }

    /// <summary>
    /// 发送消息 已经处理好的消息
    /// </summary>
    public bool SendMessage(byte[] bytes)
    {
        return WriteMessage(bytes);

    }

    void OnDisconnected(string msg)
    {
        Debug.LogError(msg);
        Close();
    }

    public void Close()
    {
        if (client != null)
        {
            if (client.Connected) {
                client.Close();
            }
            client = null;
        }


    }
}
