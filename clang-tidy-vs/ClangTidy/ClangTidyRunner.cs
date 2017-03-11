using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LLVM.ClangTidy
{
    class ClangTidyRunner
    {
        private static readonly string ClangTidyExeName = "clang-tidy.exe";
        private static Guid OutputWindowGuid = new Guid(GuidList.guidClangTidyOutputWndString);
        private static readonly string OutputWindowTitle = "Clang Tidy";
        private static IVsOutputWindowPane OutputWindowPane;
        private static string ExtensionDirPath;

        public void RunClangTidyProcess()
        {
            InitOutputWindow();

            ForceOutputWindowToFront();

            OutputWindowPane.Clear();
            OutputWindowPane.Activate();

            string active_document_full_path = GetActiveSourceFileFullPath(true);

            if (active_document_full_path != null)
            {
                string arguments = "-header-filter=" + GetActiveSourceFileHeaderName();// -dump-config ";
                arguments += " " + active_document_full_path;

                OutputWindowPane.OutputStringThreadSafe(">> Running " + ClangTidyExeName + " with arguments: '" + arguments + "'\n");

                BackgroundThreadWorker worker = new BackgroundThreadWorker(ExtensionDirPath + "\\" + ClangTidyExeName, arguments);
                worker.ThreadDone += HandleThreadFinished;

                System.Threading.Thread worker_thread = new System.Threading.Thread(worker.Run);
                worker_thread.Start();
            }
            else
            {
                OutputWindowPane.OutputStringThreadSafe(">> No source file available! <<");
            }
        }

        private void InitOutputWindow()
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
        }

        private void HandleThreadFinished(object sender, EventArgs out_args)
        {
            ValidationResultFormatter.AcquireTagsFromOutput((out_args as OutputEventArgs).Output);
            ValidationClassifier.InvalidateActiveClassifier();

            OutputWindowPane.OutputStringThreadSafe(ValidationResultFormatter.FormatOutputWindowMessage((out_args as OutputEventArgs).Output));
            OutputWindowPane.OutputStringThreadSafe(">> Finished");
        }

        private string GetActiveSourceFileFullPath(bool search_for_cpp_file)
        {
            DTE dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            if (dte.ActiveDocument != null)
            {
                string file_path = dte.ActiveDocument.FullName;
                if (search_for_cpp_file && !file_path.EndsWith(".cpp"))
                {
                    string cpp_file_path = Regex.Replace(file_path, @"\..*$", ".cpp");
                    if (File.Exists(cpp_file_path))
                        return cpp_file_path;
                }

                return file_path;
            }
            else
                return null;
        }

        private string GetActiveSourceFileName()
        {
            DTE dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            if (dte.ActiveDocument != null)
                return dte.ActiveDocument.Name;
            else
                return null;
        }

        private string GetActiveSourceFileHeaderName()
        {
            string file_name = GetActiveSourceFileName();

            if (!string.IsNullOrEmpty(file_name))
            {
                file_name = Regex.Replace(file_name, @"\..*$", ".h");
            }

            return file_name;
        }

        private void ForceOutputWindowToFront()
        {
            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            dte.ExecuteCommand("View.Output");
        }
    }
}
