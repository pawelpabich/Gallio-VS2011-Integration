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
            foreach(var gallioTestCase in gallioTestCases)
            {
                CreateTestCase(gallioTestCase, discoverySink, logger);

                if(gallioTestCase.Children.Count > 0)
                 {
                     MapGallioTestCases(gallioTestCase.AllTests, logger, discoverySink);
                 }
            }
        }

        private void CreateTestCase(TestData testData, ITestCaseDiscoverySink discoverySink, TestFrameworkLogger logger)
        {
            TestCase testCase = GetTestCase(testData);

            testCase.CodeFilePath = testData.CodeLocation.Path;
            testCase.LineNumber = testData.CodeLocation.Line;

            logger.Log(LogSeverity.Info, "Sending to the sink");

            discoverySink.SendTestCase(testCase);
            

        }

        internal static TestCase GetTestCase(TestData testData)
        {
            TestCase testCase = new TestCase(testData.FullName, new Uri(ExecutorUri));
            testCase.DisplayName = testData.CodeElement.Name;
            testCase.Source = testData.ToTest().so

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
            TestLauncherResult testLauncherResult = _launcher.Run();
            
            foreach (var allTestStepRun in testLauncherResult.Report.TestPackageRun.AllTestStepRuns)
            {
                TestCase tc = new TestCase(allTestStepRun.Step.FullName, new Uri(ExecutorUri));
                ObjectModel.TestResult testResult = new ObjectModel.TestResult(tc);
                testResult.DisplayName = allTestStepRun.Step.FullName;
                testResult.ErrorLineNumber = allTestStepRun.Step.CodeLocation.Line;
                //testResult.ErrorStackTrace
                testResult.StartTime = allTestStepRun.StartTime;
                //testResult.ErrorMessage -- why this and messages?
                testResult.EndTime = allTestStepRun.EndTime;
                //testResult.Attachments = allTestStepRun.TestLog.Attachments; -- can I append my own?
                testResult.Duration = allTestStepRun.Result.Duration;
                //testResult.Messages =
              
                var testStatus = allTestStepRun.Result.Outcome.Status;
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
                    case TestStatus.Inconclusive: //is this right to use not found?
                        testResult.Outcome = ObjectModel.TestOutcome.Skipped;
                        break;
                }

                testExecutionRecorder.RecordResult(testResult);

            }
        }

        //not picking up
        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, ITestExecutionRecorder testExecutionRecorder)
        {
            _launcher = new TestLauncher();
            _launcher.RuntimeSetup = new RuntimeSetup();
            //_launcher.TestProject. 

            foreach (var test in tests)
            {
                _launcher.AddFilePattern(test.Source);
                _launcher.TestExecutionOptions.FilterSet = FilterUtils.ParseTestFilterSet("ExactType:" + test.DisplayName);
            }

            RunTests(testExecutionRecorder);
           
        }
    }
}
