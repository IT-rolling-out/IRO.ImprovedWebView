﻿using System;
using Android.Views;
using Android.Webkit;

namespace IRO.ImprovedWebView.Droid
{
    public static class AndroidImprovedWebViewExtensions
    {
        /// <summary>
        /// Extension.
        /// <para></para>
        /// Add event to android back button tap, it will invoke GoBack() method.
        /// </summary>
        /// <param name="onClose">Invoked when can't go back in browser.</param>
        public static void UseBackButtonCrunch(AndroidImprovedWebView androidImprovedWebView, View viewToRegisterEvent, Action onClose)
        {
            int backTaps = 0;
            int wantToQuitApp = 0;
            var ev = new EventHandler<View.KeyEventArgs>(async (s, e) =>
            {
                if (e.KeyCode == Keycode.Back)
                {
                    e.Handled = true;
                    if (backTaps > 0)
                    {
                        //wantToQuitApp используется для двух попыток нажать назад перед оконсчательной установкой, что нельзя идти назад.
                        //Просто баг в WebView.
                        var canGoBack = await androidImprovedWebView.CanGoBack() && wantToQuitApp > 0;
                        if (canGoBack)
                        {
                            wantToQuitApp = 0;
                            backTaps = 0;
                            await androidImprovedWebView.GoBack();
                        }
                        else
                        {
                            wantToQuitApp++;
                        }
                    }
                    else
                    {
                        backTaps++;
                    }
                }

            });
            viewToRegisterEvent.KeyPress += ev;
        }
    }
}