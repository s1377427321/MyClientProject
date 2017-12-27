using UnityEngine;
using System.Collections.Generic;
using System;
using TinyJSON;

namespace VersionCtrl
{
    public class Project : Version
    {

        public string name { get; set; }
        public Project()
        {
            assets = new Dictionary<string, AssetItem>();
        }
        public string[] searchPaths { get; set; }
        public Dictionary<string, AssetItem> assets { get; set; }


        /// <summary>
        /// 对比资源有无更改
        /// </summary>
        /// <param name="remoteProject"></param>
        /// <returns></returns>
        internal bool assetsEquals(Project remoteProject)
        {
            var ret = true;
            var remoteAssets = remoteProject.assets;
            FileUtils fileUtils = FileUtils.getInstance();
            foreach (var it in assets)
            {
                var value = it.Value;
                if (!remoteAssets.ContainsKey(it.Key))
                {
                    ret = false;
                    fileUtils.removeFile(root + value.path);
                    fileUtils.removeFile(root + value.path + Downloader.TEMP);
                    break;
                }
                var assetItem = remoteAssets[it.Key];
                if (value.md5 != assetItem.md5)
                {
                    ret = false;
                    fileUtils.removeFile(root + value.path);
                    fileUtils.removeFile(root + value.path + Downloader.TEMP);
                    break;
                }
                if (value.path != assetItem.path)
                {
                    ret = false;
                    fileUtils.removeFile(root + value.path);
                    fileUtils.removeFile(root + value.path + Downloader.TEMP);
                    break;
                }
            }
            return ret;
        }

        internal void genResumeAssetsList(ref Dictionary<string, DownloadUnit> units)
        {
            foreach (var it in assets)
            {
                AssetItem asset = it.Value;
                if (asset.downloadState != DownloadState.Successed)
                {
                    DownloadUnit unit = getDownloadItem(asset);
                    units.Add(unit.customId, unit);
                }
            }
        }

        internal DownloadUnit getDownloadItem(AssetItem asset)
        {
            DownloadUnit unit = new DownloadUnit();
            unit.customId = asset.group;
            unit.srcUrl = packageUrl + FileUtils.getInstance().getRuntimePlatform().ToLower() + "/files/" + asset.path;
            unit.storagePath = root + asset.path;
            unit.downloadState = asset.downloadState;
            unit.size = asset.size;
            unit.md5 = asset.md5;
            return unit;
        }

        internal Dictionary<string, AssetDiff> genDiff(Project remoteProject)
        {
            Dictionary<string, AssetDiff> diff_map = new Dictionary<string, AssetDiff>();
            var remoteAssets = remoteProject.assets;
            AssetItem valueA, valueB;
            string key = "";
            foreach (var it in assets)
            {
                key = it.Key;
                valueA = it.Value;
                if (!remoteAssets.ContainsKey(key))
                {
                    AssetDiff diff;
                    diff.asset = valueA;
                    diff.type = DiffType.DELETED;
                    diff_map.Add(key, diff);
                    continue;
                }

                valueB = remoteAssets[key];
                if (valueA.md5 != valueB.md5)
                {
                    AssetDiff diff;
                    diff.asset = valueB;
                    diff.type = DiffType.MODIFIED;
                    if (!versionEquals(getLastVersion(), key))
                        diff_map.Add(key, diff);
                }

            }

            foreach (var it in remoteAssets)
            {
                key = it.Key;
                valueB = it.Value;

                if (!assets.ContainsKey(key))
                {
                    AssetDiff diff;
                    diff.asset = valueB;
                    diff.type = DiffType.ADDED;
                    if (!versionEquals(getLastVersion(), key))
                        diff_map.Add(key, diff);
                }
            }

            return diff_map;
        }

        internal void setAssetDownloadState(string key, DownloadState state)
        {
            if (!assets.ContainsKey(key))
            {
                return;
            }
            assets[key].downloadState = state;
        }
    }


    public class AssetItem
    {
        public string path;
        public string md5;
        public long size;
        public string group;
        public DownloadState downloadState = DownloadState.UnStarted;
        public override string ToString()
        {
            Node json = Node.NewTable();
            json["path"] = this.path;
            json["size"] = this.size;
            json["md5"] = this.md5;
            json["group"] = this.group;
            return JSON.stringify(json);
        }
       
    }

    public enum DiffType
    {
        ADDED,
        DELETED,
        MODIFIED
    };
    public struct AssetDiff
    {
        public AssetItem asset;
        public DiffType type;
    }
}