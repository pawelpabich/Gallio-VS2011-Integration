using System;
using Gallio.Model;
using Gallio.Model.Schema;
using Gallio.Runner.Events;
using Gallio.Runner.Extensions;
using Gallio.Runner.Reports.Schema;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.Gallio
{
    internal class VSTestWindowExtension : TestRunnerExtension
    {
        private readonly ITestExecutionRecorder _executionRecorder;

        public VSTestWindowExtension(ITestExecutionRecorder executionRecorder)
        {
            _executionRecorder = executionRecorder;
        }

        private void LogTestCaseFinished(TestStepFinishedEventArgs e)
        {
            var testCase = CreateTestCase(e.Test);

              var testResult = CreateTest(e.Test, e.TestStepRun, testCase);

              _executionRecorder.RecordEnd(testCase, testResult.Outcome);
              _executionRecorder.RecordResult(testResult);
        }
     
        private void LogTestCaseStarted(TestStepStartedEventArgs e)
        {

            var testCase = CreateTestCase(e.Test);

            var testResult = CreateTest(e.Test, e.TestStepRun, testCase);

            _executionRecorder.RecordStart(testCase);            
        }

        private TestCase CreateTestCase(TestData test)
        {
            TestCase testCase = new TestCase(test.FullName, new Uri(GallioAdapter.ExecutorUri));
            testCase.Source = test.CodeReference.AssemblyName;
            return testCase;
        }

        private ObjectModel.TestResult CreateTest(TestData test, TestStepRun stepRun, TestCase testCase)
        {  
            ObjectModel.TestResult testResult = new ObjectModel.TestResult(testCase);
            testResult.DisplayName = test.Name;
            testResult.ErrorLineNumber = test.CodeLocation.Line;
            //testResult.ErrorStackTrace
            testResult.StartTime = stepRun.StartTime;
            if (stepRun.TestLog.Streams.Count > 0)
            {
                testResult.ErrorMessage = stepRun.TestLog.Streams[0].ToString();
            }
            testResult.EndTime = stepRun.EndTime;
          
            testResult.Duration = stepRun.Result.Duration;

            var testStatus = stepRun.Result.Outcome.Status;
            switch (testStatus)
            {
                case TestStatus.Passed:
                    testResult.Outcome = ObjectModel.TestOutcome.Passed;
                    break;
                case TestStatus.Failed:
                    testResult.Outcome = ObjectModel.TestOutcome.Failed;
                    break;
                case TestStatus.Skipped:
                    testResult.Outcome = ObjectModel.TestOutcome.Skipped;
                    break;
                case TestStatus.Inconclusive: 
                    testResult.Outcome = ObjectModel.TestOutcome.NotFound;
                    break;
            }

            return testResult;
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
