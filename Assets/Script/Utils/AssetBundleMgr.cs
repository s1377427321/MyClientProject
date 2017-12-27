using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Client.UIFramework.UI;

namespace Utils
{
    public class AssetBundleMgr
    {
        class AssetBundleRef
        {
            public AssetBundle assetbundle;
            public int count;
            public bool preLoad;
        }

        AssetBundleManifest m_HallManifest;
        AssetBundleManifest m_GameManifest;

        private Dictionary<string, AssetBundleRef> m_LoadedAssetBundle = new Dictionary<string, AssetBundleRef>();

        static AssetBundleMgr _ins;
        public static AssetBundleMgr Instance
        {
            get
            {
                if (null == _ins) _ins = new AssetBundleMgr();
                return _ins;
            }
        }

        private FileUtils fileUtils;
        AssetBundleMgr()
        {
            fileUtils = FileUtils.getInstance();
        }

        public IEnumerator Init(Action cb)
        {
#if UNITY_EDITOR && !UNITY_STANDALONE
            yield return null;
#else
            AssetBundleManifest manifest;
            if (Const.GAME_CURRENT_NAME == Const.GAME_COMMON_NAME)
            {
                manifest = m_HallManifest;
            }
            else
            {
                manifest = m_GameManifest;
            }

            if (!manifest)
            {
                AssetBundle ab = fileUtils.getRootAssetBundle(Const.GAME_CURRENT_NAME.ToLower() + "assetbundle");
                AssetBundleRequest abr = ab.LoadAssetAsync<AssetBundleManifest>("AssetBundleManifest");
                yield return abr;
                while (!abr.isDone)
                {
                    yield return null;
                }

                manifest = abr.asset as AssetBundleManifest;
                ab.Unload(false);
                if (Const.GAME_CURRENT_NAME == Const.GAME_COMMON_NAME)
                {
                    m_HallManifest = manifest;
                    yield return AddIE("common/art/shader/shaderlibs", (sab) =>
                    {
                        sab.LoadAllAssets();
                        Shader.WarmupAllShaders();
                    });
                }
                else
                {
                    m_GameManifest = manifest;
                }

            }

#endif
            if (cb != null)
            {
                cb();
            }
        }

        IEnumerator AddIE(string path, Action<AssetBundle> callback)
        {
            if (m_LoadedAssetBundle.ContainsKey(path))
            {
                AssetBundleRef ret = m_LoadedAssetBundle[path];
                while (ret.assetbundle == null)
                {
                    yield return null;
                }

                ret.count++;
                if (ret.preLoad) Debug.Log("preLoad " + path);
                ret.preLoad = false;
                string[] _list = GetAllDependencies(path);
                for (int i = 0; i < _list.Length; i++)
                {
                    yield return AddIE(_list[i], null);
                }
                if (callback != null)
                {
                    callback(ret.assetbundle);
                }

            }
            else
            {
                AssetBundleRef ret = new AssetBundleRef();
                ret.count = 1;
                m_LoadedAssetBundle.Add(path, ret);
                string[] list = GetAllDependencies(path);
                for (int i = 0; i < list.Length; i++)
                {
                    yield return AddIE(list[i], null);
                }
                yield return OpenPath(path, callback);
            }

        }

        IEnumerator OpenPath(string path, Action<AssetBundle> callback)
        {
            AssetBundleRef ret = m_LoadedAssetBundle[path];
            AssetBundleCreateRequest abcr = fileUtils.getAssetBundleFromFileAsync(path);
            yield return abcr;
            if (abcr != null && abcr.assetBundle != null)
            {
                ret.assetbundle = abcr.assetBundle;

                if (callback != null)
                {
                    callback(ret.assetbundle);
                }
            }
        }

        private string[] GetAllDependencies(string path)
        {
            AssetBundleManifest manifest;
            if (path.ToLower().StartsWith("common") || path.ToLower().StartsWith(Const.GAME_COMMON_NAME.ToLower()))
            {
                manifest = m_HallManifest;
            }
            else
            {
                manifest = m_GameManifest;
            }
            return manifest.GetAllDependencies(path);
        }

        public AssetBundle Add(string path)
        {
            path = path.ToLower();
            if (m_LoadedAssetBundle.ContainsKey(path))
            {
                var ret = m_LoadedAssetBundle[path];
                ret.count++;
                if (ret.preLoad) Debug.Log("preLoad " + path);
                ret.preLoad = false;
                string[] _list = GetAllDependencies(path);
                for (int i = 0; i < _list.Length; i++)
                {
                    Add(_list[i]);
                }
                return ret.assetbundle;
            }
            string[] list = GetAllDependencies(path);
            for (int i = 0; i < list.Length; i++)
            {
                Add(list[i]);
            }
            var ab = fileUtils.getAssetBundle(path);
            if (ab)
            {
                AssetBundleRef ret = new AssetBundleRef();
                ret.count = 1;
                ret.assetbundle = ab;
                m_LoadedAssetBundle.Add(path, ret);
            }
            return ab;


        }

        public void AddSync(string path, Action<AssetBundle> callback)
        {
            Root.Instance.StartCoroutine(AddIE(path, callback));
        }

        public void Remove(string path)
        {
            Remove(path, false);
        }

        internal void Remove(string path, bool unload)
        {
            if (string.IsNullOrEmpty(path)) return;
            path = path.ToLower();
            if (m_LoadedAssetBundle.ContainsKey(path))
            {
                string[] list = GetAllDependencies(path);
                for (int i = 0; i < list.Length; i++)
                {
                    Remove(list[i]);
                }
                var ret = m_LoadedAssetBundle[path];
                if (unload)
                {
                    ret.count = 0;
                }
                else if (ret.count > 0)
                {
                    ret.count--;
                }
            }
        }


    }

}
