using Client.UIFramework.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Utils
{
    public class ResourceMgr
    {
        private static ResourceMgr _ins;
        private CacheGameObject cache;

        public static ResourceMgr Instance
        {
            get
            {
                if (null == _ins)
                {
                    _ins = new ResourceMgr();
                    _ins.cache = new CacheGameObject();
                }
                return _ins;
            }
        }

        public GameObject LoadGameObject(string path)
        {
            GameObject go = LoadGameObject(path, false);
            return go;
        }

        public GameObject LoadGameObject(string path, bool incache)
        {
            string opath = path;
            if (!path.EndsWith(".prefab"))
            {
                path += ".prefab";
            }
            GameObject go;
            if (incache)
            {
                go = cache.Get(path);
                if (go)
                {
#if UNITY_EDITOR && !UNITY_STANDALONE
                    return go;
#endif
                    AssetBundleMgr.Instance.Add(path);
                    return go;
                }
            }

#if UNITY_EDITOR && !UNITY_STANDALONE
            go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/" + path);
            if (go)
            {
                if (incache)
                {
                    CacheObject(path, go);
                }
                return go;
            }
#endif
            path = path.ToLower();
            var ab = AssetBundleMgr.Instance.Add(path);
            if (ab)
            {
                string name = System.IO.Path.GetFileName(path);
                go = ab.LoadAsset<GameObject>(name);
                if (incache)
                {
                    CacheObject(path, go);
                }
                return go;
            }
            return null;
        }

        public void CacheObject(string path, GameObject go)
        {
            go.name = go.name.Replace("(Clone)", "");
            go.SetActive(false);
            cache.Add(path, go);
        }

        public void LoadGameObjectAsync(string path, System.Action<GameObject> callback)
        {
            LoadGameObjectAsync(path, false, callback);
        }
        public void LoadGameObjectAsync(string path, bool incache, System.Action<GameObject> callback)
        {
            if (!path.EndsWith(".prefab"))
            {
                path += ".prefab";
            }
            GameObject go;
            if (incache)
            {
                go = cache.Get(path);
                if (go && callback != null)
                {
                    callback(go);
                    return;
                }
            }

#if UNITY_EDITOR && !UNITY_STANDALONE
            go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/" + path);
            if (go)
            {
                if (incache)
                {
                    CacheObject(path, go);
                }
                if (go && callback != null)
                {
                    callback(go);
                    return;
                }
            }
#endif
            path = path.ToLower();
            AssetBundleMgr.Instance.AddSync(path, (ab) =>
            {
                if (ab)
                {
                    Root.Instance.StartCoroutine(LoadAsset(ab, path, incache, callback));
                }
            });

        }

        IEnumerator LoadAsset(AssetBundle ab, string path, bool incache, System.Action<GameObject> callback)
        {
            string name = System.IO.Path.GetFileName(path);
            AssetBundleRequest ar = ab.LoadAssetAsync<GameObject>(name);
            yield return ar;
            while (!ar.isDone)
            {
                yield return null;
            }
            GameObject go = ar.asset as GameObject;
            if (incache)
            {
                CacheObject(path, go);
            }
            if (go && callback != null)
            {
                callback(go);
            }
        }

        public static void Clear()
        {
            if (_ins != null)
            {
                _ins.cache.Clear();
            }
            _ins = null;
        }



    }
}
