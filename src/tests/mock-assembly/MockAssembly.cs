// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric GUI contributors.
// Licensed under the MIT License. See LICENSE.txt in root directory.
// ***********************************************************************

using System;
using NUnit.Framework;

// This copy of mock-assembly is referenced by TestCentric.Gui.Model.Tests
// and so is copied into  the bin directory. We mark it as a non-test assembly so
// it won't be treated as a test assembly if anyone loads the solution file.
[assembly: NonTestAssembly]

namespace TestCentric.Tests
{
    namespace Assemblies
    {
        /// <summary>
        /// Constant definitions for the mock-assembly dll.
        /// </summary>
        public class MockAssembly
        {
            public const int Tests = MockTestFixture.Tests
                        + Singletons.OneTestCase.Tests
                        + TestAssembly.MockTestFixture.Tests
                        + IgnoredFixture.Tests
                        + ExplicitFixture.Tests
                        + BadFixture.Tests
#if !NET5_0
                        + FixtureWithTestCases.Tests
#endif
                        + ParameterizedFixture.Tests
                        + GenericFixtureConstants.Tests;

            private const int Ignored = MockTestFixture.Ignored + IgnoredFixture.Tests;
            private const int Explicit = MockTestFixture.Explicit + ExplicitFixture.Tests;
            private const int NotRunnable = MockTestFixture.NotRunnable + BadFixture.Tests;
            public const int Skipped = Ignored + Explicit;
            public const int TestsRun = Tests - Skipped;

            private const int Errors = MockTestFixture.Errors;
            public const int Failures = MockTestFixture.Failures;

            public const int Failed = Errors + Failures + NotRunnable;
            public const int Passed = TestsRun - Failed - Warnings - Inconclusive;
            public const int Warnings = 0;
            public const int Inconclusive = 1;

#if !NETCOREAPP1_1
            public static string AssemblyPath;

            static MockAssembly()
            {
                var assembly = typeof(MockAssembly).Assembly;
                string codeBase = assembly.EscapedCodeBase;

                AssemblyPath = codeBase.ToLower().StartsWith(Uri.UriSchemeFile)
                    ? new Uri(codeBase).LocalPath
                    : assembly.Location;
            }
#endif
        }

        [TestFixture(Description = "Fake Test Fixture")]
        [Category("FixtureCategory")]
        public class MockTestFixture
        {
            public const int Tests = 11;

            public const int Ignored = 1;
            public const int Explicit = 1;
            public const int NotRunnable = 2;

            public const int Failures = 1;
            public const int Errors = 1;

            public const int Categories = 5;

            [Test(Description = "Mock Test #1")]
            public void MockTest1()
            { }

            [Test]
            [Category("MockCategory")]
            [Property("Severity", "Critical")]
            [Description("This is a really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really long description")]
            public void MockTest2()
            { }

            [Test]
            [Category("MockCategory")]
            [Category("AnotherCategory")]
            public void MockTest3()
            { Assert.Pass("Succeeded!"); }

            [Test]
            protected static void MockTest5()
            { }

            [Test]
            public void FailingTest()
            {
                Assert.Fail("Intentional failure");
            }

            [Test, Property("TargetMethod", "SomeClassName"), Property("Size", 5), /*Property("TargetType", typeof( System.Threading.Thread ))*/]
            public void TestWithManyProperties()
            { }

            [Test]
            [Ignore("ignoring this test method for now")]
            [Category("Foo")]
            public void MockTest4()
            { }

            [Test, Explicit]
            [Category("Special")]
            public void ExplicitlyRunTest()
            { }

            [Test]
            public void NotRunnableTest(int a, int b)
            {
            }

            [Test]
            public void InconclusiveTest()
            {
                Assert.Inconclusive("No valid data");
            }

            [Test]
            public void TestWithException()
            {
                MethodThrowsException();
            }

            private void MethodThrowsException()
            {
                throw new Exception("Intentional Exception");
            }
        }
    }

    namespace Singletons
    {
        [TestFixture]
        public class OneTestCase
        {
            public const int Tests = 1;

            [Test]
            public virtual void TestCase()
            { }
        }
    }

    namespace TestAssembly
    {
        [TestFixture]
        public class MockTestFixture
        {
            public const int Tests = 1;

            [Test]
            public void MyTest()
            {
            }
        }
    }

    [TestFixture, Ignore("Testing")]
    public class IgnoredFixture
    {
        public const int Tests = 3;
        public const int Suites = 1;

        [Test]
        public void Test1() { }

        [Test]
        public void Test2() { }

        [Test]
        public void Test3() { }
    }

    [TestFixture, Explicit]
    public class ExplicitFixture
    {
        public const int Tests = 2;

        [Test]
        public void Test1() { }

        [Test]
        public void Test2() { }
    }

    [TestFixture]
    public class BadFixture
    {
        public const int Tests = 1;

        public BadFixture(int val) { }

        [Test]
        public void SomeTest() { }
    }

#if !NET5_0 // Under the nunit 3.10 framework, these tests cause an error
    [TestFixture]
    public class FixtureWithTestCases
    {
        public const int Tests = 4;

        [TestCase(2, 2, ExpectedResult = 4)]
        [TestCase(9, 11, ExpectedResult = 20)]
        public int MethodWithParameters(int x, int y)
        {
            return x + y;
        }

        [TestCase(2, 4)]
        [TestCase(9.2, 11.7)]
        public void GenericMethod<T>(T x, T y)
        {
        }
    }
#endif

    [TestFixture(5)]
    [TestFixture(42)]
    public class ParameterizedFixture
    {
        public const int Tests = 4;

        public ParameterizedFixture(int num) { }

        [Test]
        public void Test1() { }

        [Test]
        public void Test2() { }
    }

    public class GenericFixtureConstants
    {
        public const int Tests = 4;
    }

    [TestFixture(5)]
    [TestFixture(11.5)]
    public class GenericFixture<T>
    {
        public GenericFixture(T num) { }

        [Test]
        public void Test1() { }

        [Test]
        public void Test2() { }
    }
}
