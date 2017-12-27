using System;
using System.Collections.Generic;

namespace VersionCtrl
{
    public class Version
    {
        public Version()
        {
            groupVersions = new List<string>();
        }

        protected string root;
        public string packageUrl { get; set; }
        public string projectUrl { get; set; }
        public string versionUrl { get; set; }
        public string version { get; set; }
        public List<string> groupVersions { get; set; }
        public void setRoot(string path)
        {
            root = path;
        }
        public bool isLoaded()
        {
            return !string.IsNullOrEmpty(version);
        }

        public string getLastVersion()
        {
            if (groupVersions.Count == 0) return version;
            return groupVersions[groupVersions.Count - 1];
        }

        /// <summary>
        /// 对比版本
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>a>=b 返回true</returns>
        public bool versionEquals(string a, string b)
        {
            if (a == b) return true;
            var arg1 = a.Split('.');
            var arg2 = b.Split('.');
            var len = Math.Min(arg1.Length, arg2.Length);
            for (var i = 0; i < len; i++)
            {
                var num1 = Convert.ToInt32(arg1[i]);
                var num2 = Convert.ToInt32(arg2[i]);

                if (num1 < num2) return false;
                else if (num1 > num2) return true;
            }
            if (arg2.Length > arg1.Length)
                return false;
            return true;
        }

        public bool versionEquals(Version target)
        {
            if (version != target.version && versionEquals(getLastVersion(), target.getLastVersion())) return true;
            if (target.groupVersions.Count != groupVersions.Count) return false;
            for (int i = 0; i < groupVersions.Count; i++)
            {
                if (groupVersions[i] != target.groupVersions[i]) return false;

            }
            return true;
        }
    }
}