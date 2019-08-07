﻿using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using IRO.XWebView.Droid;
using IRO.Tests.XWebView.CommonTests;

namespace IRO.Tests.XWebView.DroidApp.Activities
{
    [Activity(Label = "TestJsAwaitDelayActivity",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class TestJsAwaitDelayActivity : BaseTestActivity
    {
        protected override async Task RunTest(AndroidXWebView iwv)
        {
            var test = new TestJsAwaitDelay();
            await test.RunTest(iwv, TestingEnvironment);
        }

    }
}