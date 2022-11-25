// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric GUI contributors.
// Licensed under the MIT License. See LICENSE.txt in root directory.
// ***********************************************************************

// This file contains classes used to interpret the result XML that is
// produced by test runs of the GUI.

public abstract class ResultSummary
{
	public string OverallResult { get; set; }
	public int Total  { get; set; }
	public int Passed  { get; set; }
	public int Failed  { get; set; }
	public int Warnings  { get; set; }
	public int Inconclusive  { get; set; }
	public int Skipped { get; set; }
}

public class ExpectedResult : ResultSummary
{
	public ExpectedResult(string overallResult)
	{
		OverallResult = overallResult ?? throw new ArgumentNullException(nameof(overallResult));

		// Initialize counters to -1, indicating no expected value.
		// Set properties of those items to be checked.
		Total = Passed = Failed = Warnings = Inconclusive = Skipped = -1;
	}

    private int _errorCount;

	public int CheckResult(ResultSummary actual)
	{
        _errorCount = 0;

        if (OverallResult != actual.OverallResult)
            ReportError($"  Expected: Overall Result = {OverallResult}\r\n   But was: {actual.OverallResult}");
        CheckCounter("Test Count", Total, actual.Total);
        CheckCounter("Passed", Passed, actual.Passed);
        CheckCounter("Failed", Failed, actual.Failed);
        CheckCounter("Warnings", Warnings, actual.Warnings);
        CheckCounter("Inconclusive", Inconclusive, actual.Inconclusive);
        CheckCounter("Skipped", Skipped, actual.Skipped);

        if (_errorCount == 0)
            Console.WriteLine("SUCCESS: Test Result matches expected result!");

		return _errorCount;
	}

    private void CheckCounter(string label, int expected, int actual)
    {
        if (expected > 0 && expected != actual)
            ReportError($"  Expected: {label} = {expected}\r\n   But was: {actual}");
    }

    private void ReportError(string message)
    {
        if (_errorCount++ == 0)
            Console.WriteLine("ERROR: Test Result not as expected!\n");
        Console.WriteLine(message);
    }
}

public class ActualResult : ResultSummary
{
	public ActualResult(string resultFile)
	{
		var doc = new XmlDocument();
		doc.Load(resultFile);

		Xml = doc.DocumentElement;
		if (Xml.Name != "test-run")
			throw new Exception("The tet-run element was not found");

		OverallResult = GetAttribute(Xml, "result");
		Total = IntAttribute(Xml, "total");
		Passed = IntAttribute(Xml, "passed");
		Failed = IntAttribute(Xml, "failed");
		Warnings = IntAttribute(Xml, "warnings");
		Inconclusive = IntAttribute(Xml, "inconclusive");
		Skipped = IntAttribute(Xml, "skipped");
	}

	public XmlNode Xml { get; }

	private string GetAttribute(XmlNode testRun, string name)
	{
		return testRun.Attributes[name]?.Value;
	}

	private int IntAttribute(XmlNode node, string name)
	{
		string s = GetAttribute(node, name);
		return s == null ? 0 : int.Parse(s);
	}
}

public class TestReport
{
    public PackageTest Test;
    public ActualResult Result;
    public List<string> Errors;

    public TestReport(PackageTest test, ActualResult result)
    {
        Test = test;
        Result = result;
        Errors = new List<string>();

        var expected = test.ExpectedResult;

        ReportMissingFiles();

        if (result.OverallResult == null)
            Errors.Add("     The test-run element has no result attribute.");
        else if (expected.OverallResult != result.OverallResult)
            Errors.Add($"     Expected: Overall Result = {expected.OverallResult}\r\n    But was: {result.OverallResult}");
        CheckCounter("Test Count", expected.Total, result.Total);
        CheckCounter("Passed", expected.Passed, result.Passed);
        CheckCounter("Failed", expected.Failed, result.Failed);
        CheckCounter("Warnings", expected.Warnings, result.Warnings);
        CheckCounter("Inconclusive", expected.Inconclusive, result.Inconclusive);
        CheckCounter("Skipped", expected.Skipped, result.Skipped);
    }

    public TestReport(PackageTest test, Exception ex)
    {
        Test = test;
        Result = null;
        Errors = new List<string>();
        Errors.Add($"     {ex.Message}");
    }

    public void Display(int index)
    {
        Console.WriteLine($"\n{index}. {Test.Description}");
        Console.WriteLine($"   Args: {Test.Arguments}\n");

        foreach (var error in Errors)
            Console.WriteLine(error);

        Console.WriteLine(Errors.Count == 0
            ? "   SUCCESS: Test Result matches expected result!"
            : "\n   ERROR: Test Result not as expected!");
    }

    // File level errors, like missing or mal-formatted files, need to be highlighted
    // because otherwise it's hard to detect the cause of the problem without debugging.
    // This method finds and reports that type of error.
    private void ReportMissingFiles()
    {
        // Start with all the top-level test suites. Note that files that
        // cannot be found show up as Unknown as do unsupported file types.
        var suites = Result.Xml.SelectNodes(
            "//test-suite[@type='Unknown'] | //test-suite[@type='Project'] | //test-suite[@type='Assembly']");

        // If there is no top-level suite, it generally means the file format could not be interpreted
        if (suites.Count == 0)
            Errors.Add("     No top-level suites! Possible empty command-line or misformed project.");

        foreach (XmlNode suite in suites)
        {
            // Narrow down to the specific failures we want
            string runState = GetAttribute(suite, "runstate");
            string suiteResult = GetAttribute(suite, "result");
            string label = GetAttribute(suite, "label");
            string site = suite.Attributes["site"]?.Value ?? "Test";
            if (runState == "NotRunnable" || suiteResult == "Failed" && site == "Test" && (label == "Invalid" || label=="Error"))
            {
                string message = suite.SelectSingleNode("reason/message")?.InnerText;
                Errors.Add($"     {message}");
            }
        }
    }

    private void CheckCounter(string label, int expected, int actual)
    {
        // If expected value of counter is negative, it means no check is needed
        if (expected >=0 && expected != actual)
            Errors.Add($"     Expected: {label} = {expected}\r\n      But was: {actual}");
    }

    private string GetAttribute(XmlNode node, string name)
    {
        return node.Attributes[name]?.Value;
    }
}

public class ResultReporter
{
    private string _packageName;
    private List<TestReport> _reports = new List<TestReport>();

    public ResultReporter(string packageName)
    {
        _packageName = packageName;
    }

    public void AddReport(TestReport report)
    {
        _reports.Add(report);
    }

    public bool ReportResults()
    {
        DisplayBanner($"Test Results for {_packageName}");

        Console.WriteLine("\nTest Environment");
        Console.WriteLine($"   OS Version: {Environment.OSVersion.VersionString}");
        Console.WriteLine($"  CLR Version: {Environment.Version}\n");

        int index = 0;
        bool hasErrors = false;

        foreach (var report in _reports)
        {
            hasErrors |= report.Errors.Count > 0;
            report.Display(++index);
        }

        return hasErrors;
    }
}
