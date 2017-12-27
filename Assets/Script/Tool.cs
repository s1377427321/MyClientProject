using DG.Tweening;
using SimpleJson;
using SLua;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Utils {

    public class Tool
    {
        static public T loadObjectFromJsonFile<T>(string path) where T : new()
        {
            string str = FileUtils.getInstance().getString(path);
            T data = loadObjectFromJson<T>(str);
            return data;
        }

        static public T loadObjectFromJson<T>(string str) where T : new()
        {
            if (string.IsNullOrEmpty(str))
            {
                return new T();
            }

            T data;
            try
            {
                data = LitJson.JsonMapper.ToObject<T>(str);
            }
            catch (Exception e)
            {
                data = new T();
                Debug.LogError(e.Message + " Cannot parse data from " + str);
            }

            return data;
        }

        static public void saveObjectToJsonFile<T>(T data, string path)
        {
            string jsonStr = LitJson.JsonMapper.ToJson(data);

            FileUtils.getInstance().writeFileWithCode(path, jsonStr, null);
        }


    }
}
