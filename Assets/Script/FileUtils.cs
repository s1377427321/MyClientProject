using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;

public class FileUtils {

    static FileUtils s_sharedFileUtils;
    private Dictionary<string, string> _pathCache = new Dictionary<string, string>();
    static public FileUtils getInstance()
    {
        if(s_sharedFileUtils == null) s_sharedFileUtils = new FileUtils();
        return s_sharedFileUtils;
    }

    private string streamingAssetsPath;
    private string persistenDataPath;
    private FileUtils()
    {
        streamingAssetsPath = Application.streamingAssetsPath;
        persistenDataPath = Application.persistentDataPath;
    }

    private List<string> _searchPathArray = new List<string>();

    static public void destroyInstance()
    {
        if (s_sharedFileUtils != null)
        {
            s_sharedFileUtils._searchPathArray.Clear();
            s_sharedFileUtils = null;
        }
    }

    public string getWritablePath(string gameName)
    {
        if (string.IsNullOrEmpty(gameName))
        {
            return persistenDataPath + "/root/";
        }
        return persistenDataPath + "/root/" + gameName + "/";
    }

    public string getString(string fileName)
    {
        if (!isFileExist(fileName))
        {
            return null;
        }
        return File.ReadAllText(fileName);
    }

    public bool isFileExist(string filePath)
    {
#if !UNITY_EDITOR && UNITY_ANDROID
        
#else
        return File.Exists(filePath);
#endif
    }

    public bool removeDirectory(string dir)
    {
        if (isDirectoryExist(dir))
        {
            Directory.Delete(dir, true);
            return true;
        }
        return false;
    }

    public bool isDirectoryExist(string dir)
    {
        return Directory.Exists(dir);
    }

    public void createDirectory(string path)
    {
        if (!isDirectoryExist(path))
            Directory.CreateDirectory(path);
    }

    public bool writeString(string filepath, string data)
    {
        return writeFileWithCode(filepath, data, Encoding.UTF8);
    }

    public bool writeFileWithCode(string filepath, string data, Encoding code)
    {
        try
        {
            string path = Path.GetDirectoryName(filepath);
            createDirectory(path);

            if (code != null)
            {
                File.WriteAllText(filepath, data, code);
            }
            else
            {
                File.WriteAllText(filepath, data);
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("writeFIle fail. " + filepath);
            throw e;
        }
    }

    public void ClearCache()
    {
        _pathCache.Clear();
    }
    public List<string> getSearchPaths()
    {
        return _searchPathArray;
    }

    public void setSearchPaths(List<string> searchPaths)
    {
        _searchPathArray = searchPaths;
    }

    public void genStreamPath()
    {
        var _fileUtils = FileUtils.getInstance();
        _fileUtils.ClearCache();
        var searchPath = new List<string>();

        searchPath.Add(Application.streamingAssetsPath);

        searchPath.Add(Path.Combine(Application.streamingAssetsPath, Const.GAME_CURRENT_NAME.ToLower()));

        _fileUtils.setSearchPaths(searchPath);
    }

    public void genSearchPath()
    {
        var _fileUtils = FileUtils.getInstance();
        _fileUtils.ClearCache();
        var searchPath = new List<string>();

        searchPath.Add(Application.streamingAssetsPath);

        searchPath.Add(Path.Combine(Application.streamingAssetsPath, Const.GAME_CURRENT_NAME.ToLower()));

        var root = _fileUtils.getWritablePath(Const.GAME_CURRENT_NAME);

        searchPath.Insert(0, Application.persistentDataPath);

        searchPath.Insert(0, root);

#if UNITY_EDITOR && UNITY_STANDALONE
        if (!Setting.setting.update)
        {
            string relativePath = System.Environment.CurrentDirectory.Replace("\\", "/");
            searchPath.Insert(0, relativePath);
        }
#else
        string relativePath = System.Environment.CurrentDirectory.Replace("\\", "/");
        searchPath.Insert(0, relativePath);
#endif
        _fileUtils.setSearchPaths(searchPath);
    }


    public AssetBundle getRootAssetBundle(string path)
    {
        var p = getRootAssetBundleFilePath(path);
        return getAssetBundleByFullPath(p);
    }

    AssetBundle getAssetBundleByFullPath(string p)
    {
        if (string.IsNullOrEmpty(p))
        {
            return null;
        }

        try
        {
            return AssetBundle.LoadFromFile(p);
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString() + "-->" + p);
        }
        return null;
    }

    string getRootAssetBundleFilePath(string path)
    {
        return getAssetBundleFilePathByGame(Const.GAME_CURRENT_NAME, path);
    }

    string getAssetBundleFilePathByGame(string name, string path)
    {
        string n = name.ToLower() + "assetbundle/" + path;
        var p = getFullPath(n);
        if (string.IsNullOrEmpty(p))
        {
            Debug.LogError("can not find file " + n);
        }
#if !UNITY_EDITOR && UNITY_ANDROID
        if (n == p)
        {
            p = Application.dataPath + "!assets/" + p;
        }
#endif
        return p;
    }


    public string getFullPath(string fileName)
    {
        if (_pathCache.ContainsKey(fileName)) return _pathCache[fileName];

        for (int i = 0; i < _searchPathArray.Count; i++)
        {
            string path = _searchPathArray[i];
            if (isRoot(path, fileName))
                continue;
            fixedPath(ref path);
            var p = path + fileName;
            if (isFileExist(p))
            {
                _pathCache.Add(fileName, p);
                return p;
            }
        }
        if (isFileExist(fileName))
        {
            _pathCache.Add(fileName, fileName);
            return fileName;
        }
        return "";
    }

    private bool isRoot(string path, string fileName)
    {
        bool ret = false;
        if (Path.GetDirectoryName(fileName).IndexOf(path) > -1)
        {
            ret = true;
        }
        return ret;
    }

    private void fixedPath(ref string path)
    {
        if (!path.EndsWith("/"))
        {
            path = path + "/";
        }
    }

#if !UNITY_EDITOR && UNITY_ANDROID
    private AndroidJavaClass _helper;
    private AndroidJavaClass helper
    {
        get
        {
            if (_helper != null) return _helper;
            _helper = new AndroidJavaClass("sean.unity.helper.Unity3dHelper");
            using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                object jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
                _helper.CallStatic("init", jo);
            }
            return _helper;
        }
    }
    private byte[] getBytesAndroid(string path)
    {
        if (path.IndexOf(streamingAssetsPath) > -1)
        {
            path = path.Replace(streamingAssetsPath + "/", "");
        }
        else if (path.IndexOf(persistentDataPath) > -1)
        {
            return File.ReadAllBytes(path);
        }
        return helper.CallStatic<byte[]>("getBytes", path);
    }
    private string getStringAndroid(string path)
    {
        if (path.IndexOf(streamingAssetsPath) > -1)
        {
            path = path.Replace(streamingAssetsPath + "/", "");
        }
        else if (path.IndexOf(persistentDataPath) > -1)
        {
            return File.ReadAllText(path);
        }
        return helper.CallStatic<string>("getString", path);
    }
    private bool isFileExistsAndroid(string path)
    {
        if(path.IndexOf(streamingAssetsPath) > -1)
        {
            path = path.Replace(streamingAssetsPath + "/", "");
        }
        else if(path.IndexOf(persistentDataPath) > -1)
        {
            return File.Exists(path);
        }
        return helper.CallStatic<bool>("isFileExists", path);
    }

#endif
}
