using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CacheGameObject  {
    private Dictionary<string, GameObject> mDict = new Dictionary<string, GameObject>();

    public void Add(string path, GameObject go)
    {
        path = path.ToLower();
        if (mDict.ContainsKey(path))
        {
            mDict[path] = go;
        }
        else
        {
            mDict.Add(path, go);
        }
    }

    public GameObject Get(string path)
    {
        path = path.ToLower();
        if (mDict.ContainsKey(path))
        {
            return mDict[path];
        }
        return null;
    }

    public void Clear()
    {
        mDict.Clear();
    }
}
