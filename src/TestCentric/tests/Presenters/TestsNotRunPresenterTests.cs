﻿// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric GUI contributors.
// Licensed under the MIT License. See LICENSE.txt in root directory.
// ***********************************************************************

using NSubstitute;
using NUnit.Framework;

namespace TestCentric.Gui.Presenters
{
    using Model;
    using Views;

    public class TestsNotRunPresenterTests : PresenterTestBase<ITestsNotRunView>
    {
        private static readonly TestNode FAKE_TEST_RUN = new TestNode("<test-suite id='1' testcasecount='1234' />");

        [SetUp]
        public void CreatePresenter()
        {
            new TestsNotRunPresenter(_view, _model);
        }

        [Test]
        public void WhenTestIsLoaded_DisplayIsCleared()
        {
            FireTestLoadedEvent(FAKE_TEST_RUN);

            _view.Received().Clear();
        }

        [Test]
        public void WhenTestIsReloaded_IfClearResultsIsFalse_DisplayIsNotCleared()
        {
            _settings.Gui.ClearResultsOnReload = false;

            FireTestReloadedEvent(FAKE_TEST_RUN);

            _view.DidNotReceive().Clear();
        }

        [Test]
        public void WhenTestIsReloaded_IfClearResultsIfTrue_DisplayIsCleared()
        {
            _settings.Gui.ClearResultsOnReload = true;

            FireTestReloadedEvent(FAKE_TEST_RUN);

            _view.Received().Clear();
        }

        [Test]
        public void WhenTestIsUnloaded_DisplayIsCleared()
        {
            FireTestUnloadedEvent();

            _view.Received().Clear();
        }

        [Test]
        public void WhenTestRunStarts_DisplayIsCleared()
        {
            FireRunStartingEvent(1234);

            _view.Received().Clear();
        }

        [TestCase("Skipped", "Test", true)]
        [TestCase("Skipped", "SetUp", true)]
        [TestCase("Skipped", "TearDown", true)]
        [TestCase("Skipped", "Parent", false)]
        [TestCase("Passed", "Test", false)]
        [TestCase("Failed", "Test", false)]
        [TestCase("Warning", "Test", false)]
        [TestCase("Inconclusive", "Test", false)]
        public void TestsCasesAreHandledCorrectly(string status, string site, bool shouldBeAdded)
        {
            FireTestFinishedEvent(new ResultNode(
                $"<test-case id='1' name='NAME' result='{status}' site='{site}'><reason><message>REASON</message></reason></test-case>"));

            if (shouldBeAdded)
                _view.Received().AddResult("NAME", "REASON");
            else
                _view.DidNotReceiveWithAnyArgs().AddResult(null, null);
        }

        [TestCase("Skipped", "Test", "REASON", true)]
        [TestCase("Skipped", "SetUp", "REASON", true)]
        [TestCase("Skipped", "TearDown", "REASON", true)]
        [TestCase("Skipped", "Child", "REASON", false)]
        [TestCase("Skipped", "Parent", "REASON", false)]
        [TestCase("Passed", "Test", "REASON", false)]
        [TestCase("Failed", "Test", "REASON", false)]
        [TestCase("Warning", "Test", "REASON", false)]
        [TestCase("Inconclusive", "Test", "REASON", false)]
        [TestCase("Skipped", "Test", "One or more child tests were ignored", false)]
        public void TestSuitesAreHandledCorrectly(string status, string site, string reason, bool shouldBeAdded)
        {
            FireSuiteFinishedEvent(new ResultNode(
                $"<test-suite id='1' name='NAME' result='{status}' site='{site}'><reason><message>{reason}</message></reason></test-suite>"));

            if (shouldBeAdded)
                _view.Received().AddResult("NAME", "REASON");
            else
                _view.DidNotReceiveWithAnyArgs().AddResult(null, null);
        }
    }
}
