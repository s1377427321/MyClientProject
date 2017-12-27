namespace Client.UIFramework.UI
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class Bind : MonoBehaviour
    {
        public const int ScreenWidth = 1334;
        public const int ScreenHeight = 750;

        static bool isBind = false;

        public static void bind()
        {
            if (!isBind)
            {
                isBind = true;
                //Debug.LogWarning("Bind For UI Framework.");

                //bind for your loader api to load UI.
                //Page.delegateSyncLoadUI = Resources.Load;
                Page.delegateSyncLoadUI = Utils.ResourceMgr.Instance.LoadGameObject;
                //TTUIPage.delegateAsyncLoadUI = UILoader.Load;
                Page.delegateAsyncLoadUI = Utils.ResourceMgr.Instance.LoadGameObjectAsync;
            }
        }
    }

}
