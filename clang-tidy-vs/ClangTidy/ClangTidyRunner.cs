using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using System.IO;
using System.Reflection;
using System.ComponentModel;

namespace LLVM.ClangTidy
{
    /// <summary>
    /// Launches clang-tidy.exe, waits for results and displays them in output window
    /// </summary>
    public static class ClangTidyRunner
    {
        private static readonly string ClangTidyExeName = "clang-tidy.exe";
        private static Guid OutputWindowGuid = new Guid(GuidList.guidClangTidyOutputWndString);
        private static readonly string OutputWindowTitle = "Clang Tidy";
        private static IVsOutputWindowPane OutputWindowPane;
        private static string ExtensionDirPath;
        private static BackgroundWorker InfoWorker;

        public static void RunClangTidyProcess()
        {
            InitOutputWindow();

            ForceOutputWindowToFront();

            string activeDocumentFullPath = Utility.GetActiveSourceFileFullPath(true);

            if (activeDocumentFullPath != null)
            {
                string arguments = "-header-filter=" + Utility.GetActiveSourceFileHeaderName();
                arguments += " " + activeDocumentFullPath;

                if (StartBackgroundInfoWorker())
                {
                    OutputWindowPane.OutputStringThreadSafe(">> Running " + ClangTidyExeName + " with arguments: '" + arguments + "'\n");

                    BackgroundThreadWorker worker = new BackgroundThreadWorker(ExtensionDirPath + "\\" + ClangTidyExeName, arguments);
                    worker.ThreadDone += HandleThreadFinished;

                    System.Threading.Thread workerThread = new System.Threading.Thread(worker.Run);
                    workerThread.Start();
                }
            }
            else
            {
                OutputWindowPane.OutputStringThreadSafe(">> No source file available!");
            }
        }

        private static void HandleThreadFinished(object sender, EventArgs out_args)
        {
            InfoWorker.CancelAsync();

            ValidationResultFormatter.AcquireTagsFromOutput((out_args as OutputEventArgs).Output);
            ValidationClassifier.InvalidateActiveClassifier();

            while (InfoWorker.CancellationPending) { System.Threading.Thread.Sleep(50); }

            OutputWindowPane.OutputStringThreadSafe("\n");
            OutputWindowPane.OutputStringThreadSafe(ValidationResultFormatter.FormatOutputWindowMessage((out_args as OutputEventArgs).Output));
            OutputWindowPane.OutputStringThreadSafe(">> Finished");
        }

        /// <summary>
        /// Background worker is a simple thread responsible with updating output window 
        /// (tell user something is happening in background) while clang-tidy thread does it's job.
        /// </summary>
        private static bool StartBackgroundInfoWorker()
        {
            if (InfoWorker != null && (InfoWorker.IsBusy || InfoWorker.CancellationPending))
                return false;

            if (InfoWorker == null)
                InfoWorker = new BackgroundWorker();

            InfoWorker.WorkerReportsProgress = true;
            InfoWorker.WorkerSupportsCancellation = true;

            InfoWorker.DoWork += new DoWorkEventHandler(BackgroundWorkerDoWork);
            InfoWorker.ProgressChanged += new ProgressChangedEventHandler(BackgroundWorkerUpdateProgress);
            InfoWorker.RunWorkerAsync();

            return true;
        }

        private static void BackgroundWorkerDoWork(object sender, DoWorkEventArgs args)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            int i = 0;

            while (true)
            {
                if (worker.CancellationPending == true)
                {
                    break;
                }
                else
                {
                    System.Threading.Thread.Sleep(500);
                    i++;
                    worker.ReportProgress(i);
                }
            }
        }

        /// <summary>
        /// Just put comma every now and then to ensure user clang-tidy is still working
        /// </summary>
        private static void BackgroundWorkerUpdateProgress(object sender, ProgressChangedEventArgs args)
        {
            OutputWindowPane.OutputStringThreadSafe(".");
        }

        private static void InitOutputWindow()
        {
            if (OutputWindowPane == null)
            {
                IVsOutputWindow out_window = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                out_window.CreatePane(ref OutputWindowGuid, OutputWindowTitle, 1, 1);
                out_window.GetPane(ref OutputWindowGuid, out OutputWindowPane);
            }

            if (ExtensionDirPath == null)
            {
                string code_base = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(code_base);
                string path = Uri.UnescapeDataString(uri.Path);
                ExtensionDirPath = Path.GetDirectoryName(path);
            }

            OutputWindowPane.Clear();
            OutputWindowPane.Activate();
        }

        private static void ForceOutputWindowToFront()
        {
            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            dte.ExecuteCommand("View.Output");
        }
    }
}
