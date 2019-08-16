﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IRO.EmbeddedResources;
using IRO.Tests.XWebView.Core.JsInterfaces;
using IRO.XWebView.Core;
using Newtonsoft.Json;

namespace IRO.Tests.XWebView.Core
{
    public class TestApp
    {
        public async Task Setup(TestAppSetupConfigs configs)
        {
            var mainXWV = configs.MainXWebView;
            var provider = configs.Provider;
            var env = configs.TestingEnvironment;
            var contentPath = configs.ContentPath ?? AppDomain.CurrentDomain.BaseDirectory;


            var nativeJsInterface = new TestsMainMenuJsInterface(
                mainXWV,
                provider,
                env
                );
            //Now this object will be accessible in main webview on each page, after you call AttachBridge.
            mainXWV.BindToJs(nativeJsInterface, "Native");
            Action<string> loadAct = (str) => { mainXWV.TryLoadUrl(str); };
            mainXWV.BindToJs(loadAct, "Load", "N");


            //Automatically AttachBridge not implemented due WebViews limitation and perfomance.
            //See workarounds on github. You can use code below to attach bridge on each page load, but this method will be async.
            //So there no guarantees that bridge attach will be finished at the right time even when you use 'await xwv.LoadUrl()'.
            mainXWV.LoadFinished += async delegate
            {
                await mainXWV.AttachBridge();
                //Notify page that bridge attached. Define this on your page to do some things.
                var script = @"
try{
  if(!window.IsBridgeAttachedInvoked){
    window.IsBridgeAttachedInvoked=true;
    BridgeAttached();
  }
}catch(e){}";
                await mainXWV.ExJsDirect(script);
            };


            //In current project i use 'BuildAction:Embedded Resource' to include all needed file,
            //because not all platforms (Xamarin) support 'BuildAction:Content'. You can put your files in xamarin projects assets,
            //but i decide to put them in this assembly and extract on launch.
            var extractResourcesPath = Path.Combine(contentPath, "WebAppSource");

            //If you will use this method on production i recomend to check somehow if files of current app version is extracted.
            //In this code they always re-extracted on app launch.
            var embeddedDirPath = "IRO.Tests.XWebView.Core.WebAppSource";
            var assembly = Assembly.GetExecutingAssembly();
            //From IRO.EmbeddedResourcesHelpers nuget.
            assembly.ExtractEmbeddedResourcesDirectory(embeddedDirPath, extractResourcesPath);

            await mainXWV.WaitWhileBusy();
            await mainXWV.LoadUrl("file://" + extractResourcesPath + "/MainPage.html");
        }
    }
}
