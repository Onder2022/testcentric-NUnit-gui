// ***********************************************************************
// Copyright (c) 2015 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

#if !NETCOREAPP1_1 && !NETCOREAPP2_1
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace NUnit.Engine.Services
{
    public class RuntimeFrameworkServiceTests
    {
        private RuntimeFrameworkService _runtimeService;

        [SetUp]
        public void CreateServiceContext()
        {
            var services = new ServiceContext();
            _runtimeService = new RuntimeFrameworkService();
            services.Add(_runtimeService);
            services.ServiceManager.StartServices();
        }

        [TearDown]
        public void StopService()
        {
            _runtimeService.StopService();
        }

        [Test]
        public void ServiceIsStarted()
        {
            Assert.That(_runtimeService.Status, Is.EqualTo(ServiceStatus.Started));
        }

        [TestCase("mock-assembly.dll", false)]
        [TestCase("nunit-agent.exe", false)]
        [TestCase("nunit-agent-x86.exe", true)]
        public void SelectRuntimeFramework(string assemblyName, bool runAsX86)
        {
            var assemblyPath = Path.Combine(TestContext.CurrentContext.TestDirectory, assemblyName);
            FileAssert.Exists(assemblyPath, $"File not found: {assemblyPath}");
            var package = new TestPackage(assemblyPath);

            var runtimeFramework = _runtimeService.SelectRuntimeFramework(package);

            Assert.That(package.GetSetting("RuntimeFramework", ""), Is.EqualTo(runtimeFramework));
            Assert.That(package.GetSetting("RunAsX86", false), Is.EqualTo(runAsX86));
        }

        [Test]
        public void AvailableFrameworks()
        {
            var available = _runtimeService.AvailableRuntimes;
            Assert.That(available.Count, Is.GreaterThan(0));
            foreach (var framework in available)
                Console.WriteLine("Available: {0}", framework.DisplayName);
        }

        [Test]
        public void CurrentFrameworkMustBeAvailable()
        {
            var current = RuntimeFramework.CurrentFramework;
            Console.WriteLine("Current framework is {0} ({1})", current.DisplayName, current.Id);
            Assert.That(_runtimeService.IsAvailable(current), "{0} not available", current);
        }

        [Test]
        public void AvailableFrameworksListContainsNoDuplicates()
        {
            var names = new List<string>();
            foreach (var framework in _runtimeService.AvailableRuntimes)
                names.Add(framework.DisplayName);
            Assert.That(names, Is.Unique);
        }

        [TestCase("mono", 4, 5, "net-4.5")]
        [TestCase("net", 4, 0, "net-4.5")]
        [TestCase("net", 4, 5, "net-4.5")]

        public void EngineOptionPreferredOverImageTarget(string framework, int majorVersion, int minorVersion, string requested)
        {
            var package = new TestPackage("test");
            package.AddSetting(InternalEnginePackageSettings.ImageTargetFrameworkName, framework);
            package.AddSetting(InternalEnginePackageSettings.ImageRuntimeVersion, new Version(majorVersion, minorVersion));
            package.AddSetting(EnginePackageSettings.RuntimeFramework, requested);

            _runtimeService.SelectRuntimeFramework(package);
            Assert.That(package.GetSetting<string>(EnginePackageSettings.RuntimeFramework, null), Is.EqualTo(requested));
        }

        [Test]
        public void RuntimeFrameworkIsSetForSubpackages()
        {
            var topLevelPackage = new TestPackage(new [] {"a.dll", "b.dll"});

            var net20Package = topLevelPackage.SubPackages[0];
            net20Package.Settings.Add(InternalEnginePackageSettings.ImageRuntimeVersion, new Version("2.0.50727"));
            var net40Package = topLevelPackage.SubPackages[1];
            net40Package.Settings.Add(InternalEnginePackageSettings.ImageRuntimeVersion, new Version("4.0.30319"));

            var platform = Environment.OSVersion.Platform;

            _runtimeService.SelectRuntimeFramework(topLevelPackage);

            Assert.Multiple(() =>
            {
                // HACK: this test will pass on a windows system with .NET 2.0 and .NET 4.0 installed or on a 
                // linux system with a newer version of Mono with no 2.0 profile.
                // TODO: Test should not depend on the availability of specific runtimes
                if (platform == PlatformID.Win32NT)
                {
                    Assert.That(net20Package.Settings[EnginePackageSettings.RuntimeFramework], Is.EqualTo("net-2.0"));
                    Assert.That(net40Package.Settings[EnginePackageSettings.RuntimeFramework], Is.EqualTo("net-4.0"));
                    Assert.That(topLevelPackage.Settings[EnginePackageSettings.RuntimeFramework], Is.EqualTo("net-4.0"));
                }
                else
                {
                    Assert.That(net20Package.Settings[EnginePackageSettings.RuntimeFramework], Is.EqualTo("mono-4.0"));
                    Assert.That(net40Package.Settings[EnginePackageSettings.RuntimeFramework], Is.EqualTo("mono-4.0"));
                    Assert.That(topLevelPackage.Settings[EnginePackageSettings.RuntimeFramework], Is.EqualTo("mono-4.0"));
                }
            });
        }
    }
}
#endif