using Plugin.Share.Abstractions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace Plugin.Share
{
    /// <summary>
    /// Implementation for Share
    /// </summary>
    public class ShareImplementation : IShare
    {
        /// <summary>
        /// For linker
        /// </summary>
        /// <returns></returns>
        public static async Task Init()
        {
            var test = DateTime.UtcNow;
        }

        /// <summary>
        /// Open a browser to a specific url
        /// </summary>
        /// <param name="url">Url to open</param>
        /// <param name="options">Platform specific options</param>
        /// <returns>True if the operation was successful, false otherwise</returns>
        public async Task<bool> OpenBrowser(string url, BrowserOptions options = null)
        {
            try
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(url));

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to open browser: " + ex.Message);
                return false;
            }
        }

        string title, text, url;
        DataTransferManager dataTransferManager;

        /// <summary>
        /// Simply share text with compatible services
        /// </summary>
        /// <param name="text">Text to share</param>
        /// <param name="title">Title of the share popup on Android and Windows, email subject if sharing with mail apps</param>
        /// <returns>True if the operation was successful, false otherwise</returns>
        [Obsolete("Use Share(ShareMessage, ShareOptions)")]
        public Task<bool> Share(string text, string title = null)
        {
            var shareMessage = new ShareMessage();
            shareMessage.Title = title;
            shareMessage.Text = text;

            return Share(shareMessage);
        }

        /// <summary>
        /// Share a link url with compatible services
        /// </summary>
        /// <param name="url">Link to share</param>
        /// <param name="message">Message to include with the link</param>
        /// <param name="title">Title of the share popup on Android and Windows, email subject if sharing with mail apps</param>
        /// <returns>True if the operation was successful, false otherwise</returns>
        [Obsolete("Use Share(ShareMessage, ShareOptions)")]
        public Task<bool> ShareLink(string url, string message = null, string title = null)
        {
            var shareMessage = new ShareMessage();
            shareMessage.Title = title;
            shareMessage.Text = message;
            shareMessage.Url = url;

            return Share(shareMessage);
        }

        /// <summary>
        /// Share a message with compatible services
        /// </summary>
        /// <param name="message">Message to share</param>
        /// <param name="options">Platform specific options</param>
        /// <returns>True if the operation was successful, false otherwise</returns>
        public Task<bool> Share(ShareMessage message, ShareOptions options = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                title = message.Title;
                text = message.Text;
                url = message.Url;
                if (dataTransferManager == null)
                {
                    dataTransferManager = DataTransferManager.GetForCurrentView();
                    dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.ShareTextHandler);
                }
                DataTransferManager.ShowShareUI();

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to share: " + ex.Message);
                return Task.FromResult(false);
            }
        }

        private void ShareTextHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            try
            {
                DataRequest request = e.Request;

                // The Title is mandatory
#if WINDOWS_UWP || WINDOWS_APP
                request.Data.Properties.Title = title ?? Windows.ApplicationModel.Package.Current.DisplayName;
#else
                request.Data.Properties.Title = title ?? string.Empty;
#endif

                if (text != null)
                    request.Data.SetText(text);
                if (url != null)
                    request.Data.SetWebLink(new Uri(url));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to share: " + ex.Message);
            }
        }

        /// <summary>
        /// Sets text of the clipboard
        /// </summary>
        /// <param name="text">Text to set</param>
        /// <param name="label">Label to display (not required, Android only)</param>
        /// <returns>True if the operation was successful, false otherwise</returns>
        public Task<bool> SetClipboardText(string text, string label = null)
        {
#if WINDOWS_UWP || WINDOWS_APP
            try
            {
                var dataPackage = new DataPackage();
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                dataPackage.SetText(text);

                Clipboard.SetContent(dataPackage);

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to copy to clipboard: " + ex.Message);
                return Task.FromResult(false);
            }
#else
            return Task.FromResult(false);
#endif
        }

        /// <summary>
        /// Gets if cliboard is supported
        /// </summary>
        public bool SupportsClipboard
        {
#if WINDOWS_UWP || WINDOWS_APP
            get { return true; }
#else
            get { return false; }
#endif
        }
    }
}
