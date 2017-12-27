using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LuaMgr
{
    public class LuaHelper 
    {
        static Dictionary<string, byte[]> mCacheLuaFile = new Dictionary<string, byte[]>();

        const string kAssetBundlesPath = "/AssetBundles/";

        static public void Init()
        {

        }


        /// <summary>
        /// 清除脚本文件缓存
        /// </summary>
        public static void Clear()
        {
            mCacheLuaFile.Clear();
        }


        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="Data">明文</param>
        /// <returns></returns>
        public static byte[] AecEnCode(byte[] bs)
        {
            byte[] keys = System.Text.Encoding.UTF8.GetBytes(Const.Key);
            for (int i = 0; i < bs.Length; i++)
            {
                bs[i] = (byte)(bs[i] ^ keys[i % keys.Length]);
            }
            return bs;
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="Data">密文</param>
        /// <returns></returns>
        public static byte[] AecDeCode(byte[] bs)
        {
            byte[] keys = System.Text.Encoding.UTF8.GetBytes(Const.Key);
            for (int i = 0; i < bs.Length; i++)
            {
                bs[i] = (byte)(bs[i] ^ keys[i % keys.Length]);
            }
            return bs;
        }



        /// 读lua脚本文件
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static byte[] DoFile(string fn)
        {
            try
            {
                fn = fn.Replace(".", "/");

                byte[] data = null;
                var fileUtils = FileUtils.getInstance();
                if (!fn.EndsWith(".lua")) fn += ".lua";
                if (mCacheLuaFile.ContainsKey(fn))
                {
                    return mCacheLuaFile[fn];
                }
                string path = fileUtils.getFullPath("lua/" + fn);
                if (string.IsNullOrEmpty(path))
                {
                    //加载AssetBundle
                    Debug.LogError("读不到文件--->" + fn);
                    return null;
                }
                else
                {
                    data = fileUtils.getBytes(path);
                }

#if UNITY_EDITOR && UNITY_STANDALONE
                if (Setting.setting.update)
                {
                    if (data != null)
                    {
                        data = AecDeCode(data);
                        mCacheLuaFile.Add(fn, data);
                    }
                }
#elif UNITY_EDITOR
                string relativePath = System.Environment.CurrentDirectory.Replace("\\", "/");
                if (data != null && path.IndexOf(relativePath) == -1)
                {
                    data = AecDeCode(data);
                    mCacheLuaFile.Add(fn, data);
                }
#else
                    if (data != null)
                    {
                        data = AecDeCode(data);
                        mCacheLuaFile.Add(fn, data);
                    }
#endif
                return data;
            }
            catch (System.Exception e)
            {
                throw e;
            }
        }





    }
}
