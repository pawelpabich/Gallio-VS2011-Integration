using System.Collections.Generic;
using Gallio.Loader;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
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
        private readonly CachingTestCaseFactory cachingTestCaseFactory;
        private readonly ITestResultFactory testResultFactory;
        private readonly TestRunner testRunner;
        private readonly TestExplorer testExplorer;
        private readonly TestCaseFactory testCaseFactory;

        public GallioAdapter()
        {

            LoaderManager.InitializeAndSetupRuntimeIfNeeded();

            testIdProperty = TestProperty.Register("Gallio.TestId", "Test id", typeof(string), typeof(TestCase));

            testCaseFactory = new TestCaseFactory(testIdProperty);
            cachingTestCaseFactory = new CachingTestCaseFactory(testCaseFactory, testIdProperty);
            testResultFactory = new TestResultFactory();

            testExplorer = new TestExplorer(cachingTestCaseFactory);
            testRunner = new TestRunner(cachingTestCaseFactory, testResultFactory, testIdProperty);
        }


        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            cachingTestCaseFactory.AddTestCases(tests);
            testRunner.RunTests(tests, runContext, frameworkHandle);
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            testCaseFactory.AddSources(sources);
            testRunner.RunTests(sources, runContext, frameworkHandle);
        }

        public void Cancel()
        {
            testRunner.Cancel();
        }

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {           
            //testExplorer.DiscoverTests(sources, logger, discoverySink);
            testCaseFactory.AddSources(sources);
            testExplorer.DiscoverTestsWithoutLockingDlls(sources, logger, discoverySink);
        }
    }
}
