using System.Collections.Generic;
using System.Linq;
using Gallio.Model.Filters;
using Gallio.Runner;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.Gallio
{
    public class TestRunner
    {
        private readonly ITestCaseFactory testCaseFactory;
        private readonly ITestResultFactory testResultFactory;
        private readonly TestProperty testIdProperty;
        private readonly TestProperty filePathProperty;
        private TestLauncher launcher;

        public TestRunner(ITestCaseFactory testCaseFactory, ITestResultFactory testResultFactory, 
            TestProperty testIdProperty, TestProperty filePathProperty)
        {
            this.testCaseFactory = testCaseFactory;
            this.testResultFactory = testResultFactory;
            this.testIdProperty = testIdProperty;
            this.filePathProperty = filePathProperty;
        }

        public void Cancel()
        {
            launcher.Cancel();
        }

        public void RunTests(IEnumerable<string> sources, ITestExecutionRecorder testExecutionRecorder)
        {
            launcher = new TestLauncher();
            
            foreach (var source in sources)
            {
                launcher.AddFilePattern(source);
            }

            RunTests(testExecutionRecorder);
        }

        private void RunTests(ITestExecutionRecorder testExecutionRecorder)
        {
            launcher.TestProject.AddTestRunnerExtension(new VSTestWindowExtension(testExecutionRecorder, testCaseFactory, testResultFactory));
            launcher.Run();
        }

        public void RunTests(IEnumerable<TestCase> tests, ITestExecutionRecorder testExecutionRecorder)
        {
            launcher = new TestLauncher();

            foreach (var test in tests)
            {
                var filePath = test.GetPropertyValue(filePathProperty, "");
                launcher.AddFilePattern(filePath);
            }

            SetTestFilter(tests);

            RunTests(testExecutionRecorder);

        }

        private void SetTestFilter(IEnumerable<TestCase> tests)
        {
            var filters = tests.Select(t => new EqualityFilter<string>(t.GetPropertyValue(testIdProperty).ToString())).ToArray();
            var filterSet = new FilterSet<ITestDescriptor>(new IdFilter<ITestDescriptor>(new OrFilter<string>(filters)));
            launcher.TestExecutionOptions.FilterSet = filterSet;
        } 
    }
}