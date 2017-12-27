using UnityEngine;
using System.Collections;

public class ResDestroy : MonoBehaviour
{
    public string m_abName;

    private void Awake()
    {
        if (string.IsNullOrEmpty(m_abName)) return;
        Utils.AssetBundleMgr.Instance.Add(m_abName);
    }

    void OnDestroy()
    {
        if (string.IsNullOrEmpty(m_abName)) return;
        Utils.AssetBundleMgr.Instance.Remove(m_abName);
    }
}
