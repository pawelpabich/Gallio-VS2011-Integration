using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gallio.Runtime.ProgressMonitoring;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.Gallio
{
    public class ProgressMonitorProvider : IProgressMonitorProvider
    {
        private ITestExecutionRecorder _executionRecorder;

        public ProgressMonitorProvider(ITestExecutionRecorder executionRecorder)
        {
            _executionRecorder = executionRecorder;
        }

       
        public T Run<T>(TaskWithProgress<T> task)
        {
            IProgressMonitorPresenter presenter = new ProgressMonitorPresenter(_executionRecorder);
            using (var progressMonitor = new ObservableProgressMonitor())
            {
                presenter.Present(progressMonitor);
                progressMonitor.ThrowIfCanceled();
                T result = task(progressMonitor);
                progressMonitor.ThrowIfCanceled();
                return result;
            }
        }

        public void Run(TaskWithProgress task)
        {
            Run(ProgressMonitor =>
            {
                task(ProgressMonitor);
                return 0;
            });
        }
    }
}
