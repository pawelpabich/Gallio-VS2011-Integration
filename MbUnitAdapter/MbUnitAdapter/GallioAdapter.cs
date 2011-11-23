using System.Collections.Generic;
using Gallio.Loader;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TestPlatform.Gallio
{
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    [DefaultExecutorUri(ExecutorUri)]
    [ExtensionUri(ExecutorUri)]
    public class GallioAdapter : ITestDiscoverer, ITestExecutor
    {
        public const string ExecutorUri = "executor://www.mbunit.com/GallioAdapter";

        private readonly TestProperty testIdProperty;
        private readonly TestProperty filePathProperty;
        private readonly ITestCaseFactory testCaseFactory;
        private readonly ITestResultFactory testResultFactory;
        private readonly TestRunner testRunner;
        private readonly TestExplorer testExplorer;

        public GallioAdapter()
        {
             LoaderManager.InitializeAndSetupRuntimeIfNeeded();

             testIdProperty = TestProperty.Register("Gallio.TestId", "Test id", typeof(string), typeof(TestCase));
             filePathProperty = TestProperty.Register("Gallio.FilePath", "File path", typeof(string), typeof(TestCase));

            testCaseFactory = new CachingTestCaseFactory(new TestCaseFactory(testIdProperty, filePathProperty));
            testResultFactory = new TestResultFactory();

            testExplorer = new TestExplorer(testCaseFactory);
            testRunner = new TestRunner(testCaseFactory, testResultFactory, testIdProperty, filePathProperty);
        }

        public void DiscoverTests(IEnumerable<string> sources, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            testExplorer.DiscoverTests(sources, logger, discoverySink);
        }

        public void Cancel()
        {           
            testRunner.Cancel();
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, ITestExecutionRecorder testExecutionRecorder)
        {
            testRunner.RunTests(sources, runContext, testExecutionRecorder);
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, ITestExecutionRecorder testExecutionRecorder)
        {
            testRunner.RunTests(tests, runContext, testExecutionRecorder);
        }
    }
}
