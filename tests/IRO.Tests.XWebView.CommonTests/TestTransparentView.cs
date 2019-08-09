﻿using System.Threading.Tasks;
using IRO.XWebView.Core;
using IRO.XWebView.Core.Consts;

namespace IRO.Tests.XWebView.CommonTests
{
    public class TestTransparentView : IXWebViewTest
    {
        public async Task RunTest(IXWebViewProvider xwvProvider, ITestingEnvironment env)
        {
            env.Message("Will execute alert('Hello transparent!') in transparent webview.");
            var xwv = await xwvProvider.Resolve(XWebViewVisibility.Hidden);
            await xwv.ExJsDirect("alert('Hello from transparent!')");
        }
    }
}