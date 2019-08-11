﻿using System;
using System.Threading.Tasks;
using Android.OS;
using Android.Webkit;
using IRO.XWebView.Core;
using IRO.XWebView.Core.BindingJs;
using IRO.XWebView.Core.Consts;
using IRO.XWebView.Droid.BrowserClients;
using IRO.XWebView.Droid.Utils;

namespace IRO.XWebView.Droid
{
    public class AndroidXWebView : BaseXWebView
    {
        IWebViewContainer _webViewContainer;

        /// <summary>
        /// WebViewClient will be overrided.
        /// </summary>
        /// <param name="webViewActivity"></param>
        protected AndroidXWebView(IWebViewContainer webViewContainer)
        {
            _webViewContainer = webViewContainer ?? throw new ArgumentNullException(nameof(webViewContainer));
            CurrentWebView = _webViewContainer.CurrentWebView ??
                             throw new NullReferenceException(nameof(_webViewContainer.CurrentWebView));

            //Set events proxy webviewclient.
            var webViewClient = CurrentWebView.ProxyWebViewClient();
            WebViewClientEvents = webViewClient.EventsProxy;

            //Set events proxy webchromeclient.
            var webChromeClient = CurrentWebView.ProxyWebChromeClient();
            WebChromeClientEvents = webChromeClient.EventsProxy;

            //Register main events.
            var weakThis = new WeakReference<AndroidXWebView>(this);
            webViewClient.LoadStarted += (s, a) =>
            {
                if (weakThis.TryGetTarget(out var thisReference))
                    thisReference.OnLoadStarted(a);
            };
            webViewClient.LoadFinished += (s, a) =>
            {
                if (weakThis.TryGetTarget(out var thisReference))
                    thisReference.OnLoadFinished(a);
            };
            CurrentWebView.ViewDetachedFromWindow += delegate
            {
                if (weakThis.TryGetTarget(out var thisReference))
                    thisReference.Dispose(true);
            };

            //Stop load because previouse loads will not be
            //detected (because IsBusy set event wasn't assigned from start).
            Stop();
            
            //Add js interface.
            CurrentWebView.AddJavascriptInterface(
                new AndroidBridge(BindingJsSystem, this),
                Core.BindingJs.BindingJsSystem.JsBridgeObjectName
            );
        }

        public IWebViewClientEventsProxy WebViewClientEvents { get; private set; }

        public WebChromeClientEventsProxy WebChromeClientEvents { get; private set; }

        public WebView CurrentWebView { get; private set; }

        public override bool CanSetVisibility => _webViewContainer.CanSetVisibility;

        public static async Task<AndroidXWebView> Create(IWebViewContainer webViewContainer)
        {
            if (webViewContainer == null)
                throw new ArgumentNullException(nameof(webViewContainer));
            await webViewContainer.WaitWebViewInitialized();
            var iwv = new AndroidXWebView(webViewContainer);
            await iwv.TryLoadUrl("about:blank");
            ThreadSync.TryInvoke(() => { iwv.CurrentWebView.ClearHistory(); });
            await webViewContainer.WebViewWrapped(iwv);
            return iwv;
        }

        /// <summary>
        /// Js result will be converted by JsonConvert.
        /// Note: Promises will be awaited like <see cref="Task" />.
        /// </summary>
        public override async Task<string> ExJsDirect(string script, int? timeoutMS = null)
        {
            ThrowIfDisposed();
            var jsResult = await CurrentWebView.ExJsWithResult(script, timeoutMS);
            var jsResultString = jsResult.ToString();
            return jsResultString;
        }

        public sealed override void Stop()
        {
            ThreadSync.Invoke(() => { CurrentWebView.StopLoading(); });
        }

        public override bool CanGoForward()
        {
            var res = ThreadSync.Invoke(() => CurrentWebView.CanGoForward());
            return res;
        }

        public override bool CanGoBack()
        {
            var res = ThreadSync.Invoke(() => CurrentWebView.CanGoBack());
            return res;
        }

        /// <summary>
        /// Return native WebView.
        /// </summary>
        /// <returns></returns>
        public override object Native()
        {
            return CurrentWebView;
        }

        public override void ClearCookies()
        {
            ThreadSync.Invoke(() =>
            {
                CurrentWebView.ClearCache(true);
                if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.LollipopMr1)
                {
                    CookieManager.Instance.RemoveAllCookies(null);
                    CookieManager.Instance.Flush();
                }
                else
                {
                    var cookieSyncMngr = CookieSyncManager.CreateInstance(Android.App.Application.Context);
                    cookieSyncMngr.StartSync();
                    var cookieManager = CookieManager.Instance;
                    cookieManager.RemoveAllCookies(null);
                    cookieManager.RemoveSessionCookie();
                    cookieSyncMngr.StopSync();
                    cookieSyncMngr.Sync();
                }
            });
        }

        protected override void ToggleVisibilityState(XWebViewVisibility visibility)
        {
            _webViewContainer.ToggleVisibilityState(visibility);
        }

        protected override void StartLoading(string url)
        {
            ThreadSync.Invoke(() => { CurrentWebView.LoadUrl(url); });
        }

        protected override void StartLoadingHtml(string data, string baseUrl)
        {
            ThreadSync.Invoke(() =>
            {
                CurrentWebView.LoadDataWithBaseURL(
                    baseUrl,
                    data,
                    "text/html",
                    "UTF-8",
                    Url
                );
            });
        }

        protected override void StartReloading()
        {
            ThreadSync.Invoke(() => { CurrentWebView.Reload(); });
        }

        protected override void StartGoForward()
        {
            ThreadSync.Invoke(() => { CurrentWebView.GoForward(); });
        }

        protected override void StartGoBack()
        {
            ThreadSync.Invoke(() => { CurrentWebView.GoBack(); });
        }

        #region Disposing.
        bool _isDisposing;

        public override void Dispose()
        {
            base.Dispose();
            Dispose(false);
        }

        void Dispose(bool containerDisposed)
        {
            //In order not to call the method again from Activity.Finish().
            if (_isDisposing)
                return;
            _isDisposing = true;

            //Dispose webview.
            ThreadSync.Invoke(() =>
            {
                try
                {
                    CurrentWebView.Destroy();
                    CurrentWebView.Dispose();
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"WebView disposing exception {ex}.");
                }
            });
            CurrentWebView = null;

            //Dispose activity.
            if (!containerDisposed)
            {
                ThreadSync.Invoke(() =>
                {
                    try
                    {
                        if(!_webViewContainer.IsDisposed)
                            _webViewContainer.Dispose();
                    }
                    catch
                    {
                    }
                });
            }

            _webViewContainer = null;
            WebViewClientEvents = null;
        }
        #endregion
    }
}