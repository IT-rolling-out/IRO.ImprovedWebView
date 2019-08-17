﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Chromely.CefGlue;
using Chromely.CefGlue.Browser;
using Chromely.CefGlue.Browser.EventParams;
using Chromely.Core;
using Chromely.Core.Host;
using IRO.XWebView.Core.Exceptions;
using IRO.XWebView.OnCefGlue.Exceptions;
using Xilium.CefGlue;

namespace IRO.XWebView.OnCefGlue
{
    public static class CefBrowserExtensions
    {
        const string CallbacksStartsWith = "CALLBACK_MESSAGE_";

        public static async Task<string> ExecuteJavascript(this CefBrowser browser, string script, int? timeoutMS)
        {
            var task = Task.Run(async () =>
            {
                browser.GetMainFrame().ExecuteJavaScript(script, "", 0);
                if (timeoutMS != null)
                {
                }

                if (cefV8Exception == null)
                {
                    var res = returnValue.GetStringValue();
                    return res;
                }
                else
                {
                    throw new CefGlueJsException(cefV8Exception);
                }
            });

            if (timeoutMS == null)
            {
                await task;
            }
            else
            {
                await Task.WhenAny(
                    task,
                    Task.Delay(timeoutMS.Value)
                );
            }

            if (task.IsCompleted)
                return task.Result;
            else
                throw new XWebViewException($"Js evaluation timeout {timeoutMS}");
        }
    }
}
