﻿using System;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.Wpf;
using IRO.XWebView.CefSharp.Containers;
using IRO.XWebView.CefSharp.Utils;
using IRO.XWebView.CefSharp.Wpf.Utils;
using IRO.XWebView.Core;
using IRO.XWebView.Core.Consts;
using IRO.XWebView.Core.Exceptions;
using IRO.XWebView.Core.Providers;

namespace IRO.XWebView.CefSharp.Wpf.Providers
{
    public class WpfCefSharpXWebViewProvider : IXWebViewProvider
    {
        Action<IBrowserSettings, RequestContextSettings> _configAct;

        public virtual async Task<IXWebView> Resolve(XWebViewVisibility prefferedVisibility = XWebViewVisibility.Hidden)
        {
            var chromiumWindow = CreateWpfWindow();
            WpfThreadSync.Invoke(() =>
            {
                chromiumWindow.Show();
            }, chromiumWindow.Dispatcher);
            chromiumWindow.SetVisibilityState(prefferedVisibility);
            var xwv = await CefSharpXWebView.Create(chromiumWindow);
            return xwv;
        }

        public void Configure(Action<IBrowserSettings, RequestContextSettings> action)
        {
            _configAct = action;
        }

        public virtual ChromiumWindow CreateWpfWindow()
        {
            CefHelpers.InitializeCefIfNot();
            var chromiumWindow = new ChromiumWindow();
            return WpfThreadSync.Invoke(() =>
            {
                var br = (ChromiumWebBrowser) chromiumWindow.CurrentBrowser;
                br.BrowserSettings ??= new BrowserSettings();
                var requestContextSettings = new RequestContextSettings();
                _configAct?.Invoke(br.BrowserSettings, requestContextSettings);
                br.RequestContext = new RequestContext(requestContextSettings);
                return chromiumWindow;
            }, chromiumWindow.Dispatcher);
            
        }
    }
}
