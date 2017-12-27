using UnityEngine;
using Client.UIFramework.UI;
using VersionCtrl;
using UnityEngine.UI;
using System.Collections;
using Utils;
using LuaMgr;

namespace UI
{

    public class UILoadGame : Page
    {
        Slider mSlider;
        Text labLoading;
        GameObject loading;
        bool isDownload;
        GameObject newVersion;
        Button btnOk;
        Button btnCancel;
        System.Action _onOk;
        System.Action _onCancel;
        Text msg;
        Text mBtnOkText;
        Text mBtnCancelText;

        public UILoadGame() : base(UIType.Normal, UIMode.HideOther, UICollider.Normal)
        {
            uiPath = "Common/ui/Prefab/loading/ui_loadgame";
        }
        public override void Awake(GameObject go)
        {
            loading = FindGameObject("loading");
            mSlider = GetComponent<Slider>("loading/Slider");
            labLoading = GetComponent<Text>("loading/labLoading");
            newVersion = FindGameObject("newVersion");
            btnOk = GetComponent<Button>("newVersion/bg/btn_ok");
            btnCancel = GetComponent<Button>("newVersion/bg/btn_cancel");
            mBtnOkText = GetComponent<Text>("newVersion/bg/btn_ok/Text");
            mBtnCancelText = GetComponent<Text>("newVersion/bg/btn_cancel/Text");
            msg = GetComponent<Text>("newVersion/bg/labMsg");
            btnOk.onClick.AddListener(() =>
            {
                if (_onOk != null)
                    _onOk.Invoke();
            });
            btnCancel.onClick.AddListener(() =>
            {
                if (_onCancel != null)
                    _onCancel.Invoke();
            });
            loading.SetActive(false);
            newVersion.SetActive(false);
        }
        protected override void Hide()
        {
            if(HideData)
            {
                base.Hide();
            }
            else
            {
                this.loading.SetActive(false);
            }
        }
        public override void Refresh()
        {
            labLoading.text = "";
            isDownload = false;
            if (data == null)
                Root.Instance.StartCoroutine(Wait());
            else
                LuaManager.getInstance().CallLuaFunction("App.ReStart");
        }
        /// <summary>
        /// 对话框
        /// </summary>
        /// <param name="m"></param>
        /// <param name="onOk"></param>
        /// <param name="onCancel"></param>
        /// <param name="two"></param>
        /// <param name="txtOk"></param>
        /// <param name="txtCancel"></param>
        private void ShowDialog(string m, System.Action onOk, System.Action onCancel, bool two = true, string txtOk = "确定", string txtCancel = "取消")
        {
            _onOk = onOk;
            _onCancel = onCancel;
            msg.text = m;
            if (!two)
            {
                btnOk.gameObject.SetActive(true);
                btnCancel.gameObject.SetActive(false);
                var rectTransform = btnOk.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(0, rectTransform.anchoredPosition.y);
            }
            else
            {
                btnOk.gameObject.SetActive(true);
                btnCancel.gameObject.SetActive(true);
                var rectTransform = btnOk.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(-164, rectTransform.anchoredPosition.y);
            }
            mBtnOkText.text = txtOk;
            mBtnCancelText.text = txtCancel;
            newVersion.SetActive(true);
        }

        private IEnumerator Wait()
        {
            bool ret = Application.internetReachability == NetworkReachability.NotReachable;
            if (ret)
            {
                ShowDialog("亲，网络连接异常，请稍后重试", () =>
                {
                    Root.Instance.StartCoroutine(Wait());
                }, () =>
                {
                    Root.Instance.StartCoroutine(Wait());
                }, false);
                yield break;
            }
            newVersion.SetActive(false);

            yield return new WaitForSeconds(.2f);

            var src_path = Application.streamingAssetsPath + "/setting.json";
            var src_json = FileUtils.getInstance().getString(src_path);
            Setting src_setting = JsonUtility.FromJson<Setting>(src_json);

            if (this.data != null)
            {
                string s = (string)this.data;
                AssetsMgr mgr = new AssetsMgr(onCallBack, s);
                loading.SetActive(true);
                mgr.Update();
            }
            else if (src_setting.update)
            {
                Const.GAME_CURRENT_NAME = Const.GAME_COMMON_NAME;
                AssetsMgr mgr = new AssetsMgr(Application.streamingAssetsPath,
                    FileUtils.getInstance().getWritablePath(Const.GAME_CURRENT_NAME), onCallBack);
                loading.SetActive(true);
                mgr.Update();
            }
            else
            {
                Const.GAME_CURRENT_NAME = Const.GAME_COMMON_NAME;
                loading.SetActive(true);
                FileUtils.getInstance().genSearchPath();
                upToData(null);
            }
        }
        private void onCallBack(AssetsMgr am, AssetsMgr.State state, int p, string msg)
        {
            switch (state)
            {
                case AssetsMgr.State.Updating:
                    ///下载进度
                    isDownload = true;
                    mSlider.value = p / 100.0f;
                    labLoading.text = string.Format("正在下载资源({0}%)...", p);
                    break;
                case AssetsMgr.State.DecompressFail:
                    isDownload = false;
                    Debug.LogError("解压失败");
                    upToData(am);
                    break;
                case AssetsMgr.State.FailToUpdate:
                    isDownload = false;
                    Debug.LogError(msg + " update fail");
                    upToData(am);
                    break;
                case AssetsMgr.State.UpToData:
                    upToData(am);
                    break;
                case AssetsMgr.State.UpdateSuccess:
                    upToData(am, true);
                    break;
                case AssetsMgr.State.NewBigVersion:
                    ShowDialog("发现新的版本，是否前往下载？", () =>
                    {
                        Application.OpenURL(Setting.setting.downloadLink);
                    }, () =>
                    {
                        newVersion.SetActive(false);
                        upToData(null);
                    }, true, "立即下载", "下次更新");
                    am.genSearchPath();
                    break;
                default:
                    upToData(am);
                    break;
            }
        }

        private void upToData(AssetsMgr am, bool updateSuccess = false)
        {
            if (am != null)
                am.genSearchPath();

            ResourceMgr.Clear();

            Root.Instance.StartCoroutine(
                AssetBundleMgr.Instance.Init(() =>
                {
                    LoadMain(updateSuccess);
                }));
        }

        private void LoadMain(bool updateSuccess)
        {
            if (!isDownload)
            {
                StartMain1(updateSuccess);
            }
            else
            {
                StartMain2(updateSuccess);
            }
        }

        private void StartMain1(bool updateSuccess)
        {
            labLoading.text = "正在加载中，请稍候...";
            LuaManager.getInstance().Start("common/main", updateSuccess, (p) =>
            {
                mSlider.value = p / 100.0f;
                if (p == 101)
                {
                    mSlider.value = 1;
                    loading.SetActive(false);
                }
            });
        }

        private void StartMain2(bool updateSuccess)
        {
            mSlider.value = 1;
            labLoading.text = string.Format("正在初始化...", 100);
            LuaManager.getInstance().Start("common/main", updateSuccess, (p) =>
            {
                if (p == 101)
                {
                    loading.SetActive(false);
                }
            });
        }
    }
}
