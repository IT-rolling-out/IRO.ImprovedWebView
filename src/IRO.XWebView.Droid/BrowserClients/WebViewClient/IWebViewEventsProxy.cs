﻿namespace IRO.XWebView.Droid.BrowserClients
{
    public interface IWebViewClientEventsProxy
    {
        event OnPageFinishedDelegate PageFinishedEvent;

        event OnPageStartedDelegate PageStartedEvent;

        event ShouldOverrideUrlLoadingDelegate ShouldOverrideUrlLoadingEvent;

        event ShouldOverrideUrlLoading2Delegate ShouldOverrideUrlLoading2Event;

        event OnReceivedErrorDelegate ReceivedErrorEvent;

        event OnReceivedError2Delegate ReceivedError2Event;

        event OnLoadResourceDelegate LoadResourceEvent;
    }
}