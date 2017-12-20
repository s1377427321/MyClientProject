using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Client.UIFramework.UI;

public class GameManager : MonoBehaviour {
    static GameManager cur;

    private void Awake()
    {
        QualitySettings.SetQualityLevel(3);
        AppFacade.Destroy();

        if (cur) Destroy(cur.gameObject);
        cur = this;
    }

    // Use this for initialization
    void Start () {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = 30;
        DontDestroyOnLoad(gameObject);

        // AppFacade.getInstance().RegisterCommand("LoadGame",typ)
        Init();
    }

    private void Init()
    {
        ClearCache();
#if UNITY_EDITOR && UNITY_STANDALONE
        FileUtils.getInstance().genStreamPath();
#else
        FileUtils.getInstance().genSearchPath();
#endif
        Root.Instance.StartCoroutine(
           AssetBundleMgr.Instance.Init(() =>
           {
               init = true;
               AppFacade.getInstance().SendNotification(Const.LoadGame);
           }));

    }

    private void ClearCache()
    {
        var utils = FileUtils.getInstance();
        var s_path = utils.getWritablePath(null)+"/time.txt";
        var src_path = Application.streamingAssetsPath + "/time.txt";
        string time = utils.getString(s_path);
        string src_time = utils.getString(src_path);
        if (string.IsNullOrEmpty(time))
        {
            utils.removeDirectory(utils.getWritablePath(null));
            utils.writeString(s_path, src_time);
            return;
        }

        if (time != src_time)
        {
            utils.removeDirectory(utils.getWritablePath(null));
            utils.writeString(s_path, src_time);
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
