using System;
using System.Collections.Generic;
using Gallio.Common.Reflection;
using Gallio.Model.Schema;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TestPlatform.Gallio
{
    public class TestCaseFactory : ITestCaseFactory
    {
        private readonly TestProperty testIdProperty;
        private readonly TestProperty filePathProperty;

        public TestCaseFactory(TestProperty testIdProperty, TestProperty filePathProperty)
        {
            this.testIdProperty = testIdProperty;
            this.filePathProperty = filePathProperty;
        }

        public TestCase GetTestCase(TestData testData, IEnumerable<string> sources)
        {
            var testCase = new TestCase(testData.FullName, new Uri(GallioAdapter.ExecutorUri))
            {
                CodeFilePath = testData.CodeLocation.Path,
                LineNumber = testData.CodeLocation.Line
            };

            if (testData.CodeElement == null)
            {
                foreach(var source in sources)
                {
                    testCase.Source = source;
                }
            }
            else
            {
                testCase.DisplayName = testData.CodeElement.Name;
                var assembly = ReflectionUtils.GetAssembly(testData.CodeElement);
                testCase.Source = assembly.Path;
                testCase.SetPropertyValue(filePathProperty, assembly.Path);
            }
       
            testCase.SetPropertyValue(testIdProperty, testData.Id);

            return testCase;
        } 
    }
}