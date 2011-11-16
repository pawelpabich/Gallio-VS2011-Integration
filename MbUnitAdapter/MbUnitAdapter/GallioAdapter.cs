using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gallio.Common.Messaging;
using Gallio.Common.Reflection;
using Gallio.Loader;
using Gallio.Model;
using Gallio.Model.Filters;
using Gallio.Model.Messages.Exploration;
using Gallio.Model.Schema;
using Gallio.Runner;
using Gallio.Runtime;
using Gallio.Runtime.Logging;
using Gallio.Runtime.ProgressMonitoring;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.Gallio
{
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    [DefaultExecutorUri(ExecutorUri)]
    [ExtensionUri(ExecutorUri)]
    public class GallioAdapter : ITestDiscoverer, ITestExecutor
    {
        public const string ExecutorUri = "executor://www.mbunit.com/GallioAdapter";
        private TestLauncher _launcher;
        private readonly TestProperty testIdProperty;
        private readonly TestProperty filePathProperty;
        private readonly TestCaseFactory testCaseFactory;

        public GallioAdapter()
        {
             LoaderManager.InitializeAndSetupRuntimeIfNeeded();

             testIdProperty = TestProperty.Register("Gallio.TestId", "Test id", typeof(string), typeof(TestCase));
             filePathProperty = TestProperty.Register("Gallio.FilePath", "File path", typeof(string), typeof(TestCase));

            testCaseFactory = new TestCaseFactory(testIdProperty, filePathProperty);
        }

        #region Test Discovery
        
        public void DiscoverTests(IEnumerable<string> sources, ObjectModel.Logging.IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            var frameworkLogger = new TestFrameworkLogger(logger);

            try
            {

                frameworkLogger.Log(LogSeverity.Info, "Gallio starting up");

                var gallioTestscases = GetGallioTestcases(frameworkLogger, sources);
                if (gallioTestscases != null)
                {
                    frameworkLogger.Log(LogSeverity.Info, "Found " + gallioTestscases.Count().ToString());
                    MapGallioTestCases(gallioTestscases, frameworkLogger, discoverySink);
                }
            }
            catch (Exception ex)
            {
                frameworkLogger.Log(LogSeverity.Error, String.Format("Gallio: Exception discovering tests from {0}", ex));
            }
        }

        private void MapGallioTestCases(IEnumerable<TestData> gallioTestCases, TestFrameworkLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            foreach (var gallioTestCase in gallioTestCases)
            {
                var testCase = testCaseFactory.GetTestCase(gallioTestCase);
                discoverySink.SendTestCase(testCase);

                if (gallioTestCase.Children.Count > 0)
                {
                    MapGallioTestCases(gallioTestCase.AllTests, logger, discoverySink);
                }
            }
        }

        private ICodeElementInfo LoadAssembly(string source, ReflectionOnlyAssemblyLoader loader)
        {
            loader.AddHintDirectory(Path.GetDirectoryName(source));
            return loader.ReflectionPolicy.LoadAssemblyFrom(source);
        }

        private IEnumerable<TestData> GetGallioTestcases(TestFrameworkLogger frameworkLogger, IEnumerable<string> sources)
        {
            try
            {
                var testFrameworkManager = RuntimeAccessor.ServiceLocator.Resolve<ITestFrameworkManager>();
                var loader = new ReflectionOnlyAssemblyLoader();

                IList<ICodeElementInfo> assemblyInfos = sources.Select(source => LoadAssembly(source, loader)).Where(assembly => assembly != null).ToList();
                
                var testFrameworkSelector = new TestFrameworkSelector()
                {
                    Filter = testFrameworkHandle => testFrameworkHandle.Id != "MSTestAdapter.TestFramework" || testFrameworkHandle.Id != "NUnitAdapter.TestFramework",
                   
                    FallbackMode = TestFrameworkFallbackMode.Approximate
                };

                ITestDriver driver = testFrameworkManager.GetTestDriver(testFrameworkSelector, frameworkLogger);
                var testExplorationOptions = new TestExplorationOptions();

                var tests = new List<TestData>();
                MessageConsumer messageConsumer = new MessageConsumer()
                    .Handle<TestDiscoveredMessage>(message =>
                                                       {
                                                           if (message.Test.IsTestCase)
                                                               tests.Add((message.Test));
                                                       })
                    .Handle<AnnotationDiscoveredMessage>(message => message.Annotation.Log(frameworkLogger, true));

                driver.Describe(loader.ReflectionPolicy, assemblyInfos,
                                testExplorationOptions, messageConsumer, NullProgressMonitor.CreateInstance());

                bool reset = ResetCollectionForExposedTests(tests);

                if (reset)
                {
                    tests = null;
                }
             
                return tests;
            }
            catch (Exception ex)
            {
                frameworkLogger.Log(LogSeverity.Error, "Gallio failed to load tests", ex);

                return null;
            }
        }

        /// <summary>
        /// Reset test collection for NUnit\XUnit so we don't confuse the test runner
        /// </summary>
        /// <param name="tests">test collection</param>
        /// <returns>if NUnit or XUnit found then a valid reset</returns>
        private static bool ResetCollectionForExposedTests(IEnumerable<TestData> tests)
        {
            return tests.Any(test => test.FullName.Contains("NUnitTest") || test.FullName.Contains("XUnitTest"));
        }

        #endregion

        public void Cancel()
        {           
            _launcher.Cancel();
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, ITestExecutionRecorder testExecutionRecorder)
        {          
            _launcher = new TestLauncher();
            foreach (string source in sources)
            {
                _launcher.AddFilePattern(source);
            }

            RunTests(testExecutionRecorder);
        }

        private void RunTests(ITestExecutionRecorder testExecutionRecorder)
        {
            //_launcher.TestProject.TestRunnerFactoryName = StandardTestRunnerFactoryNames.IsolatedAppDomain;
            //_launcher.RuntimeSetup = new RuntimeSetup();

            _launcher.TestProject.AddTestRunnerExtension(new VSTestWindowExtension(testExecutionRecorder, testCaseFactory));

            TestLauncherResult testLauncherResult = _launcher.Run();

            
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, ITestExecutionRecorder testExecutionRecorder)
        {
            _launcher = new TestLauncher();

            foreach (var test in tests)
            {
                var filePath = test.GetPropertyValue(filePathProperty, "");
                _launcher.AddFilePattern(filePath);
            }

            SetTestFilter(tests);

            RunTests(testExecutionRecorder);
         
        }

        private void SetTestFilter(IEnumerable<TestCase> tests)
        {
            var filters = tests.Select(t => new EqualityFilter<string>(t.GetPropertyValue(testIdProperty).ToString())).ToArray();
            var filterSet = new FilterSet<ITestDescriptor>(new IdFilter<ITestDescriptor>(new OrFilter<string>(filters)));
            _launcher.TestExecutionOptions.FilterSet = filterSet;
        }
    }
}
