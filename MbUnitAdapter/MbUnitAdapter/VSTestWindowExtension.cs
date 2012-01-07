using Gallio.Runner.Events;
using Gallio.Runner.Extensions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TestPlatform.Gallio
{
    internal class VSTestWindowExtension : TestRunnerExtension
    {
        private readonly ITestExecutionRecorder executionRecorder;
        private readonly ITestCaseFactory testCaseFactory;
        private readonly ITestResultFactory testResultFactory;

        public VSTestWindowExtension(ITestExecutionRecorder executionRecorder, ITestCaseFactory testCaseFactory, ITestResultFactory testResultFactory)
        {
            this.executionRecorder = executionRecorder;
            this.testCaseFactory = testCaseFactory;
            this.testResultFactory = testResultFactory;
        }

        private void LogTestCaseFinished(TestStepFinishedEventArgs e)
        {
            var testCase = testCaseFactory.GetTestCase(e.Test);

              var testResult = testResultFactory.BuildTestResult(e.Test, e.TestStepRun, testCase);

              executionRecorder.RecordEnd(testCase, testResult.Outcome);
              executionRecorder.RecordResult(testResult);
        }
     
        private void LogTestCaseStarted(TestStepStartedEventArgs e)
        {
            var testCase = testCaseFactory.GetTestCase(e.Test);
            executionRecorder.RecordStart(testCase);
        }

        protected override void Initialize()
        {
            Events.TestStepStarted += delegate(object sender, TestStepStartedEventArgs e)
            {
                if (e.TestStepRun.Step.IsTestCase)
                {
                    LogTestCaseStarted(e);
                }
            };

            Events.TestStepFinished += delegate(object sender, TestStepFinishedEventArgs e)
            {
                if (e.TestStepRun.Step.IsTestCase)
                {
                    LogTestCaseFinished(e);
                }
            };
        }
    }
}
