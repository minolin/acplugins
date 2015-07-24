using acServerFake.view.logviewer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace acServerFake
{
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // If we're running around with plugins and reflection, the last Exception is often a TargetInvocationException that isn't helpful at all,
            // but the next InnerException is
            var ex = e.Exception;
            if (ex != null && ex.GetType() == typeof(TargetInvocationException) && ex.InnerException != null)
                ex = ex.InnerException;

            // Now we'll use two possible ways to show this Exception. First - and coolest by far - is a Error entry in the logviewer.
            // But this requires the logviewer to work, which could get difficult while startup
            if (AwesomeViewerStolenFromTheInternet.ReadyToShowErrorMessages)
            {
                AwesomeViewerStolenFromTheInternet.LogException(ex);
                e.Handled = true;
            }

            else
            {
                // Then we'll just add all the error messages to one nice report
                var errMsg = "";
                var inner = ex;
                while (inner != null)
                {
                    errMsg += ex.Message + Environment.NewLine;
                    inner = inner.InnerException;
                }

                // And post it in a MessageBox - "Ok" says "go on", "Cancel" says exit the program. The latter is useful when you got
                // something that spams Exceptions.
                e.Handled = MessageBox.Show(ex.Message, "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.OK;
            }
        }
    }
}
