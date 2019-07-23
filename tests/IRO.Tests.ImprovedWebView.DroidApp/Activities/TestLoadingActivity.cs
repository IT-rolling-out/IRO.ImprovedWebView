﻿using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using IRO.ImprovedWebView.Core;
using IRO.ImprovedWebView.Droid;

namespace IRO.Tests.ImprovedWebView.DroidApp.Activities
{
    [Activity(Label = "TestLoadingActivity", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class TestLoadingActivity : BaseTestActivity
    {
        protected override async Task RunTest(AndroidImprovedWebView iwv)
        {
            await Task.Run(() => { });
            int delay = 1000;

            //Choose websites that can load long time.
            //This three must be aborted in test.
            iwv.TryLoadUrl("https://stackoverflow.com");
            Application.SynchronizationContext.Send((obj) =>
            {
                CurrentWebView.LoadUrl("https://twitter.com");
            }, null);
            iwv.TryLoadUrl("https://visualstudio.microsoft.com/ru/");
            await Task.Delay(50);
            ShowMessage($"3 loads aborted.");

            var loadRes = await iwv.LoadUrl("https://www.microsoft.com/");
            ShowMessage($"Loaded {loadRes.Url}");
            await Task.Delay(delay);

            loadRes = await iwv.Reload();
            ShowMessage($"Reloaded {loadRes.Url}");
            await Task.Delay(delay);

            loadRes = await iwv.LoadUrl("https://www.youtube.com/");
            ShowMessage($"Loaded {loadRes.Url}");
            await Task.Delay(delay);

            loadRes = await iwv.LoadUrl("https://www.google.com/");
            ShowMessage($"Loaded {loadRes.Url}");
            await Task.Delay(delay);

            loadRes = await iwv.GoBack();
            ShowMessage($"GoBack {loadRes.Url}");
            await Task.Delay(delay);

            loadRes = await iwv.GoForward();
            ShowMessage($"GoForward {loadRes.Url}");
            await Task.Delay(delay);

        }
    }
}