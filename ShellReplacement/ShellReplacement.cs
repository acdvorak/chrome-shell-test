using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ShellReplacement
{
    public class ShellReplacement
    {
        private string _browserPath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
        private string _homepageUrl = "http://www.google.com/";

        #region Public methods

        public void LaunchChrome()
        {
            // Commenting these 2 lines out will produce blank white screens in Chrome 23 thru 28
            if (!ExplorerProcesses.Any())
                RegisterShellWindow();

            try
            {
                StartChromeProcess();
            }
            finally
            {
                // Not sure if this is necessary, but it can't hurt
                if (!ExplorerProcesses.Any())
                    UnregisterShellWindow();
            }
        }

        #endregion

        #region Process handling

        private static IEnumerable<Process> ExplorerProcesses
        {
            get
            {
                return Process.GetProcessesByName("explorer").Where(IsValidProcess);
            }
        }

        private static IEnumerable<Process> ChromeProcesses
        {
            get
            {
                return Process.GetProcessesByName("chrome").Where(IsValidProcess);
            }
        }

        private static bool IsValidProcess(Process process)
        {
            try
            {
                return !process.HasExited && process.MainWindowHandle != IntPtr.Zero;
            }
            catch (Win32Exception)
            {
                // Ignore processes running w/ elevated persmissions or started by other users
                return false;
            }
        }

        private void StartChromeProcess()
        {
            var chromeProcess =
                Process.Start(new ProcessStartInfo(_browserPath,
                                                   string.Format(
                                                       "--kiosk --kiosk-printing --no-first-run --auto-launch-at-startup --bwsi --disable-popup-blocking --disable-restore-session-state --disable-sync --disable-translate \"{0}\"",
                                                       _homepageUrl)));
            while (chromeProcess != null)
            {
                chromeProcess.WaitForExit();
                chromeProcess = ChromeProcesses.FirstOrDefault();
            }
        }

        #endregion

        #region Win32 Interop

        /// <summary>
        /// When the explorer.exe shell process is not running, registers a handle to a <see cref="NativeWindow"/>
        /// as the top-level "shell window" that will be returned by subsequent calls to <code>user32.dll::GetShellWindow()</code>.
        /// This fixes a bug introduced in Chrome 23 wherein the calls to <code>user32.dll::GetShellWindow()</code> return <code>NULL</code> when explorer.exe is not running,
        /// which in turn causes GPU acceleration to fail and results in blank white screens in Chrome.
        /// </summary>
        /// <remarks>
        /// After calling this method, when the .NET application terminates, explorer.exe will be automatically restarted by Windows unless
        /// <code>HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\AutoRestartShell</code> is <code>0</code>.
        /// See <see cref="http://technet.microsoft.com/en-us/library/cc939703.aspx"/> for more information.
        /// </remarks>
        /// <returns><code>true</code> if the call to <code>user32.dll::SetShellWindow(hWnd)</code> succeeded, or <code>false</code> if it was unable to set the shell window handle</returns>
        /// <seealso cref="http://crbug.com/169652#c43"/>
        /// <seealso cref="http://www.winehq.org/pipermail/wine-devel/2003-October/021368.html"/>
        /// <seealso cref="http://technet.microsoft.com/en-us/library/cc939703.aspx"/>
        public bool RegisterShellWindow()
        {
            return SetShellWindow(NativeShellWindow.Instance.Handle);
        }

        /// <summary>
        /// When the explorer.exe shell process is not running, unregisters the handle to the <see cref="NativeWindow"/>
        /// as the top-level "shell window" such that subsequent calls to <code>user32.dll::GetShellWindow()</code> will return <code>NULL</code>.
        /// </summary>
        /// <returns><code>true</code> if the call to <code>user32.dll::SetShellWindow(NULL)</code> succeeded, or <code>false</code> if it was unable to reset the shell window handle</returns>
        /// <seealso cref="http://www.winehq.org/pipermail/wine-devel/2003-October/021368.html"/>
        public bool UnregisterShellWindow()
        {
            return SetShellWindow(IntPtr.Zero);
        }

        /// <summary>
        /// Undocumented Windows kernel function that sets the window handle returned by subsequent calls to <code>user32.dll::GetShellWindow()</code>.
        /// </summary>
        /// <remarks>
        /// After calling this method (and passing anything other than <see cref="IntPtr.Zero"/>), when the .NET application terminates,
        /// explorer.exe will be automatically restarted by Windows unless
        /// <code>HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\AutoRestartShell</code> is <code>0</code>.
        /// See <see cref="http://technet.microsoft.com/en-us/library/cc939703.aspx"/> for more information.
        /// </remarks>
        /// <param name="hWnd">Handle to a <see cref="NativeWindow"/>, or <see cref="IntPtr.Zero"/> to reset the shell window</param>
        /// <returns><code>true</code> if the call to <code>user32.dll::SetShellWindow(hWnd)</code> succeeded, or <code>false</code> if it was unable to set the shell window handle</returns>
        /// <seealso cref="http://crbug.com/169652#c43"/>
        /// <seealso cref="http://www.winehq.org/pipermail/wine-devel/2003-October/021368.html"/>
        /// <seealso cref="http://technet.microsoft.com/en-us/library/cc939703.aspx"/>
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetShellWindow(IntPtr hWnd);

        #endregion
    }

    sealed class NativeShellWindow : NativeWindow
    {
        private NativeShellWindow()
        {
            CreateHandle(new CreateParams());
        }

        public static readonly NativeShellWindow Instance = new NativeShellWindow();
    }
}
