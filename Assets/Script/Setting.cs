using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Setting
{
    public string api = "http://api.3dcoffice.com";
    public int port = 80;
    public string wechatID;
    public string wechatSecret;
    public string androidBuglyID;
    public string iOSBuglyID;
    public string androidYoumengID;
    public string iOSYoumengID;
    public string talkingDataID;

    public string lastVersion;
    public string platform = "ios";
    public string downloadLink = "http://www.hkrlqp.com";

    public bool iosAudit = false;
    public bool update = true;
    public bool isFPS = false;
    public int type = 1;//1正式服 2开发服 3测试网 4准现网
    public int appId = 7;
    public string keystore;
    public string keypass;
    public string keyaliname;
    public string keyalipass;
    public bool openPay = false;
    public string pay_systemname;
    public string pay_code;
    public string pay_appid;
    public string pay_comkey;
    public string pay_key;
    public string pay_vector;
    public string pay_return_url;
    public string pay_notice_url;

    static Setting mSetting;

    public static Setting setting
    {
        get
        {
            if (mSetting == null)
            {
                string str = FileUtils.getInstance().getString(FileUtils.getInstance().getFullPath("setting.json"));
                if (!string.IsNullOrEmpty(str))
                {
                    mSetting = UnityEngine.JsonUtility.FromJson<Setting>(str);
                }
                else
                    mSetting = new Setting();
            }

            return mSetting;
        }
    }
}