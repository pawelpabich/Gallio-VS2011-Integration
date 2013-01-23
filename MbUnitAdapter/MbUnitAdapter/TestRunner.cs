using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gallio.Loader;
using Gallio.Model.Filters;
using Gallio.Runner;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace TestPlatform.Gallio
{
    public class TestRunner
    {
        private readonly ITestCaseFactory testCaseFactory;
        private readonly ITestResultFactory testResultFactory;
        private readonly TestProperty testIdProperty;
        private TestLauncher launcher;

        public TestRunner(ITestCaseFactory testCaseFactory, ITestResultFactory testResultFactory, TestProperty testIdProperty)
        {
            this.testCaseFactory = testCaseFactory;
            this.testResultFactory = testResultFactory;
            this.testIdProperty = testIdProperty;
        }

        public void Cancel()
        {
            launcher.Cancel();
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle testExecutionRecorder)
        {
            launcher = new TestLauncher();

            foreach (var source in sources)
            {
                launcher.AddFilePattern(source);
            }

            RunTests(runContext, testExecutionRecorder);
        }

        private void RunTests(IRunContext runContext, IFrameworkHandle testExecutionRecorder)
        {
            try
            {
                Log(Environment.CurrentDirectory +  " " + runContext.SolutionDirectory + " " + runContext.TestRunDirectory);
                Log(Environment.Is64BitProcess + " " + LoaderManager.Loader.RuntimePath);
                // testExecutionRecorder.RecordEnd();
                //if (runContext.InIsolation)
                launcher.TestProject.TestRunnerFactoryName = StandardTestRunnerFactoryNames.IsolatedAppDomain;

                var extension = new VSTestWindowExtension(testExecutionRecorder, testCaseFactory, testResultFactory);

                launcher.TestProject.AddTestRunnerExtension(extension);
                launcher.Run();

            }
            catch (Exception e)
            {
               Log(e.ToString());
               throw;
            }
        }

        private void Log(string message)
        {
            if (!Directory.Exists(@"C:\Addin")) Directory.CreateDirectory("C:\\Addin");
            File.WriteAllText("C:\\Addin\\" + DateTime.Now.Ticks + ".txt", message);
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle testExecutionRecorder)
        {
            launcher = new TestLauncher();

            foreach (var test in tests)
            {
                launcher.AddFilePattern(test.Source);
            }

            SetTestFilter(tests);

            RunTests(runContext, testExecutionRecorder);
        }

        private void SetTestFilter(IEnumerable<TestCase> tests)
        {
            var filters = tests.Select(t => new EqualityFilter<string>(t.GetPropertyValue(testIdProperty).ToString())).ToArray();
            var filterSet = new FilterSet<ITestDescriptor>(new IdFilter<ITestDescriptor>(new OrFilter<string>(filters)));
            launcher.TestExecutionOptions.FilterSet = filterSet;
        } 
    }
}