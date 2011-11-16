using System;
using Gallio.Common.Markup;
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
        public TestResult BuildTestResult(TestData testData, TestStepRun testStepRun, TestCase testCase)
        {
            return new TestResult(testCase)
            {
                DisplayName = testData.Name,
                ErrorLineNumber = testData.CodeLocation.Line,
                StartTime = testStepRun.StartTime,
                EndTime = testStepRun.EndTime,
                Duration = testStepRun.Result.Duration,
                Outcome = GetOutcome(testStepRun.Result.Outcome.Status),
                ErrorMessage = GetErrorMessage(testStepRun),
                //ErrorStackTrace = ?,
            };
        }

        private static string GetErrorMessage(TestStepRun stepRun)
        {
            var failuresStream = stepRun.TestLog.GetStream(MarkupStreamNames.Failures);
            return failuresStream != null ? failuresStream.ToString() : "";
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