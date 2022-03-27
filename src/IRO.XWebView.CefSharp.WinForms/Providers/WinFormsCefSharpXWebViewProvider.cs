﻿using System;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.WinForms;
using IRO.XWebView.CefSharp.Utils;
using IRO.XWebView.Core;
using IRO.XWebView.Core.Consts;
using IRO.XWebView.Core.Providers;
using IRO.XWebView.Core.Utils;

namespace IRO.XWebView.CefSharp.WinForms.Providers
{
    public class WinFormsCefSharpXWebViewProvider : BaseXWebViewProvider
    {
        Action<IBrowserSettings, RequestContextSettings> _configAct;

        protected override async Task<IXWebView> ProtectedResolve(XWebViewVisibility preferredVisibility)
        {
            var chromiumForm = CreateForm();
            var xwv = new CefSharpXWebView(chromiumForm);
            XWebViewThreadSync.Inst.Invoke(() =>
            {
                chromiumForm.Show();
            });
            chromiumForm.SetVisibilityState(preferredVisibility);
            await chromiumForm.CurrentBrowser.WaitInitialization();
            return xwv;
        }

        public void Configure(Action<IBrowserSettings, RequestContextSettings> action)
        {
            _configAct = action;
        }

        protected virtual CefSharpXWebViewForm CreateForm()
        {
            CefHelpers.InitializeCefIfNot(new CefSettings());
            return XWebViewThreadSync.Inst.Invoke(() =>
            {
                var chromiumWindow = new CefSharpXWebViewForm();
                var br = (ChromiumWebBrowser)chromiumWindow.CurrentBrowser;
                br.BrowserSettings ??= new BrowserSettings();
                var requestContextSettings = new RequestContextSettings();
                _configAct?.Invoke(br.BrowserSettings, requestContextSettings);
                br.RequestContext = new RequestContext(requestContextSettings);
                return chromiumWindow;
            });

        }
    }
}
