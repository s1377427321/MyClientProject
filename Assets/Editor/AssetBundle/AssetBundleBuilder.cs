using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sean.Editor;
using AssetBundles;

public class AssetBundleBuilder : EditorWindow
{
    private AssetBuildMrg mrg = new AssetBuildMrg();
    private List<AssetItem> mBundleList = new List<AssetItem>();

    private VersionCtrl.Project project;

    private static GUIContent
            insertContent = new GUIContent("+", "添加变量"),
            browse = new GUIContent("浏览", "浏览文件夹"),
            deleteContent = new GUIContent("-", "删除变量");

    private static GUILayoutOption buttonWidth = GUILayout.MaxWidth(20f);

    private AssetItem temp = new AssetItem();

    string openFolder()
    {
        var path = EditorUtility.OpenFolderPanel("选择目录", Application.dataPath, "");
        if (!string.IsNullOrEmpty(path))
        {
            path = FileUtils.getLinuxPath(path);
            if (path.IndexOf(Application.dataPath + "/") == -1)
            {
                EditorUtility.DisplayDialog("tips", "请选择Assets下的目录", "OK");
                return "";
            }
            path = path.Replace(Application.dataPath + "/", "Assets/");
        }
        return path;
    }


    void OnGUI()
    {
        var json = EditorTools.loadObjectFromJsonFile<AssetBuildMrg>(AssetBundlesMenuItems.bundleListPath);
        if (json != null)
        {
            mrg = json;
            mBundleList = json.list;
        }
        project = EditorTools.loadObjectFromJsonFile<VersionCtrl.Project>(AssetBundlesMenuItems.projectPath);

        GUILayout.Space(5);
        EditorGUILayout.LabelField("版本号:" + project.version);
        GUILayout.Space(5);
        EditorTools.DrawSeparator();
        EditorGUILayout.LabelField("需要打包的文件");
        EditorTools.DrawSeparator();
        EditorTools.BeginContents();
        GUILayout.Space(5);
        for (int i = 0; i < mBundleList.Count; i++)
        {
            if (i % 2 == 0) GUI.backgroundColor = Color.cyan;
            else GUI.backgroundColor = Color.magenta;
            drawLine(mBundleList[i]);
        }
        EditorTools.EndContents();
        GUILayout.Space(5);
        GUI.backgroundColor = Color.yellow;
        EditorTools.BeginContents();
        GUI.backgroundColor = Color.white;
        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        temp.type = (AssetType)EditorGUILayout.EnumPopup(temp.type);
        GUILayout.Label(temp.path, "HelpBox", GUILayout.Height(16f), GUILayout.Width(250));
        if (GUILayout.Button(browse, EditorStyles.miniButtonLeft, GUILayout.MaxWidth(80f)))
        {
            var path = openFolder();
            temp.path = path;
        }
        temp.root = EditorGUILayout.TextField(new GUIContent(temp.root), temp.root, GUILayout.Height(16f), GUILayout.Width(250));
        if (GUILayout.Button(insertContent, EditorStyles.miniButtonLeft, buttonWidth))
        {
            if (!string.IsNullOrEmpty(temp.path))
            {
                mBundleList.Add(temp);
                saveSetting();
            }
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);
        EditorTools.EndContents();
        GUI.backgroundColor = Color.white;

        EditorTools.DrawSeparator();
        EditorTools.BeginContents();
        GUILayout.Space(5);
        string old = mrg.output;
        mrg.output = EditorGUILayout.TextField(new GUIContent("输出"), mrg.output);

        if (mrg.output != old)
        {
            saveSetting();
        }

        GUILayout.Space(5);

        var buildHall = new GUIContent("打大厅", "打包资源");
        if (GUILayout.Button(buildHall, EditorStyles.miniButtonLeft, GUILayout.MinWidth(180f)))
        {
            AssetBundlesMenuItems.BuildAllNew(mBundleList, mrg, project, false);
        }

        GUILayout.Space(5);
        var buildAll = new GUIContent("打所有包", "打包资源");


        if (GUILayout.Button(buildAll, EditorStyles.miniButtonLeft, GUILayout.MinWidth(180f)))
        {
            AssetBundlesMenuItems.BuildAllNew(mBundleList, mrg, project);
        }
        GUILayout.Space(5);

        var rebuildAll = new GUIContent("重新打包", "打包资源");

        if (GUILayout.Button(rebuildAll, EditorStyles.miniButtonLeft, GUILayout.MinWidth(180f)))
        {
            FileUtil.DeleteFileOrDirectory(mrg.output + "/" + FileUtils.getInstance().getRuntimePlatform() + "/current");
            AssetBundlesMenuItems.BuildAllNew(mBundleList, mrg, project);
        }
        GUILayout.Space(5);
        EditorTools.EndContents();

    }

    private void drawLine(AssetItem dictionary)
    {
        EditorGUILayout.BeginHorizontal();
        var tp = dictionary.type;
        dictionary.type = (AssetType)EditorGUILayout.EnumPopup(dictionary.type);
        if (tp != dictionary.type)
        {
            saveSetting();
        }
        GUILayout.Label(dictionary.path, "HelpBox", GUILayout.Height(16f), GUILayout.Width(250));
        if (GUILayout.Button(browse, EditorStyles.miniButtonLeft, GUILayout.MaxWidth(80f)))
        {
            var path = openFolder();
            if (!string.IsNullOrEmpty(path))
            {
                dictionary.path = path;
                saveSetting();
            }
        }
        dictionary.root = EditorGUILayout.TextField(new GUIContent(dictionary.root), dictionary.root, GUILayout.Height(16f), GUILayout.Width(250));
        saveSetting();

        if (GUILayout.Button(deleteContent, EditorStyles.miniButtonLeft, buttonWidth))
        {
            mBundleList.Remove(dictionary);
            if (!string.IsNullOrEmpty(dictionary.path))
                AssetBundlesMenuItems.SetAssetBundleName(dictionary.path, "t:" + dictionary.type.ToString(), dictionary.root, true);
            saveSetting();
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);

    }

    private void saveSetting()
    {
        mrg.list = mBundleList;
        EditorTools.saveObjectToJsonFile(mrg, AssetBundlesMenuItems.bundleListPath);
    }
}