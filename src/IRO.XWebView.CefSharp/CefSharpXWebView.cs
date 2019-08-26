﻿using System;
using System.Threading.Tasks;
using CefSharp;
using IRO.XWebView.CefSharp.BrowserClients;
using IRO.XWebView.CefSharp.Containers;
using IRO.XWebView.Core;
using IRO.XWebView.Core.BindingJs.LowLevelBridge;
using IRO.XWebView.Core.Consts;
using IRO.XWebView.Core.Events;
using IRO.XWebView.Core.Exceptions;
using IRO.XWebView.Core.Utils;

namespace IRO.XWebView.CefSharp
{
    public class CefSharpXWebView : BaseXWebView
    {
        LowLevelBridge _bridge;

        ICefSharpContainer _container;

        public IWebBrowser Browser { get; private set; }

        /// <summary>
        /// If true - you can change visibility after creation.
        /// If false - <see cref="P:IRO.XWebView.Core.IXWebView.Visibility" /> assignment will throw exception.
        /// </summary>
        public override bool CanSetVisibility => _container.CanSetVisibility;

        public override string BrowserName => nameof(CefSharpXWebView);

        protected CefSharpXWebView(ICefSharpContainer container, CustomRequestHandler customRequestHandler = null)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            Browser = _container.CurrentBrowser;
            if (Browser == null)
            {
                throw new XWebViewException("Browser is null.");
            }

            //Register native bridge.
            _bridge = new LowLevelBridge(this.BindingJsSystem, this);

            ThreadSync.Inst.Invoke(() =>
            {
                var bindingOpt = new BindingOptions();
                bindingOpt.CamelCaseJavascriptNames = false;
                Browser.RegisterJsObject(
                    Core.BindingJs.BindingJsSystem.JsBridgeObjectName,
                    _bridge,
                    bindingOpt
                    );
                Browser.RequestHandler = customRequestHandler ?? new CustomRequestHandler();

                // ReSharper disable once VirtualMemberCallInConstructor
                RegisterEvents();
            });
        }

        public static async Task<CefSharpXWebView> Create(ICefSharpContainer container,
            CustomRequestHandler customRequestHandler = null)
        {
            var xwv = new CefSharpXWebView(container, customRequestHandler);
            await container.Wrapped(xwv);
            return xwv;
        }

        public override async Task<string> UnmanagedExecuteJavascriptWithResult(string script, int? timeoutMS = null)
        {
            ThrowIfDisposed();
            await WaitCanExecuteJs(2000);
            return await ThreadSync.Inst.InvokeAsync(async () =>
            {
                if (!Browser.CanExecuteJavascriptInMainFrame)
                    throw new XWebViewException($"Can't execute js in main frame. " +
                                                $"Use '{nameof(UnmanagedExecuteJavascriptAsync)}' to get around this limitation.");

                TimeSpan ? timeout = null;
                if (timeoutMS != null)
                {
                    timeout = TimeSpan.FromMilliseconds(timeoutMS.Value);
                }
                //Use JSON.stringify to make it compatible with other browsers.
                var allScript = $@"
var result = {script} ;
JSON.stringify(result);
";
                var jsResponse = await Browser.EvaluateScriptAsync(allScript, timeout);
                if (jsResponse.Success)
                {
                    var res = jsResponse.Result.ToString();
                    return res;
                }
                else
                {
                    throw new XWebViewException(
                        $"{nameof(UnmanagedExecuteJavascriptWithResult)} error '{jsResponse.Message}'.");
                }
            });
        }

        public override void UnmanagedExecuteJavascriptAsync(string script, int? timeoutMS = null)
        {
            ThrowIfDisposed();
            ThreadSync.Inst.Invoke(() =>
            {
                //?Why not Browser.ExecuteScriptAsync(script); ?
                //Method above will throw exceptions if V8Context of frame is not created.
                //This can happen when page load aborted or page doesn't contains javascript.
                //Opposite, Frame.ExecuteJavaScriptAsync will ignore this and always execute js,
                //because it will create context if it not exists.
                var mainFrame = Browser.GetMainFrame();
                mainFrame.ExecuteJavaScriptAsync(script, mainFrame.Url);
            });
        }

        public override void Stop()
        {
            ThrowIfDisposed();
            ThreadSync.Inst.TryInvoke(() =>
            {
                Browser.Stop();
            });
        }

        public override void ClearCookies()
        {
            ThrowIfDisposed();
            ThreadSync.Inst.TryInvoke(() =>
            {
                var cookieManager = Browser.GetCookieManager();
                cookieManager.DeleteCookies();
            });
        }

        protected override void StartLoading(string url)
        {
            ThreadSync.Inst.TryInvoke(() =>
            {
                Browser.Load(url);
            });
        }

        protected override void StartLoadingHtml(string data, string baseUrl)
        {
            ThreadSync.Inst.TryInvoke(() =>
            {
                Browser.LoadHtml(data, baseUrl);
            });
        }

        protected override void SetVisibilityState(XWebViewVisibility visibility)
        {
            _container.SetVisibilityState(visibility);
        }

        protected override XWebViewVisibility GetVisibilityState()
            => _container.GetVisibilityState();

        public override bool CanGoForward()
        {
            return ThreadSync.Inst.Invoke(() => Browser.CanGoForward);
        }

        public override bool CanGoBack()
        {
            return ThreadSync.Inst.Invoke(() => Browser.CanGoBack);
        }

        public override object Native()
        {
            return Browser;
        }

        public override void Dispose()
        {
            ThreadSync.Inst.TryInvoke(() => 
            {
                Browser.Dispose();
                Browser = null;
            });
            try
            {
                _bridge.Dispose();
                _container.Dispose();
                _bridge = null;
                _container = null;
            }
            catch { }
            base.Dispose();
        }

        protected virtual void RegisterEvents()
        {
            var reqHandler = (CustomRequestHandler)Browser.RequestHandler;
            reqHandler.BeforeBrowse += (chromiumWebBrowser, browser, frame, request, userGesture, isRedirect) =>
            {
                if (frame.IsMain && !isRedirect)
                {
                    var args = new LoadStartedEventArgs()
                    {
                        Url = request.Url
                    };
                    OnLoadStarted(args);
                    return !args.Cancel;
                }
                return true;
            };
            bool isFirstLoad = true;
            Browser.FrameLoadEnd += (s, a) =>
            {
                if (isFirstLoad)
                {
                    //!Ignore first load (load of initial page).
                    //It's easiest way to handle FrameLoadEnd of first page i found,
                    //because it will be rised without rising BeforeBrowse.
                    isFirstLoad = false;
                    return;
                }
                if (!a.Frame.IsMain)
                    return;
                var args = new LoadFinishedEventArgs()
                {
                    Url = a.Frame.Url
                };
                OnLoadFinished(args);
            };
            Browser.LoadError += (s, a) =>
            {
                if (!a.Frame.IsMain)
                    return;
                //if (a.ErrorCode == CefErrorCode.Aborted)
                //    return;
                var args = new LoadFinishedEventArgs()
                {
                    Url = a.FailedUrl,
                    ErrorType = a.ErrorCode.ToString(),
                    ErrorDescription = a.ErrorText,
                    IsError = true
                };
                OnLoadFinished(args);
            };

            //Auto disposing.
            Browser.StatusMessage += (s, a) =>
            {
                if (a.Browser.IsDisposed)
                {
                    try
                    {
                        Dispose();
                    }
                    catch { }
                }
            };
        }

        #region CefSharp special.
        /// <summary>
        /// Return true if can execute.
        /// </summary>
        public async Task<bool> WaitCanExecuteJs(int timeoutMS = 3000)
        {
            while (true)
            {
                var canExecute = ThreadSync.Inst.Invoke(
                    () => Browser.CanExecuteJavascriptInMainFrame
                    );
                if (canExecute)
                {
                    return true;
                }
                await Task.Delay(20).ConfigureAwait(false);
                timeoutMS -= 20;
                if (timeoutMS <= 0)
                {
                    return false;
                }
            }
        }
        #endregion
    }
}
