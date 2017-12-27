using Client.UIFramework.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEngine;

namespace VersionCtrl
{
    public enum DownloadState
    {
        UnStarted,
        Downloading,
        Successed
    };

    public struct DownloadUnit
    {
        public string srcUrl;
        public string storagePath;
        public string customId;
        public string md5;
        public long size;
        public DownloadState downloadState;
    };



    public class Downloader
    {
        public IUpdateHandler handler;
        public int _connectionTimeout;
        public const string TEMP = ".temp";

        private double percent = 0;
        private double mTotal;
        int _totalWaitToDownload = 0;

        private Dictionary<string, DownloadUnit> wantToDownLoadUnits;

        private bool flags;

        public enum ErrorCode
        {
            UnNetWork,
            /// <summary>
            /// Initial status of a request
            /// </summary>
            Initial,

            /// <summary>
            /// Waiting in a queue to be processed
            /// </summary>
            Queued,

            /// <summary>
            /// Processing of the request started
            /// </summary>
            Processing,

            /// <summary>
            /// The request finished without problem.
            /// </summary>
            Finished,

            /// <summary>
            /// The request finished with an unexpected error. The request's Exception property may contain more info about the error.
            /// </summary>
            Error,

            /// <summary>
            /// The request aborted by the client.
            /// </summary>
            Aborted,

            /// <summary>
            /// Ceonnecting to the server is timed out.
            /// </summary>
            ConnectionTimedOut,

            /// <summary>
            /// The request didn't finished in the given time.
            /// </summary>
            TimedOut
        };

        public struct Error
        {
            public ErrorCode code;
            public string message;
            public string customId;
            public string url;
        };

        public class ProgressData
        {
            private FileStream stream;
            public Downloader downloader;
            public string customId;
            public string url;
            public string path;
            public string name;
            public double downloaded;
            public double totalToDownload;
            public bool async;
            public string md5;
            public long size;

            public void Write(byte[] buf, int size)
            {

                if (stream == null)
                {
                    throw new IOException(string.Format("file {0} not open.", path + name + TEMP));
                }
                stream.Write(buf, 0, size);
            }

            public void Close()
            {
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
            }

            public bool Open(long countLength, out long lStartPos, out bool hasDownload)
            {
                hasDownload = false;
                var targetPath = path + name;
                if (File.Exists(targetPath))
                {
                    if (!async && md5 == FileUtils.getInstance().GetMd5HashFromFile(targetPath))
                    {
                        lStartPos = countLength;
                        hasDownload = true;
                        return true;
                    }
                    else
                    {
                        FileUtils.getInstance().removeFile(targetPath);
                    }
                }
                //打开上次下载的文件或新建文件 
                lStartPos = 0;
                FileStream fs;
                var saveFile = path + name + TEMP;
                if (File.Exists(saveFile))
                {
                    fs = File.OpenWrite(saveFile);
                    stream = fs;
                    lStartPos = fs.Length;
                    if (countLength - lStartPos <= 0)
                    {
                        return true;
                    }
                    fs.Seek(lStartPos, System.IO.SeekOrigin.Current); //移动文件流中的当前指针 
                }
                else
                {
                    FileUtils.getInstance().createDirectory(path);
                    fs = new System.IO.FileStream(saveFile, System.IO.FileMode.Create);
                }
                stream = fs;
                return false;
            }
        }

        public Downloader(IUpdateHandler handler)
        {
            this.handler = handler;
            setConnectionTimeout(5);
            UnityThreadHelper.EnsureHelper();
        }

        public void setConnectionTimeout(int timeout)
        {
            if (timeout >= 0)
                _connectionTimeout = timeout;
        }

        public void downloadAsync(string srcUrl, string storagePath, string customId)
        {
            ProgressData data = new ProgressData();
            prepareDownload(srcUrl, storagePath, customId, "", 0, data);
            data.async = true;

            percent = 0;
            mTotal = 0;

            UnityThreadHelper.CreateThread(() =>
            {
                downloadFile(data);
            });

        }

        private bool downloadFile(ProgressData data)
        {
            Stream ns = null;
            HttpWebResponse webResponse = null;
            try
            {
                //打开网络连接 
                long countLength = data.size;
                long lStartPos = 0;
                bool hasDownload = false;
                if (data.async) countLength = GetFileLenghtFromNet(data.url);
                if (data.Open(countLength, out lStartPos, out hasDownload))
                {
                    data.Close();
                    return DownloadFinish(data, hasDownload);
                }
                if (mTotal == 0) mTotal = countLength;
                HttpWebRequest request = HttpTool.Instance.createWebRequest(data.url);
                if (lStartPos > 0)
                {
                    request.AddRange((int)lStartPos); //设置Range值
                    //Debuger.Log(lStartPos);
                }

                webResponse = request.GetResponse() as HttpWebResponse;

                //向服务器请求，获得服务器回应数据流 
                ns = webResponse.GetResponseStream();

                int len = 1024 * 8;

                byte[] nbytes = new byte[len];
                int nReadSize = 0;
                nReadSize = ns.Read(nbytes, 0, len);

                double dlNow = lStartPos;

                while (nReadSize > 0)
                {
                    dlNow += nReadSize;
                    percent += nReadSize;
                    data.Write(nbytes, nReadSize);
                    onProgress(data, countLength, dlNow, mTotal, percent);
                    nReadSize = ns.Read(nbytes, 0, len);
                }
            }
            catch (WebException e)
            {
                OnError(data, e.Status);
                Debug.LogError(e);
                Debug.Log(data.url);
                return false;
            }
            finally
            {
                if (webResponse != null)
                    webResponse.Close();
                if (ns != null)
                    ns.Close();
                data.Close();
            }

            return DownloadFinish(data, false);
        }

        private bool DownloadFinish(ProgressData data, bool hasDownload)
        {
            var f = data.path + data.name + TEMP;
            if (!data.async)
            {
                if (!hasDownload)
                {
                    var md5 = FileUtils.getInstance().GetMd5HashFromFile(f);
                    if (md5 != data.md5 && md5 != data.md5.ToLower())
                    {
                        var err = new Error();
                        err.customId = data.customId;
                        err.code = ErrorCode.Error;
                        err.message = "Md5验证失败！ file:" + data.path + "/" + data.name + " md5:" + md5;
                        if (handler != null)
                        {
                            UnityThreadHelper.Dispatcher.Dispatch(() =>
                            {
                                handler.onError(err, this);
                            });
                        }
                        FileUtils.getInstance().removeFile(f);
                        return false;

                    }
                    FileUtils.getInstance().renameFile(data.path, data.name + TEMP, data.name);
                }
                if (handler != null)
                {
                    UnityThreadHelper.Dispatcher.Dispatch(() =>
                    {
                        handler.onDownladed(data.url, data.path + data.name, data.customId, this);
                    });
                }

                _totalWaitToDownload--;

                wantToDownLoadUnits.Remove(data.customId);
                if (_totalWaitToDownload == 0)
                {
                    if (handler != null)
                    {
                        UnityThreadHelper.Dispatcher.Dispatch(() =>
                        {
                            handler.onSuccess(this);
                        });
                    }
                }
            }
            else
            {
                FileUtils.getInstance().renameFile(data.path, data.name + TEMP, data.name);
                if (handler != null)
                {
                    UnityThreadHelper.Dispatcher.Dispatch(() =>
                    {
                        handler.onDownladed(data.url, data.path + data.name, data.customId, this);
                    });
                }
            }

            return true;
        }


        private int onProgress(object extraData, double dlTotal, double dlNow, double ulTotal, double ulNow)
        {
            if (dlTotal <= 0) return 0;
            var val = ((ulNow / ulTotal) * 100);

            var data = (ProgressData)extraData;
            if (handler != null)
            {
                UnityThreadHelper.Dispatcher.Dispatch(() =>
                {
                    handler.onProgress(val, data.url, data.customId, this);
                });

            }
            return 0;
        }

        private void OnError(ProgressData data, WebExceptionStatus code)
        {
            var err = new Error();
            err.customId = data.customId;
            err.code = ErrorCode.Error;
            err.message = code.ToString();
            if (handler != null)
            {
                UnityThreadHelper.Dispatcher.Dispatch(() =>
                {
                    handler.onError(err, this);
                });
            }
        }


        /// <summary>
        /// 获得远程文件的大小
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static long GetFileLenghtFromNet(string url)
        {
            HttpWebRequest requestFileLenght = (HttpWebRequest)WebRequest.Create(url);
            long countLength = 0;
            HttpWebResponse res = null;
            try
            {
                requestFileLenght.Method = "HEAD";
                requestFileLenght.Timeout = 10000;
                requestFileLenght.Proxy = null;
                res = requestFileLenght.GetResponse() as HttpWebResponse;
                countLength = res.ContentLength;
            }
            catch (WebException e)
            {
                throw e;
            }
            finally
            {
                if (res != null)
                    res.Close();
            }

            return countLength;
        }

        private void prepareDownload(string srcUrl, string storagePath, string customId, string md5, long size, ProgressData data)
        {
            data.downloader = this;
            data.customId = customId;
            data.url = srcUrl;
            data.name = Path.GetFileName(storagePath);
            data.path = Path.GetDirectoryName(storagePath) + "/";
            data.md5 = md5;
            data.size = size;
        }



        /// <summary>
        /// 批量下载
        /// </summary>
        /// <param name="units"></param>
        /// <param name="batchId"></param>
        public void batchDownloadAsync(Dictionary<string, DownloadUnit> units)
        {
            _totalWaitToDownload = units.Count;
            if (_totalWaitToDownload == 0) handler.onSuccess(this);
            wantToDownLoadUnits = units;
            List<DownloadUnit> list = new List<DownloadUnit>();
            percent = 0;
            foreach (var item in units)
            {
                list.Add(item.Value);
                mTotal += item.Value.size;
            }
            UnityThreadHelper.CreateThread(() =>
            {
                download(list);
            });
        }

        private void download(List<DownloadUnit> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];

                flags = groupBatchDownload(item);
                if (!flags) break;
            }
        }

        private bool groupBatchDownload(DownloadUnit unit)
        {
            ProgressData data = new ProgressData();
            prepareDownload(unit.srcUrl, unit.storagePath, unit.customId, unit.md5, unit.size, data);
            return downloadFile(data);
        }

    }
}