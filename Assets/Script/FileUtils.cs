using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using System.Security.Cryptography;
using ICSharpCode.SharpZipLib.Zip;
using System.Text.RegularExpressions;

public enum BuildPlatform
{
    WebGL,
    Standalones,
    IOS,
    Android,
    WP8,
    uwp
}

public class FileUtils {

    static FileUtils s_sharedFileUtils;
    private Dictionary<string, string> _pathCache = new Dictionary<string, string>();
    static public FileUtils getInstance()
    {
        if(s_sharedFileUtils == null) s_sharedFileUtils = new FileUtils();
        return s_sharedFileUtils;
    }

    private string streamingAssetsPath;
    private string persistentDataPath;
    private FileUtils()
    {
        streamingAssetsPath = Application.streamingAssetsPath;
        persistentDataPath = Application.persistentDataPath;
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
            return persistentDataPath + "/root/";
        }
        return persistentDataPath + "/root/" + gameName + "/";
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
        return isFileExistsAndroid(filePath);
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

    public AssetBundle getAssetBundle(string path)
    {
        var p = getAssetBundleFilePath(path);
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

    public AssetBundleCreateRequest getAssetBundleFromFileAsync(string path)
    {
        path = path.ToLower();
        var p = getAssetBundleFilePath(path);
        if (string.IsNullOrEmpty(p)) return null;
        return AssetBundle.LoadFromFileAsync(p);
    }

    public string getAssetBundleFilePath(string path)
    {
        string name = path.Substring(0, path.IndexOf("/"));
        if (name.ToLower() == Const.GAME_COMMON_NAME.ToLower() || name.ToLower() == "common")
        {
            name = Const.GAME_COMMON_NAME;
        }
        else
        {
            name = Const.GAME_CURRENT_NAME;
        }
        return getAssetBundleFilePathByGame(name, path);
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

    public static string getLinuxPath(string path)
    {
#if UNITY_EDITOR
        return Regex.Replace(path, "\\\\", "/");
#else
        return path;
#endif
    }

    /// <summary>
    /// 从文件读二进制
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public byte[] getBytes(string fileName)
    {
        if (!isFileExist(fileName))
        {
            return null;
        }
#if !UNITY_EDITOR && UNITY_ANDROID
        return getBytesAndroid(fileName);
#else
        return File.ReadAllBytes(fileName);
#endif
    }

    /// <summary>
    /// 解压zip文件
    /// </summary>
    /// <param name="zip"></param>
    /// <returns></returns>
    public bool unZip(string zip)
    {
        ZipConstants.DefaultCodePage = System.Text.Encoding.UTF8.CodePage;
        string rootPath = System.IO.Path.GetDirectoryName(zip);
        if (!isFileExist(zip)) return false;
        // 开始解压
        //FastZipEvents events = new FastZipEvents();
        //events.Progress = onProgress;
        FastZip fast = new FastZip();
        fast.ExtractZip(zip, rootPath, "");
        return true;
    }

    public bool removeFile(string file)
    {
        if (isFileExist(file))
        {
#if !UNITY_EDITOR
            if (file.IndexOf(streamingAssetsPath) == -1)
#endif
            {
                File.Delete(file);
                return true;
            }
        }
        return false;
    }

    public string GetMd5HashFromFile(string fileName)
    {
        try
        {
            if (!File.Exists(fileName))
                return "";
            FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
        catch (Exception e)
        {
            Debug.LogError("GetMd5HashFromFile fail,error: " + e.Message);
        }
        return "";
    }

    public bool renameFile(string path, string oldFile, string newFile)
    {
        string _old = path + oldFile;
        string _new = path + newFile;
        try
        {
            if (isFileExist(_old))
            {
                removeFile(_new);
            }
            else
                return false;
            File.Move(_old, _new);
            return true;
        }
        catch (IOException e)
        {
            Debug.LogError(e.ToString());
        }
        Debug.LogError("can't found " + _old);
        return false;
    }

    /// <summary>
    /// 遍历文件夹下所有文件。
    /// </summary>
    /// <param name="path"></param>
    /// <param name="searchPattern"></param>
    /// <param name="callBack"></param>
    public void ForEachDirectory(string path, string searchPattern, Action<string> callBack)
    {
        DirectoryInfo info = new DirectoryInfo(path);
        if (!info.Exists)
        {
            return;
        }
        FileInfo[] files = info.GetFiles(searchPattern, SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            callBack(getLinuxPath(files[i].FullName));
        }

    }

    public string getRuntimePlatform()
    {
        string pf = "";
#if UNITY_EDITOR
        switch (UnityEditor.EditorUserBuildSettings.activeBuildTarget)
        {
            case UnityEditor.BuildTarget.StandaloneLinux:
            case UnityEditor.BuildTarget.StandaloneLinux64:
            case UnityEditor.BuildTarget.StandaloneLinuxUniversal:
            case UnityEditor.BuildTarget.StandaloneOSXIntel:
                pf = BuildPlatform.IOS.ToString();
                break;
            case UnityEditor.BuildTarget.StandaloneWindows:
            case UnityEditor.BuildTarget.StandaloneWindows64:
                pf = BuildPlatform.Standalones.ToString();
                //pf = BuildPlatform.Android.ToString();
                break;
            case UnityEditor.BuildTarget.WebGL:
                pf = BuildPlatform.WebGL.ToString();
                break;
#if UNITY_5
            case UnityEditor.BuildTarget.iOS:
#else
            case UnityEditor.BuildTarget.iPhone:
#endif
                pf = BuildPlatform.IOS.ToString();
                break;
            case UnityEditor.BuildTarget.Android:
                pf = BuildPlatform.Android.ToString();
                break;
            case UnityEditor.BuildTarget.WSAPlayer:
                pf = BuildPlatform.uwp.ToString();
                break;
            default:
                Debug.LogError("Internal error. Bundle Manager dosn't support for platform " + UnityEditor.EditorUserBuildSettings.activeBuildTarget);
                pf = BuildPlatform.Standalones.ToString();
                break;
        }
#else
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.OSXPlayer:
                pf = BuildPlatform.Standalones.ToString();
                break;
            case RuntimePlatform.OSXWebPlayer:
            case RuntimePlatform.WindowsWebPlayer:
                pf = BuildPlatform.WebGL.ToString();
                break;
            case RuntimePlatform.IPhonePlayer:
                //IOS
                pf = BuildPlatform.IOS.ToString();
                break;
            case RuntimePlatform.Android:
                //安卓
                pf = BuildPlatform.Android.ToString();
                break;

            case RuntimePlatform.WSAPlayerARM:
            case RuntimePlatform.WSAPlayerX64:
            case RuntimePlatform.WSAPlayerX86:
                //Win10
               // pf = BuildPlatform.Win10.ToString();
                break;
            default:
                Debug.LogError("Platform " + Application.platform + " is not supported by BundleManager.");
                pf = BuildPlatform.Standalones.ToString();
                break;
        }
#endif
        return pf.ToLower();

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
