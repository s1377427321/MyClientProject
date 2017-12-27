using LuaMgr;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VersionCtrl;

namespace Sean.Editor
{
    public class BuildWindow
    {
        private static string settingPath
        {
            get
            {
                return Application.streamingAssetsPath + "/setting.json";
            }
        }
        private static string projectPath
        {
            get
            {
                return Application.streamingAssetsPath + "/project.manifest";
            }
        }

        public static void CopyDirectory(string sourcePath, string destinationPath, bool filter = true)
        {
            sourcePath = sourcePath.Replace("\r", "").Replace("\n", "");
            destinationPath = destinationPath.Replace("\r", "").Replace("\n", "");
            if (!Directory.Exists(sourcePath))
                return;
            DirectoryInfo info = new DirectoryInfo(sourcePath);
            FileUtils.getInstance().createDirectory(destinationPath);
            foreach (FileSystemInfo fsi in info.GetFileSystemInfos())
            {
                string destName = Path.Combine(destinationPath, fsi.Name);
                FileUtils.getInstance().removeDirectory(destName);
                if (fsi is System.IO.FileInfo)
                {
                    if (filter)
                    {
                        if (fsi.FullName.IndexOf(".manifest") > 0) continue;
                    }
                    FileInfo dinfo = new FileInfo(destName);
                    FileInfo sinfo = (FileInfo)fsi;
                    if (!dinfo.Exists || sinfo.Length != dinfo.Length || sinfo.LastWriteTime != dinfo.LastWriteTime)
                    {
                        File.Copy(fsi.FullName, destName, true);
                    }
                }
                else
                {
                    FileUtils.getInstance().createDirectory(destName);
                    CopyDirectory(fsi.FullName, destName);
                }
            }
        }

        public static void GenMd5(bool all)
        {
            clear();

            if (!copyAndZip(GetOS(), all))
                return;

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        public static string GetOS()
        {
            string os = "";
            switch (UnityEditor.EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.Android:
                    os = "android";
                    break;
                case BuildTarget.iOS:
                    os = "ios";
                    break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    os = "standalones";
                    break;
            }
            return os;
        }

        static void clear()
        {
            string path = "Assets/StreamingAssets";
            DirectoryInfo d = new DirectoryInfo(path);
            DirectoryInfo[] ds = d.GetDirectories();

            for (int i = 0; i < ds.Length; i++)
            {
                string p = ds[i].FullName;
                FileUtils.getInstance().removeDirectory(p);
                FileUtils.getInstance().removeFile(p + ".meta");
                FileUtils.getInstance().removeFile(Application.streamingAssetsPath + "/" + p + ".meta");
            }
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// 按平台复制文件
        /// </summary>
        /// <param name="pf">Android|IOS</param>
        private static bool copyAndZip(string pf, bool all = true)
        {
            var pro = EditorTools.loadObjectFromJsonFile<Project>(projectPath);
            string version = pro.version;
            if (all)
            {
                List<string> _copyList = PathSetting.loadList(PathSetting.copy_list);
                for (int i = 0; i < _copyList.Count; i++)
                {
                    var p = _copyList[i].Replace("\r", "");
                    copyOneAndZip(p, pf, version);
                }
            }
            copyOneAndZip("hall", pf, version);
            return true;
        }

        private static void copyOneAndZip(string p, string pf, string version)
        {
            if (string.IsNullOrEmpty(p.Trim()))
            {
                return;
            }
            var path = "AssetBundles/" + pf + "/" + version + "/" + p;

            FileUtils.getInstance().removeDirectory(path + "/lua");
            if (p == "hall")
            {
                CopyDirectory("lua/HALL", path + "/lua/HALL");
                CopyDirectory("lua/common", path + "/lua/common");
            }
            else
            {
                CopyDirectory("lua/" + p.ToUpper(), path + "/lua/" + p.ToUpper());
            }
            zipLua(path + "/lua");

            CopyDirectory(path, Application.streamingAssetsPath + "/" + p);
        }

        public static void zipLua(string path)
        {
            EditorUtility.DisplayProgressBar("打包Lua", "正在加密lua", 0.5f);

            FileUtils.getInstance().ForEachDirectory(path, "*.lua", (filePath) =>
            {
                byte[] data = File.ReadAllBytes(filePath);
                data = LuaHelper.AecEnCode(data);
                File.WriteAllBytes(filePath, data);
                EditorUtility.DisplayProgressBar("打包Lua", "正在加密：" + Path.GetFileNameWithoutExtension(filePath), 0.5f);

            });
            EditorUtility.ClearProgressBar();
        }
    }
}
