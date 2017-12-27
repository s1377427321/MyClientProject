using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Object = UnityEngine.Object;

#region define
public enum UICollider
{
    None,      // 显示该界面不包含碰撞背景
    Normal,    // 碰撞透明背景
    WithBg,    // 碰撞非透明背景
}

public enum UIMode
{
    DoNothing,
    HideOther,     // 闭其他界面
    NeedBack,      // 点击返回按钮关闭当前,不关闭其他界面(需要调整好层级关系)
    NoNeedBack,    // 关闭TopBar,关闭其他界面,不加入backSequence队列
}

public enum UIType
{
    Normal,
    Fixed,
    PopUp,
    None,      //独立的窗口
}
#endregion

namespace Client.UIFramework.UI
{
    public abstract class Page
    {
        public string name = string.Empty;
        //this page's type
        public UIType type = UIType.Normal;

        //how to show this page.
        public UIMode mode = UIMode.DoNothing;

        //the background collider mode
        public UICollider collider = UICollider.None;

        //path to load ui
        public string uiPath = string.Empty;

        //this ui's gameobject
        public GameObject gameObject;
        public Transform transform;

        //record this ui load mode.async or sync.
        private bool isAsyncUI = false;
        private bool isLoading = false;

        //delegate load ui function.
        public static Func<string, Object> delegateSyncLoadUI = null;
        public static Action<string, Action<GameObject>> delegateAsyncLoadUI = null;

        //HideData forcs destroy
        public bool forceHide = false;
        private bool m_hideData = true;
        public bool HideData
        {
            get
            {
                return m_hideData;
            }
            set
            {
                m_hideData = value;
            }
        }

        //refresh page 's data.
        private object m_data = null;
        protected object data { get { return m_data; } }


        protected bool isActived = false;

        private static List<Page> m_currentPageNodes;
        public static List<Page> currentPageNodes
        { get { return m_currentPageNodes; } }


        private static Dictionary<string, Page> m_allPages;
        public static Dictionary<string, Page> allPages
        { get { return m_allPages; } }



        public Page(UIType type, UIMode mod, UICollider col)
        {
            this.type = type;
            this.mode = mod;
            this.collider = col;
            this.name = this.GetType().ToString();

            //when create one page.
            //bind special delegate .
            Bind.bind();
            //Debug.LogWarning("[UI] create page:" + ToString());
        }


        ///When Instance UI Ony Once.
        public virtual void Awake(GameObject go) { }

        ///Show UI Refresh Eachtime.
        public virtual void Refresh() { }

        public GameObject FindGameObject(string path)
        {
            Transform tran = Find(path);
            if (tran)
                return tran.gameObject;
            return null;
        }

        public Transform Find(string path)
        {
            if (!transform) return null;
            return transform.Find(path);
        }

        public T GetComponent<T>(string path) where T : Component
        {
            var tran = Find(path);
            if (!tran) return null;
            return tran.GetComponent<T>();
        }

        protected virtual void Hide()
        {
            if (HideData)
            {
                m_currentPageNodes.Remove(this);
                m_allPages.Remove(this.name);
                Object.DestroyObject(gameObject);
            }
            else
            {
                this.gameObject.SetActive(false);
                isActived = false;
                //set this page's data null when hide.
                this.m_data = null;
            }
        }

        private static void ShowPage<T>(Action callback, object pageData, bool isAsync) where T : Page, new()
        {
            Type t = typeof(T);
            string pageName = t.ToString();

            if (m_allPages != null && m_allPages.ContainsKey(pageName))
            {
                ShowPage(pageName, m_allPages[pageName], callback, pageData, isAsync);
            }
            else
            {
                T instance = new T();
                ShowPage(pageName, instance, callback, pageData, isAsync);
            }
        }

        private static void ShowPage(string pageName, Page pageInstance, Action callback, object pageData, bool isAsync)
        {
            if (string.IsNullOrEmpty(pageName) || pageInstance == null)
            {
                Debug.LogError("[UI] show page error with :" + pageName + " maybe null instance.");
                return;
            }

            if (m_allPages == null)
            {
                m_allPages = new Dictionary<string, Page>();
            }

            Page page = null;
            if (m_allPages.ContainsKey(pageName))
            {
                page = m_allPages[pageName];
            }
            else
            {
                m_allPages.Add(pageName, pageInstance);
                page = pageInstance;
            }

            page.name = pageName;
            page.m_data = pageData;

            if (isAsync)
                page.Show(callback);
            else
                page.Show();

        }

        /// <summary>
        /// Async Show UI Logic
        /// </summary>
        protected void Show(Action callback)
        {
            if (this.isLoading && this.gameObject == null)
            {
                return;
            }
            Root.Instance.StartCoroutine(AsyncShow(callback));
        }

        IEnumerator AsyncShow(Action callback)
        {
            //1:Instance UI
            //FIX:support this is manager multi gameObject,instance by your self.
            if (this.gameObject == null && string.IsNullOrEmpty(uiPath) == false)
            {
                this.isLoading = true;
                GameObject go = null;
                bool _loading = true;
                delegateAsyncLoadUI(uiPath, (o) =>
                {
                    this.isLoading = false; ;
                    go = o != null ? GameObject.Instantiate(o) as GameObject : null;
#if UNITY_EDITOR && !UNITY_STANDALONE
#else
                    ResDestroyAdd(go, uiPath);
#endif

                    AnchorUIGameObject(go);

                    Awake(go);
                    isAsyncUI = true;
                    _loading = false;

                    //:animation active.
                    Active();
                    //:refresh ui component.
                    Refresh();
                    //:popup this node to top if need back.
                    PopNode(this);

                    if (callback != null) callback();
                });

                float _t0 = Time.realtimeSinceStartup;
                while (_loading)
                {
                    if (Time.realtimeSinceStartup - _t0 >= 100.0f)
                    {
                        Debug.LogError(uiPath + " [UI] WTF async load your ui prefab timeout!");
                        yield break;
                    }
                    yield return null;
                }
            }
            else
            {
                //:animation active.
                Active();

                //:refresh ui component.
                Refresh();

                //:popup this node to top if need back.
                PopNode(this);

                if (callback != null) callback();
            }
        }


        /// <summary>
        /// Sync Show UI Logic
        /// </summary>
        protected void Show()
        {
            //1:instance UI
            if (this.gameObject == null && string.IsNullOrEmpty(uiPath) == false)
            {
                GameObject go = null;
                if (delegateSyncLoadUI != null)
                {
                    Object o = delegateSyncLoadUI(uiPath);
                    go = o != null ? GameObject.Instantiate(o) as GameObject : null;
                }
                else
                {
                    go = GameObject.Instantiate(Resources.Load(uiPath)) as GameObject;
                }

                //protected.
                if (go == null)
                {
                    Debug.LogErrorFormat("[UI] Cant sync load your ui prefab. {0}", uiPath);
                    return;
                }
                go.name = go.name.Replace("(Clone)", "");
#if UNITY_EDITOR && !UNITY_STANDALONE
#else
                ResDestroyAdd(go, uiPath);
#endif
                AnchorUIGameObject(go);

                //after instance should awake init.
                Awake(go);
                //mark this ui sync ui
                isAsyncUI = false;
            }

            //:animation or init when active.
            Active();

            //:refresh ui component.
            Refresh();

            //:popup this node to top if need back.
            PopNode(this);
        }


        public void ResDestroyAdd(GameObject go, string abname)
        {
            abname = abname.ToLower();
            if (!abname.EndsWith(".prefab"))
            {
                abname += ".prefab";
            }
            if (!go.GetComponent<ResDestroy>())
            {
                ResDestroy res = go.AddComponent<ResDestroy>();
                res.m_abName = abname;
            }
        }



        /// <summary>
        /// make the target node to the top.
        /// </summary>
        private static void PopNode(Page page)
        {
            if (m_currentPageNodes == null)
            {
                m_currentPageNodes = new List<Page>();
            }

            if (page == null)
            {
                Debug.LogError("[UI] page popup is null.");
                return;
            }

            //sub pages should not need back.
            if (CheckIfNeedBack(page) == false)
            {
                return;
            }

            bool _isFound = false;
            for (int i = 0; i < m_currentPageNodes.Count; i++)
            {
                if (m_currentPageNodes[i].Equals(page))
                {
                    m_currentPageNodes.RemoveAt(i);
                    m_currentPageNodes.Add(page);
                    _isFound = true;
                    break;
                }
            }

            //if dont found in old nodes
            //should add in nodelist.
            if (!_isFound)
            {
                m_currentPageNodes.Add(page);
            }

            //after pop should hide the old node if need.
            HideOldNodes();
        }

        private static void HideOldNodes()
        {
            if (m_currentPageNodes.Count < 0) return;
            Page topPage = m_currentPageNodes[m_currentPageNodes.Count - 1];
            if (topPage.mode == UIMode.HideOther)
            {
                //form bottm to top.
                for (int i = m_currentPageNodes.Count - 2; i >= 0; i--)
                {
                    if (m_currentPageNodes[i].isActive())
                        m_currentPageNodes[i].Hide();
                }
            }
        }

        public bool isActive()
        {

            //fix,if this page is not only one gameObject
            //so,should check isActived too.
            bool ret = gameObject != null && gameObject.activeSelf;
            return ret || isActived;
        }

        private static bool CheckIfNeedBack(Page page)
        {
            return page != null && page.CheckIfNeedBack();
        }

        internal bool CheckIfNeedBack()
        {
            if (type == UIType.Fixed || type == UIType.None) return false;
            else if (mode == UIMode.NoNeedBack || mode == UIMode.DoNothing) return false;
            return true;
        }

        ///Active this UI
        public virtual void Active()
        {
            if (!this.gameObject.activeSelf)
            {
                this.gameObject.SetActive(true);
            }
            isActived = true;
            gameObject.GetComponent<RectTransform>().SetSiblingIndex(transform.parent.childCount);
        }

        protected void AnchorUIGameObject(GameObject ui)
        {
            if (Root.Instance == null || ui == null) return;

            this.gameObject = ui;
            this.transform = ui.transform;

            //check if this is ugui or (ngui)?
            Vector3 anchorPos = Vector3.zero;
            Vector2 sizeDel = Vector2.zero;
            Vector3 scale = Vector3.one;
            if (ui.GetComponent<RectTransform>() != null)
            {
                anchorPos = ui.GetComponent<RectTransform>().anchoredPosition;
                sizeDel = ui.GetComponent<RectTransform>().sizeDelta;
                scale = ui.GetComponent<RectTransform>().localScale;
            }
            else
            {
                anchorPos = ui.transform.localPosition;
                scale = ui.transform.localScale;
            }

            //Debug.Log("anchorPos:" + anchorPos + "|sizeDel:" + sizeDel);

            if (type == UIType.Fixed)
            {
                ui.transform.SetParent(Root.Instance.fixedRoot);
            }
            else if (type == UIType.Normal)
            {
                ui.transform.SetParent(Root.Instance.normalRoot);
            }
            else if (type == UIType.PopUp)
            {
                ui.transform.SetParent(Root.Instance.popupRoot);
            }


            if (ui.GetComponent<RectTransform>() != null)
            {
                ui.GetComponent<RectTransform>().anchoredPosition = anchorPos;
                ui.GetComponent<RectTransform>().sizeDelta = sizeDel;
                ui.GetComponent<RectTransform>().localScale = scale;
            }
            else
            {
                ui.transform.localPosition = anchorPos;
                ui.transform.localScale = scale;
            }
        }


        /// <summary>
        /// Sync Show Page
        /// </summary>
        public static void ShowPage<T>() where T : Page, new()
        {
            ShowPage<T>(null, null, true);
        }

        /// <summary>
        /// Sync Show Page With Page Data Input.
        /// </summary>
        public static void ShowPage<T>(object pageData) where T : Page, new()
        {
            ShowPage<T>(null, pageData, false);
        }


        public static void ShowPage(string pageName, Page pageInstance)
        {
            ShowPage(pageName, pageInstance, null, null, false);
        }

        public static void ShowPage(string pageName, Page pageInstance, object pageData)
        {
            ShowPage(pageName, pageInstance, null, pageData, false);
        }

        /// <summary>
        /// Async Show Page with Async loader bind in 'Bind.Bind()'
        /// </summary>
        public static void ShowPage<T>(Action callback) where T : Page, new()
        {
            ShowPage<T>(callback, null, true);
        }

        public static void ShowPage<T>(Action callback, object pageData) where T : Page, new()
        {
            ShowPage<T>(callback, pageData, true);
        }

        /// <summary>
        /// Async Show Page with Async loader bind in 'Bind.Bind()'
        /// </summary>
        public static void ShowPage(string pageName, Page pageInstance, Action callback)
        {
            ShowPage(pageName, pageInstance, callback, null, true);
        }

        public static void ShowPage(string pageName, Page pageInstance, Action callback, object pageData)
        {
            ShowPage(pageName, pageInstance, callback, pageData, true);
        }

    }
}
