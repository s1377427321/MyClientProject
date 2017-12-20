using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Client.UIFramework.UI;

namespace Utils
{ 
    public class AssetBundleMgr
    {
        AssetBundleManifest m_HallManifest;
        AssetBundleManifest m_GameManifest;

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
#if !UNITY_EDITOR && !UNITY_STANDALONE
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
    }

}
