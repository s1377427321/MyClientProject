using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace Sean.Editor
{
    public static class EditorTools 
    {
        static bool mEndHorizontal = false;

        static public T loadObjectFromJsonFile<T>(string path) where T : new()
        {
            if (!File.Exists(path))
                return new T();
            string str = File.ReadAllText(path);
            if (string.IsNullOrEmpty(str))
            {
                Debug.Log("Cannot find " + path);
                return new T();
            }
            T data = LitJson.JsonMapper.ToObject<T>(str);
            if (data == null)
            {
                Debug.Log("Cannot read data from " + path);
            }

            return data;
        }

        static public Texture2D blankTexture
        {
            get
            {
                return EditorGUIUtility.whiteTexture;
            }
        }

        /// <summary>
        /// Draw a visible separator in addition to adding some padding.
        /// </summary>

        static public void DrawSeparator()
        {
            GUILayout.Space(12f);

            if (Event.current.type == EventType.Repaint)
            {
                Texture2D tex = blankTexture;
                Rect rect = GUILayoutUtility.GetLastRect();
                GUI.color = new Color(0f, 0f, 0f, 0.25f);
                GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 4f), tex);
                GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 1f), tex);
                GUI.DrawTexture(new Rect(0f, rect.yMin + 9f, Screen.width, 1f), tex);
                GUI.color = Color.white;
            }
        }

        static public void BeginContents() { BeginContents(false); }

        static public void BeginContents(bool minimalistic)
        {
            if (!minimalistic)
            {
                mEndHorizontal = true;
                GUILayout.BeginHorizontal();
                EditorGUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(10f));
            }
            else
            {
                mEndHorizontal = false;
                EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(10f));
                GUILayout.Space(10f);
            }
            GUILayout.BeginVertical();
            GUILayout.Space(2f);
        }

        static public void saveObjectToJsonFile<T>(T data, string path)
        {
            string jsonStr = LitJson.JsonMapper.ToJson(data);

            writeFileWithCode(path, jsonStr, null);
        }

        static public bool writeFileWithCode(string filepath, string data, Encoding code)
        {
            try
            {
                string path = Path.GetDirectoryName(filepath);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
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
            catch (System.Exception e)
            {
                Debug.LogError("writeFIle fail. " + filepath);
                throw e;
            }
        }

        static public void EndContents()
        {
            GUILayout.Space(3f);
            GUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (mEndHorizontal)
            {
                GUILayout.Space(3f);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(3f);
        }

    }
}
