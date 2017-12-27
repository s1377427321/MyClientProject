namespace VersionCtrl
{
    public interface IUpdateHandler
    {
        void onProgress(double progress, string url, string customId, Downloader loader);

        void onDownladed(string srcUrl, string storagePath, string customId, Downloader loader);

        void onError(Downloader.Error err, Downloader loader);

        void onSuccess(Downloader loader);
    }
}