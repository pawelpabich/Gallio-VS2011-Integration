using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gallio.Runtime.ProgressMonitoring;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.Gallio
{
    public class ProgressMonitorPresenter : IProgressMonitorPresenter
    {
        private ITestExecutionRecorder _executionRecorder;

        public ProgressMonitorPresenter(ITestExecutionRecorder executionRecorder)
        {
            _executionRecorder = executionRecorder;          
        }

        public void Present(ObservableProgressMonitor progressMonitor)
        {
            progressMonitor.TaskStarting += progressMonitor_TaskStarting;
            progressMonitor.TaskFinished += progressMonitor_TaskFinished;
        }

        void progressMonitor_TaskFinished(object sender, EventArgs e)
        {
             //_executionRecorder.RecordEnd
        }

        void progressMonitor_TaskStarting(object sender, EventArgs e)
        {
             //_executionRecorder.RecordStart(
        }
    }
}
