using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private TestLauncher _launcher = null;
  
        public GallioAdapter()
        {
             LoaderManager.InitializeAndSetupRuntimeIfNeeded();
        }

        #region Test Discovery
        
        public void DiscoverTests(IEnumerable<string> sources, ObjectModel.Logging.IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            var frameworkLogger = new TestFrameworkLogger(logger);

            try
            {

                frameworkLogger.Log(LogSeverity.Info, "Gallio starting up");

                var diaMap = new Dictionary<string, DiaSession>();

                var gallioTestscases = GetGallioTestcases(frameworkLogger, sources);
                if (gallioTestscases != null)
                {
                    frameworkLogger.Log(LogSeverity.Info, "Found " + gallioTestscases.Count().ToString());
                    MapGallioTestCases(gallioTestscases, frameworkLogger, discoverySink, sources);
                }
            }
            catch (Exception ex)
            {
                frameworkLogger.Log(LogSeverity.Error, String.Format("Gallio: Exception discovering tests from {0}", ex));
            }
        }
          

        private void MapGallioTestCases(IEnumerable<TestData> gallioTestCases, TestFrameworkLogger logger, ITestCaseDiscoverySink discoverySink, IEnumerable<string> sources)
        {
            foreach(var gallioTestCase in gallioTestCases)
            {
                CreateTestCase(gallioTestCase, discoverySink, logger, sources);

                if(gallioTestCase.Children.Count > 0)
                 {
                     MapGallioTestCases(gallioTestCase.AllTests, logger, discoverySink, sources);
                 }
            }
        }

        private void CreateTestCase(TestData testData, ITestCaseDiscoverySink discoverySink, TestFrameworkLogger logger, IEnumerable<string> sources)
        {
            TestCase testCase = GetTestCase(testData, sources);
            
            logger.Log(LogSeverity.Info, "Sending to the sink");

            discoverySink.SendTestCase(testCase);
            

        }

        internal TestCase GetTestCase(TestData testData, IEnumerable<string> sources)
        {
            TestCase testCase = new TestCase(testData.FullName, new Uri(ExecutorUri));
            testCase.DisplayName = testData.CodeElement.Name;
            testCase.Source = testData.CodeReference.AssemblyName;
            testCase.CodeFilePath = testData.CodeLocation.Path;
            testCase.LineNumber = testData.CodeLocation.Line;


            int count = 0;
            foreach (var source in sources)
            {
                count += 1;
                var tp = CreateTestProperty("fileid" + count, "filelabel" + count);
                testCase.SetPropertyValue<string>(tp, source);
            }

            var testProperty = TestProperty.Register("filecount", "filecount", typeof(int), typeof(int));
            testCase.SetPropertyValue<int>(testProperty, count);

            return testCase;
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

        void ITestExecutor.RunTests(IEnumerable<string> sources, IRunContext runContext, ITestExecutionRecorder testExecutionRecorder)
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

            _launcher.TestProject.AddTestRunnerExtension(new VSTestWindowExtension(testExecutionRecorder));

            TestLauncherResult testLauncherResult = _launcher.Run();

            
        }

              void ITestExecutor.RunTests(IEnumerable<TestCase> tests, IRunContext runContext, ITestExecutionRecorder testExecutionRecorder)
        {
            _launcher = new TestLauncher();

            foreach (var test in tests)
            {              
                var testProperty = TestProperty.Register("filecount", "filecount", typeof(int), typeof(int));
                var testCount = test.GetPropertyValue<int>(testProperty, 0);

                for (int i = 0; i < testCount; i++)
                {
                    var tp = CreateTestProperty("fileid" + testCount, "filelabel" + testCount);
                    var file = test.GetPropertyValue<string>(tp, "");
                    _launcher.AddFilePattern(file);
                }
                    
                //_launcher.TestExecutionOptions.FilterSet = FilterUtils.ParseTestFilterSet("ExactType:" + test.DisplayName);
            }

            RunTests(testExecutionRecorder);
         
        }

        private TestProperty CreateTestProperty(string id, string label)
        {
            var testProperty = TestProperty.Register(id, label, typeof(string), typeof(string));
            return testProperty;
        }
    }
}
