using System;
using Gallio.Model;
using Gallio.Model.Schema;
using Gallio.Runner.Reports.Schema;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TestOutcome = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome;
using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace Microsoft.VisualStudio.TestPlatform.Gallio
{
    public class TestResultFactory : ITestResultFactory
    {
        public TestResult BuildTestResult(TestData test, TestStepRun stepRun, TestCase testCase)
        {
            var testResult = new TestResult(testCase)
            {
                DisplayName = test.Name,
                ErrorLineNumber = test.CodeLocation.Line,
                StartTime = stepRun.StartTime,
                EndTime = stepRun.EndTime,
                Duration = stepRun.Result.Duration,
                Outcome = GetOutcome(stepRun.Result.Outcome.Status),
                //ErrorStackTrace = ?,
            };

            if (stepRun.TestLog.Streams.Count > 0)
            {
                testResult.ErrorMessage = stepRun.TestLog.Streams[0].ToString();
            }

            return testResult;
        }

        private static TestOutcome GetOutcome(TestStatus testStatus)
        {
            switch (testStatus)
            {
                case TestStatus.Passed:
                    return TestOutcome.Passed;
                case TestStatus.Failed:
                    return TestOutcome.Failed;
                case TestStatus.Skipped:
                    return TestOutcome.Skipped;
                case TestStatus.Inconclusive:
                    return TestOutcome.NotFound;
                default:
                    throw new ArgumentException("Unexpected test status");
            }
        }
    }
}