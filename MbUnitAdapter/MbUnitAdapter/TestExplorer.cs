using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gallio.Common.Reflection;
using Gallio.Model;
using Gallio.Model.Messages.Exploration;
using Gallio.Model.Schema;
using Gallio.Runner;
using Gallio.Runtime;
using Gallio.Runtime.Logging;
using Gallio.Runtime.ProgressMonitoring;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Gallio.Common.Messaging.MessageSinks;

namespace TestPlatform.Gallio
{
    public class TestExplorer
    {
        private const string MsTestFrameworkHandleId = "MSTestAdapter.TestFramework";
        private const string NUnitTestFrameworkHandleId = "NUnitAdapter.TestFramework";
        private readonly TestCaseFactory testCaseFactory;

        public TestExplorer(TestCaseFactory testCaseFactory)
        {
            this.testCaseFactory = testCaseFactory;
        }

        public void DiscoverTests(IEnumerable<string> sources, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            var frameworkLogger = new TestFrameworkLogger(logger);

            try
            {
                frameworkLogger.Log(LogSeverity.Info, "Gallio starting up");

                var tests = Explore(sources, frameworkLogger);
                
                if (tests == null)
                    return;
             
                frameworkLogger.Log(LogSeverity.Info, "Found " + tests.Count());
                PublishTests(tests, discoverySink);
            }
            catch (Exception ex)
            {
                frameworkLogger.Log(LogSeverity.Error, String.Format("Gallio: Exception discovering tests from {0}", ex));
            }
        }

        private static IList<TestData> Explore(IEnumerable<string> sources, ILogger frameworkLogger)
        {
            try
            {
                var testDriver = GetTestDriver(frameworkLogger);

                var tests = new List<TestData>();
                var messageConsumer = new MessageConsumer()
                    .Handle<TestDiscoveredMessage>(message =>
                    {
                        if (message.Test.IsTestCase)
                            tests.Add(message.Test);
                    })
                    .Handle<AnnotationDiscoveredMessage>(message => message.Annotation.Log(frameworkLogger, true));

                var loader = new ReflectionOnlyAssemblyLoader();
                var assemblyInfos = sources.Select(source => LoadAssembly(source, loader)).Where(assembly => assembly != null).ToList();
                var testExplorationOptions = new TestExplorationOptions();

                testDriver.Describe(loader.ReflectionPolicy, assemblyInfos, testExplorationOptions, messageConsumer, NullProgressMonitor.CreateInstance());

                return ResetCollectionForExposedTests(tests) ? null : tests;
            }
            catch (Exception ex)
            {
                frameworkLogger.Log(LogSeverity.Error, "Gallio failed to load tests", ex);
                return null;
            }
        }

        private static ITestDriver GetTestDriver(ILogger frameworkLogger)
        {
            var testFrameworkManager = RuntimeAccessor.ServiceLocator.Resolve<ITestFrameworkManager>();

            var testFrameworkSelector = new TestFrameworkSelector
            {
                Filter = testFrameworkHandle => NotMsTestOrNunit(testFrameworkHandle.Id),
                FallbackMode = TestFrameworkFallbackMode.Approximate
            };

            var testDriver = testFrameworkManager.GetTestDriver(testFrameworkSelector, frameworkLogger);
            return testDriver;
        }

        private static bool NotMsTestOrNunit(string testFrameworkHandleId)
        {
            return testFrameworkHandleId != MsTestFrameworkHandleId || testFrameworkHandleId != NUnitTestFrameworkHandleId;
        }

        private static ICodeElementInfo LoadAssembly(string source, ReflectionOnlyAssemblyLoader loader)
        {
            loader.AddHintDirectory(Path.GetDirectoryName(source));
            return loader.ReflectionPolicy.LoadAssemblyFrom(source);
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

        private void PublishTests(IEnumerable<TestData> tests, ITestCaseDiscoverySink discoverySink)
        {
            foreach (var test in tests)
            {
                var testCase = testCaseFactory.GetTestCase(test);
                discoverySink.SendTestCase(testCase);

                if (test.Children.Count > 0)
                {
                    PublishTests(test.AllTests, discoverySink);
                }
            }
        }

        public void DiscoverTestsWithoutLockingDlls(IEnumerable<string> sources, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            var frameworkLogger = new TestFrameworkLogger(logger);
            try
            {
                var testRunnerManager = RuntimeAccessor.ServiceLocator.Resolve<ITestRunnerManager>();
                // Do not use Local because then the dlls with tests will be locked by the discovery process
                var testRunnerFactory = testRunnerManager.GetFactory(StandardTestRunnerFactoryNames.IsolatedAppDomain);
                var runner = testRunnerFactory.CreateTestRunner();
                runner.Initialize(new TestRunnerOptions(), frameworkLogger, new ObservableProgressMonitor());
                var testPackage = CreateTestPackage(sources);
                var tests = FindTests(testPackage, runner);
                testCaseFactory.AddSources(sources);
                SendTestCasesToVisualStudio(discoverySink, tests);
            }
            catch (Exception ex)
            {
                frameworkLogger.Log(LogSeverity.Error, "Gallio failed to load tests", ex);
            }
        }

        private void SendTestCasesToVisualStudio(ITestCaseDiscoverySink discoverySink, List<TestData> tests)
        {
            foreach (var test in tests)
            {
                var vsTest = testCaseFactory.GetTestCase(test);
                discoverySink.SendTestCase(vsTest);
            }
        }

        private static List<TestData> FindTests(TestPackage testPackage, ITestRunner runner)
        {
            var options = new TestExplorationOptions();
            testPackage.AddExcludedTestFrameworkId(MsTestFrameworkHandleId);
            testPackage.AddExcludedTestFrameworkId(NUnitTestFrameworkHandleId);
            var result = runner.Explore(testPackage, options, new ObservableProgressMonitor());
            var tests = result.TestModel.AllTests.Where(t => t.IsTestCase).ToList();
            return tests;
        }

        private static TestPackage CreateTestPackage(IEnumerable<string> sources)
        {
            var testPackage = new TestPackage();
            foreach (var source in sources)
            {
                testPackage.AddFile(new FileInfo(source));
            }
            return testPackage;
        }
    }
}