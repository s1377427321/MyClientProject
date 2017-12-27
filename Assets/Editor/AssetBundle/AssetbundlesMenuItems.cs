using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Sean.Editor;

namespace AssetBundles
{
    public enum AssetType
    {
        Prefab,
        Texture,
        Shader,
        Material,
        Font,
        Scene,
        AudioClip,
        AnimationClip,
        AnimatorController,
        AudioMixer,
        Asset,
        SkeletonDataAsset,
        emojidata,
        All
    }

    public class AssetItem
    {
        public string path;
        public AssetType type = AssetType.Prefab;
        public string root;
    }

    [System.Serializable]
    public class AssetBuildMrg
    {
        public List<AssetItem> list = new List<AssetItem>();
        public string output = "AssetBundles";
    }


    public class AssetBundlesMenuItems
    {
        public static string projectPath
        {
            get
            {
                return Application.streamingAssetsPath + "/project.manifest";
            }
        }

        public static string bundleListPath
        {
            get
            {
                return Application.dataPath + "/../editor_config/bundle_list.json";
            }
        }


        [MenuItem("[AssetBundle]/Open")]
        public static void Open()
        {
            EditorWindow.GetWindow<AssetBundleBuilder>();
        }

        public static void SetAssetBundleName(string path, string type, string root, bool isClear)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            if ("t:" + AssetType.All.ToString() == type)
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
                foreach (Object o in Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets))
                {
                    string p = AssetDatabase.GetAssetPath(o);
                    if (p.IndexOf(path) == -1) continue;
                    setAssetBundleName(p, path, root, isClear);
                }
                AssetDatabase.SaveAssets();
                return;
            }

        }

        static void setAssetBundleName(string p, string path, string root, bool isClear)
        {
            if (Directory.Exists(p)) return;
            var pp = p;
            if (isClear)
            {
                p = "";
            }
            else
            {
                p = p.Replace("Assets/", "");
                if (string.IsNullOrEmpty(root))
                {
                }
                else
                {
                    //空格
                    if (string.IsNullOrEmpty(root.Trim()))
                    {
                        p = p.Substring(0, p.LastIndexOf('/'));
                    }
                    else
                    {
                        p = p.Substring(0, p.LastIndexOf('/') + 1) + root;
                    }
                }
            }

            var meta = pp + ".meta";
            if (File.Exists(meta))
            {
                ///快速检查，加快打包速度
                string[] line = File.ReadAllLines(meta);
                for (int i = 0; i < line.Length; i++)
                {
                    var l = line[i];
                    if (l.IndexOf("assetBundleName:") > -1)
                    {
                        l = l.Replace("assetBundleName:", "").Trim();
                        if (string.IsNullOrEmpty(l))
                        {
                            if (isClear)
                                return;
                        }
                        else if (l == p.ToLower())
                            return;

                    }
                }
            }
            AssetImporter ai = AssetImporter.GetAtPath(pp);
            if (!ai)
            {
                Debug.LogFormat("文件错误：{0}", pp);
                return;
            }
            ai.assetBundleName = p;
        }


        public static void BuildAllNew(List<AssetItem> mBundleList, AssetBuildMrg mrg, VersionCtrl.Project project, bool all = true)
        {
         
            int ret = 0;
            int count = 3;
            if (all)
            {
                List<string> ls = PathSetting.loadList(PathSetting.copy_list);
                count = ls.Count * 3 + count;
                foreach (string s in ls)
                {
                    var s1 = s.Replace("\r", "").Replace("\n", "");
                    ret = BuildGame(mBundleList, mrg, project, s1.ToUpper(), ret, count);
                }
            }

            ret = BuildGame(mBundleList, mrg, project, "HALL", ret, count);

            BuildWindow.GenMd5(all);
            Debug.Log("打包完成！！！！！");
        }

        static public int BuildGame(List<AssetItem> mBundleList, AssetBuildMrg mrg, VersionCtrl.Project project, string name, int ret, int count)
        {
            clearAllName();
            //清空掉非本游戏的名字

            for (int i = 0; i < mBundleList.Count; i++)
            {
                var item = mBundleList[i];
                if (item != null && !string.IsNullOrEmpty(item.path))
                {
                    string p = item.path.ToLower();
                    if (p.StartsWith("assets/" + name.ToLower()) || p.StartsWith("assets/" + "common"))
                    {
                        //设置名字
                        SetAssetBundleName(item.path, "t:" + item.type.ToString(), item.root, false);
                    }
                }
            }
            string[] paths = { "ui/Animator", "ui/Font", "ui/Prefab", "ui/Audio/music", "ui/Atlas", "ui/Texture", "ui/Audio/sound" };
            AssetType[] types = { AssetType.AnimatorController, AssetType.Font, AssetType.Prefab, AssetType.AudioClip, AssetType.Texture, AssetType.Texture, AssetType.AudioClip };
            string[] roots = { "", "", "", "", " ", " ", " " };
            for (int i = 0; i < paths.Length; i++)
            {
                string p = "assets/" + name.ToLower() + "/" + paths[i].ToLower();
                if (paths[i] == "ui/Audio/sound")
                {
                    p = p + "_" + name.ToLower();
                }

                //设置名字
                SetAssetBundleName(p, "t:" + types[i].ToString(), roots[i], false);
            }

            ret = ret + 1;
            EditorUtility.DisplayProgressBar("打包", "打包游戏" + name, ret * 1.0f / count);

            string outputPath
                    = Path.Combine(
                        Path.Combine(
                            Path.Combine(
                                Path.Combine(mrg.output,
                                    FileUtils.getInstance().getRuntimePlatform()),
                             "current"),
                        name),
                      name.ToLower() + "assetbundle");
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            //根据BuildSetting里面所激活的平台进行打包 设置过AssetBundleName的都会进行打包  
            BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.DeterministicAssetBundle, EditorUserBuildSettings.activeBuildTarget);

            ret = ret + 1;
            EditorUtility.DisplayProgressBar("打包", "打包游戏" + name, ret * 1.0f / count);
            var versionDir = Path.Combine(
                                Path.Combine(
                                    Path.Combine(mrg.output,
                                        FileUtils.getInstance().getRuntimePlatform()),
                                    project.version),
                                name);
            var versionpath = Path.Combine(versionDir,
                      name.ToLower() + "assetbundle");
            CopyToVersion(outputPath, versionpath);
            if (name.ToLower() != "hall")
            {
                FileUtil.DeleteFileOrDirectory(versionpath + "/" + "common");
                FileUtil.DeleteFileOrDirectory(versionpath + "/" + "hall");
            }

            ret = ret + 1;
            EditorUtility.DisplayProgressBar("打包", "打包游戏" + name, ret * 1.0f / count);
            return ret;

        }

        static void CopyToVersion(string src, string tar)
        {
            BuildWindow.CopyDirectory(src, tar, false);
        }

        static void clearAllName()
        {
            List<string> ls = PathSetting.loadList(PathSetting.copy_list);
            foreach (string s in ls)
            {
                var s1 = s.Replace("\r", "").Replace("\n", "");
                string path = "Assets/" + s1.ToUpper();
                DirectoryInfo info = new DirectoryInfo(path);
                SetAssetBundleNameNull(info);
            }
            //DirectoryInfo info2 = new DirectoryInfo("Assets/HALL");
            //SetAssetBundleNameNull(info2);
        }

        static void SetAssetBundleNameNull(DirectoryInfo dirInfo)
        {
            FileSystemInfo[] files = dirInfo.GetFileSystemInfos();
            foreach (FileSystemInfo file in files)
            {
                if (file is FileInfo && file.Extension != ".meta" && file.Extension != ".txt")
                {
                    string filePath = file.FullName.Replace('\\', '/');
                    filePath = filePath.Replace(Application.dataPath, "Assets");
                    AssetImporter ai = AssetImporter.GetAtPath(filePath);
                    ai.assetBundleName = null;
                }
                else if (file is DirectoryInfo)
                {
                    string filePath = file.FullName.Replace('\\', '/');
                    filePath = filePath.Replace(Application.dataPath, "Assets");
                    AssetImporter ai = AssetImporter.GetAtPath(filePath);
                    ai.assetBundleName = null;
                    SetAssetBundleNameNull(file as DirectoryInfo);
                }
            }
        }

    }

   
}
