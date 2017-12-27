using UnityEngine;
using System.Collections;
using Utils;
using System;
using System.Collections.Generic;
using System.IO;
using SLua;

namespace VersionCtrl
{
    public class AssetsMgr : IUpdateHandler
    {

        public enum State
        {
            /// <summary>
            /// ���汾
            /// </summary>
            UnChecked,
            /// <summary>
            /// ���ذ汾�ļ�
            /// </summary>
            DownloadVersion,
            /// <summary>
            /// �汾�ļ�������
            /// </summary>
            VersionLoaded,
            /// <summary>
            /// ����Project�ļ�
            /// </summary>
            DownloadProject,
            /// <summary>
            /// Project�ļ�������
            /// </summary>
            ProjectLoaded,
            /// <summary>
            /// �Ƿ���Ҫ����
            /// </summary>
            NeedUpdate,
            /// <summary>
            /// ������
            /// </summary>
            Updating,
            /// <summary>
            /// �Ѿ������°汾
            /// </summary>
            UpToData,
            UpdateSuccess,
            /// <summary>
            /// ����ʧ��
            /// </summary>
            FailToUpdate,
            /// <summary>
            /// ��ѹʧ��
            /// </summary>
            DecompressFail,
            NewBigVersion,
        }
        private System.Action<AssetsMgr, State, int, string> onCallback;

        private string mFolderName;

        public const string PROJECT_FILENAME = "project.manifest";
        private const string VERSION_FILENAME = "version.manifest";
        public const string PROJECT_ID = "@manifest";
        public const string VERSION_ID = "@version";
        public const string TEMP_PROJECT_FILENAME = "project.manifest.temp";

        private FileUtils _fileUtils;
        private string _storagePath;
        private Project _localProject;
        private Version _remoteVersion;
        private Project _tempProject;
        string _cacheVersionPath;
        string _cacheProjectPath;
        string _tempProjectPath;
        State _updateState;
        Downloader mDownloader;
        string installVersion;
        private Dictionary<string, DownloadUnit> _downloadUnits = new Dictionary<string, DownloadUnit>();

        /// <summary>
        /// ��ѹ�ļ�
        /// </summary>
        List<string> _compressedFiles = new List<string>();



        [DoNotToLua]
        public AssetsMgr(System.Action<AssetsMgr, State, int, string> cb, string localProjectContent)
        {

        }

        [DoNotToLua]
        public AssetsMgr(string projectFolder, string storagePath, System.Action<AssetsMgr, State, int, string> cb)
        {
            mFolderName = Const.GAME_COMMON_NAME;
            Project localProject = Tool.loadObjectFromJsonFile<Project>(projectFolder + "/" + PROJECT_FILENAME);

            onCallback = cb;
            init(localProject, storagePath);
        }

        void init(Project localProject, string storagePath)
        {
            _fileUtils = FileUtils.getInstance();
            _fileUtils.createDirectory(storagePath);
            this._storagePath = storagePath;
            adjustPath(ref this._storagePath);

            initProject(localProject);

            mDownloader = new Downloader(this);
            _updateState = State.UnChecked;
        }

        private void initProject(Project localProject)
        {
            _localProject = localProject;
            _localProject.setRoot(_storagePath);
            installVersion = _localProject.getLastVersion();

            _cacheVersionPath = _storagePath + VERSION_FILENAME;
            _cacheProjectPath = _storagePath + PROJECT_FILENAME;
            _tempProjectPath = _storagePath + TEMP_PROJECT_FILENAME;

            if (_fileUtils.isFileExist(_cacheProjectPath))
            {
                var _cacheProject = Tool.loadObjectFromJsonFile<Project>(_cacheProjectPath);
                if (!_cacheProject.isLoaded())
                {
                    _fileUtils.removeFile(_cacheProjectPath);
                }
                else if (_localProject.version == _cacheProject.version)
                {
                    _localProject = _cacheProject;
                    _localProject.setRoot(_storagePath);
                }
            }

            _tempProject = Tool.loadObjectFromJsonFile<Project>(_tempProjectPath);
            if (!_tempProject.isLoaded())
            {
                _fileUtils.removeFile(_tempProjectPath);
            }
            else
            {
                _tempProject.setRoot(_storagePath);
            }
        }

        protected void adjustPath(ref string path)
        {
            if (!string.IsNullOrEmpty(path) && path[path.Length - 1] != System.IO.Path.DirectorySeparatorChar)
            {
                path += "/";
            }
        }

        /// <summary>
        /// ���ذ汾�ļ�
        /// </summary>
        private void downloadVersion()
        {
            if (_updateState != State.DownloadVersion) return;

            var url = _localProject.versionUrl + "/" + FileUtils.getInstance().getRuntimePlatform().ToLower() + "/configs/0/" + VERSION_FILENAME;

            mDownloader.downloadAsync(url, _cacheVersionPath, VERSION_ID);
        }

        /// <summary>
        /// ����project�ļ�
        /// </summary>
        private void downloadProject()
        {
            if (_updateState != State.DownloadProject) return;
            //var url = _localProject.projectUrl + "?pf=" + FileUtils.getInstance().getRuntimePlatform().ToLower();
            var url = _localProject.projectUrl + "/" + FileUtils.getInstance().getRuntimePlatform().ToLower() + "/configs/0/" + PROJECT_FILENAME;
            Debug.LogError(url);
            mDownloader.downloadAsync(url, _tempProjectPath, PROJECT_ID);
        }

        /// <summary>
        /// �����Ƿ���Ҫ����
        /// </summary>
        private void checkVersion()
        {
            if (_updateState != State.VersionLoaded) return;
            _remoteVersion = Tool.loadObjectFromJsonFile<Version>(_cacheVersionPath);
            if (_remoteVersion.isLoaded())
            {
                _remoteVersion.setRoot(_storagePath);
                if (!_localProject.versionEquals(_localProject.version, _remoteVersion.version))
                {
                    _updateState = State.NewBigVersion;
                }
                else
                {
                    var ret = _localProject.versionEquals(_remoteVersion);
                    if (ret) _updateState = State.UpToData;
                    else
                    {
                        _updateState = State.DownloadProject;
                    }
                }
            }
            else
            {
                _updateState = State.DownloadProject;
            }
            Update();
        }

        private void checkProject()
        {
            if (_updateState != State.ProjectLoaded) return;
            _remoteVersion = Tool.loadObjectFromJsonFile<Project>(_tempProjectPath);
            if (_remoteVersion.isLoaded())
            {
                _remoteVersion.setRoot(_storagePath);
                var ret = _localProject.versionEquals(_remoteVersion);
                if (ret) _updateState = State.UpToData;
                else
                {
                    _updateState = State.NeedUpdate;
                }
            }
            else
            {
                _updateState = State.UnChecked;
            }
            Update();
        }

        private void StartUpdate()
        {
            if (_updateState != State.NeedUpdate) return;
            _downloadUnits.Clear();
            if (_tempProject.isLoaded() && _tempProject.versionEquals(_remoteVersion) && _tempProject.assetsEquals((Project)_remoteVersion))
            {
                ///���л����ҿ���������һ����
                _tempProject.genResumeAssetsList(ref _downloadUnits);
                updateAssets(_downloadUnits);
            }
            else
            {
                _tempProject = (Project)_remoteVersion;
                ///�ԱȰ汾�ļ��������ļ�����ɾ��
                Dictionary<string, AssetDiff> diff_map = _localProject.genDiff(_tempProject);
                if (diff_map.Count == 0)
                {
                    _updateState = State.UpToData;
                    Update();
                    return;
                }
                genDownloadList(diff_map);
                updateAssets(_downloadUnits);
            }

        }

        public void genSearchPath()
        {
            var searchPath = new List<string>();
            //�Ӱ����ڵ�
            addCommonPath(ref searchPath, mFolderName);
            //�Ӵ�����
            if (!mFolderName.Equals(Const.GAME_COMMON_NAME))
            {
                string projectFolder = FileUtils.getInstance().getWritablePath(Const.GAME_COMMON_NAME);
                Project commonProject = Tool.loadObjectFromJsonFile<Project>(projectFolder + "/" + PROJECT_FILENAME);
                addVersionPaths(Const.GAME_COMMON_NAME, ref searchPath, ref commonProject);
            }

            //����Ϸ��
            addVersionPaths(mFolderName, ref searchPath, ref _localProject);

#if UNITY_EDITOR && UNITY_STANDALONE
            if(!Setting.setting.update)
            {
                string relativePath = System.Environment.CurrentDirectory.Replace("\\", "/");
                searchPath.Insert(0, relativePath);
            }
#elif UNITY_EDITOR
            string relativePath = System.Environment.CurrentDirectory.Replace("\\", "/");
            searchPath.Insert(0, relativePath);
            searchPath.Insert(0, Application.persistentDataPath);
#endif
            FileUtils.getInstance().setSearchPaths(searchPath);
        }

        static void addCommonPath(ref List<string> searchPath, string folderName)
        {
            FileUtils.getInstance().ClearCache();

            searchPath.Add(Application.streamingAssetsPath);

            searchPath.Add(Path.Combine(Application.streamingAssetsPath, folderName.ToLower()));

            searchPath.Add(Path.Combine(Application.streamingAssetsPath, Const.GAME_COMMON_NAME.ToLower()));

            searchPath.Insert(0, Application.persistentDataPath);
        }

        private void addVersionPaths(string gameName, ref List<string> searchPath, ref Project localProject)
        {
            var root = _fileUtils.getWritablePath(gameName);
            searchPath.Insert(0, root);
            if (localProject.isLoaded())
            {
                var versions = localProject.groupVersions;
                for (int i = 0; i < versions.Count; i++)
                {
                    var ret = localProject.versionEquals(installVersion, versions[i]);
                    if (!ret)
                    {
                        searchPath.Insert(0, root + versions[i]);
                    }
                }
            }
        }



        /// <summary>
        /// ��������б�
        /// </summary>
        /// <param name="diff_map"></param>
        private void genDownloadList(Dictionary<string, AssetDiff> diff_map)
        {
            foreach (var it in diff_map)
            {
                AssetDiff diff = it.Value;

                if (diff.type == DiffType.DELETED)
                {
                    _fileUtils.removeFile(_storagePath + diff.asset.path);
                }
                else
                {
                    string path = diff.asset.path;
                    // Create path
                    _fileUtils.createDirectory(_storagePath + System.IO.Path.GetDirectoryName(path));
                    DownloadUnit unit = _tempProject.getDownloadItem(diff.asset);
                    _downloadUnits.Add(unit.customId, unit);
                }
            }
            // Set other assets' downloadState to SUCCESSED
            var assets = _tempProject.assets;
            foreach (var it in assets)
            {
                string key = it.Key;
                bool diffIt = diff_map.ContainsKey(key);
                //TODO ����
                if (!diffIt)
                {
                    _tempProject.setAssetDownloadState(key, DownloadState.Successed);
                    Tool.saveObjectToJsonFile(_tempProject, _tempProjectPath);
                }
            }

        }



        /// <summary>
        /// ����
        /// </summary>
        /// <param name="assets"></param>
        protected void updateAssets(Dictionary<string, DownloadUnit> assets)
        {
            if (_updateState != State.Updating)
            {
                _updateState = State.Updating;
                _downloadUnits = assets;
                mDownloader.batchDownloadAsync(assets);

            }
        }

        /// <summary>
        /// ��ѹ�ļ�
        /// </summary>
        private void decompressDownloadedZip()
        {

            for (int i = 0; i < _compressedFiles.Count; i++)
            {
                string zipfile = _compressedFiles[i];
                if (!_fileUtils.unZip(zipfile))
                {
                    _updateState = State.DecompressFail;
                }
                _fileUtils.removeFile(zipfile);
            }
            _compressedFiles.Clear();
        }


        public void Update()
        {
            if (!_localProject.isLoaded())
            {
                Debug.LogError("project file is null");
                return;
            }
            switch (_updateState)
            {
                case State.UnChecked:
                case State.DownloadVersion:
                    _updateState = State.DownloadVersion;
                    downloadVersion();
                    break;
                case State.VersionLoaded:
                    checkVersion();
                    break;
                case State.DownloadProject:
                    downloadProject();
                    break;
                case State.ProjectLoaded:
                    checkProject();
                    break;
                case State.FailToUpdate:
                case State.NeedUpdate:
                    _updateState = State.NeedUpdate;
                    StartUpdate();
                    break;
                case State.Updating:
                    break;
                case State.UpToData:
                case State.UpdateSuccess:
                    dispatchUpdateEvent(_updateState, 100, "update succeed!");
                    break;
                case State.DecompressFail:
                    dispatchUpdateEvent(State.DecompressFail, 0, "");
                    break;
                case State.NewBigVersion:
                    dispatchUpdateEvent(State.NewBigVersion, 0, "");
                    break;

            }
        }


        protected void dispatchUpdateEvent(State state, int p, string msg)
        {
            if (onCallback != null)
            {
                onCallback(this, state, p, msg);
            }

            //if (luafun != null)
            //{
            //    luafun.call(this, state, p, msg);
            //}
        }






        #region �¼��ص�
        void IUpdateHandler.onProgress(double progress, string url, string customId, Downloader loader)
        {

        }

        void IUpdateHandler.onDownladed(string srcUrl, string storagePath, string customId, Downloader loader)
        {
            if (customId == VERSION_ID)
            {
                _updateState = State.VersionLoaded;
                Update();
            }
            else if (customId == PROJECT_ID)
            {
                _updateState = State.ProjectLoaded;
                Update();
            }
            else
            {
                _tempProject.setAssetDownloadState(customId, DownloadState.Successed);
                Tool.saveObjectToJsonFile(_tempProject, _tempProjectPath);
                _compressedFiles.Add(storagePath);
            }
        }

        void IUpdateHandler.onError(Downloader.Error err, Downloader loader)
        {
            Debug.Log(err.message);
            if (err.customId == VERSION_ID)
            {
                _updateState = State.DownloadProject;
                Update();
            }
            else
            {
                _updateState = State.FailToUpdate;
                //dispatchUpdateEvent(State.FailToUpdate, 0, err.customId);
            }
        }

        void IUpdateHandler.onSuccess(Downloader loader)
        {
            _updateState = State.UpdateSuccess;
            try
            {
                decompressDownloadedZip();
            }
            catch (Exception e)
            {
                _updateState = State.DecompressFail;
                Debug.Log(e.Message);
            }
            if (_updateState == State.UpdateSuccess)
            {
                _fileUtils.renameFile(_storagePath, TEMP_PROJECT_FILENAME, PROJECT_FILENAME);
                _localProject = _tempProject;
            }
        }

        #endregion
    }
}