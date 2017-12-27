using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

public class HttpTool  {
    private HttpTool() { } //Private Constructor

    private static HttpTool myInstance;

    public static HttpTool Instance
    {
        get
        {
            if (myInstance == null)
            {
                myInstance = new HttpTool();
            }
            return myInstance;
        }
    }

    public HttpWebRequest createWebRequest(string url)
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Timeout = 10000;
        request.ReadWriteTimeout = 10000;
        request.Proxy = null;
        return request;
    }



}
