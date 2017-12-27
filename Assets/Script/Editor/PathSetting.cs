using System.Collections;
using System.Collections.Generic;

namespace Sean.Editor
{
    public class PathSetting 
    {
        public string[] needCopy;
        public const string copy_list = "editor_config/copy_list.txt";
        public const string copyed_list = "editor_config/copyed_list.txt";

        public static List<string> loadList(string path)
        {
            var str = FileUtils.getInstance().getString(path);
            if (string.IsNullOrEmpty(str)) return new List<string>();
            string[] ps = str.Split('\n');
            List<string> l = new List<string>();
            for (int i = 0; i < ps.Length; i++)
            {
                l.Add(ps[i].Replace("\r", ""));
            }
            return l;
        }

        public static void saveList(string path, List<string> paths)
        {
            var str = string.Join("\n", paths.ToArray());
            FileUtils.getInstance().writeString(path, str);
        }
    }
}
