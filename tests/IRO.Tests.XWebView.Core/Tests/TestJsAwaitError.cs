﻿using System;
using System.Threading.Tasks;
using IRO.XWebView.Core.Consts;
using IRO.XWebView.Core.Providers;

namespace IRO.Tests.XWebView.Core.Tests
{
    public class TestJsAwaitError : IXWebViewTest
    {
        public async Task RunTest(IXWebViewProvider xwvProvider, ITestingEnvironment env, TestAppSetupConfigs appConfigs)
        {
            var xwv = await xwvProvider.Resolve(XWebViewVisibility.Visible);
            xwv.Disposing += delegate { env.Message($"XWebView disposed."); };
            //Rejected after delay.
            var delayScript = @"
window['delayPromiseError'] = function(delayMS) {
  return new Promise(function(resolve, reject){
    setTimeout(function(){
      reject('-----REJECT PASSED MESSAGE-----');
    }, delayMS)
  });
}";
            await xwv.ExJs<string>(delayScript);
            await xwv.AttachBridge();

            //ES7, chromium 55+ required.
            var scriptWithError = @"
return (async function(){
  await delayPromiseError(5000); 
})();";
            try
            {
                await xwv.ExJs<string>(scriptWithError, true);
            }
            catch (Exception ex)
            {
                if (!ex.ToString().Contains("-----REJECT PASSED MESSAGE-----"))
                    throw;
                xwv.Dispose();
                env.Message($"Test successful.\nCatched exception from promise: '{ex.ToString()}'");
            }
        }
    }
}