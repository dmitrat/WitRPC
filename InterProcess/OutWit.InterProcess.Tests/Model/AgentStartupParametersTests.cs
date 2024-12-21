using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.CommandLine;
using OutWit.InterProcess.Model;

namespace OutWit.InterProcess.Tests.Model
{
    [TestFixture]
    public class AgentStartupParametersTests
    {
        [Test]
        public void ConstructorTest()
        {
            var parameters = new AgentStartupParameters("1", 2, TimeSpan.FromSeconds(3), true);
            Assert.That(parameters.Address, Is.EqualTo("1"));
            Assert.That(parameters.ParentProcessId, Is.EqualTo(2));
            Assert.That(parameters.Timeout, Is.EqualTo(TimeSpan.FromSeconds(3)));
            Assert.That(parameters.ShutdownOnParentProcessExited, Is.EqualTo(true));
            Console.WriteLine(parameters);

            parameters = new AgentStartupParameters(TimeSpan.FromSeconds(3));
            Assert.That(parameters.Address, Is.Not.Empty);
            Assert.That(parameters.ParentProcessId, Is.GreaterThan(0));
            Assert.That(parameters.Timeout, Is.EqualTo(TimeSpan.FromSeconds(3)));
            Assert.That(parameters.ShutdownOnParentProcessExited, Is.EqualTo(true));
            Console.WriteLine(parameters);

        }

        [Test]
        public void IsTest()
        {
            var parameters1 = new AgentStartupParameters("1", 2, TimeSpan.FromSeconds(3), true);
            var parameters2 = new AgentStartupParameters("1", 2, TimeSpan.FromSeconds(3), true);
            Assert.That(parameters1.Is(parameters2), Is.True);

            parameters2 = new AgentStartupParameters("2", 2, TimeSpan.FromSeconds(3), true);
            Assert.That(parameters1.Is(parameters2), Is.False);

            parameters2 = new AgentStartupParameters("1", 3, TimeSpan.FromSeconds(3), true);
            Assert.That(parameters1.Is(parameters2), Is.False);

            parameters2 = new AgentStartupParameters("1", 2, TimeSpan.FromSeconds(4), true);
            Assert.That(parameters1.Is(parameters2), Is.False);

            parameters2 = new AgentStartupParameters("1", 2, TimeSpan.FromSeconds(3), false);
            Assert.That(parameters1.Is(parameters2), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var parameters1 = new AgentStartupParameters("1", 2, TimeSpan.FromSeconds(3), true);
            var parameters2 = parameters1.Clone() as AgentStartupParameters;

            Assert.That(parameters2, Is.Not.Null);
            Assert.That(parameters1, Is.Not.SameAs(parameters2));
            Assert.That(parameters1.Is(parameters2), Is.True);
        }

        [Test]
        public void SerializationTest()
        {
            var parameters1 = new AgentStartupParameters("1", 2, TimeSpan.FromSeconds(3), true);
            var parametersString = parameters1.ToString();

            var parameters2 = parametersString.DeserializeCommandLine<AgentStartupParameters>();
            Assert.That(parameters2, Is.Not.Null);
            Assert.That(parameters1, Is.Not.SameAs(parameters2));
            Assert.That(parameters1.Is(parameters2), Is.True);
            Console.WriteLine(parameters2);

            parameters1 = new AgentStartupParameters(TimeSpan.FromSeconds(123));
            parametersString = parameters1.ToString();

            parameters2 = parametersString.DeserializeCommandLine<AgentStartupParameters>();
            Assert.That(parameters2, Is.Not.Null);
            Assert.That(parameters1, Is.Not.SameAs(parameters2));
            Assert.That(parameters1.Is(parameters2), Is.True);
            Console.WriteLine(parameters2);
        }

        [Test]
        public void CommandLineParseTest()
        {
            string cmd = "someProcess.exe --address somePipe --process 12345 --timeout 00:00:03 --shutdown";
            var parameters = cmd.DeserializeCommandLine<AgentStartupParameters>();

            Assert.That(parameters.Address, Is.EqualTo("somePipe"));
            Assert.That(parameters.ParentProcessId, Is.EqualTo(12345));
            Assert.That(parameters.Timeout, Is.EqualTo(TimeSpan.FromSeconds(3)));
            Assert.That(parameters.ShutdownOnParentProcessExited, Is.EqualTo(true));
            Console.WriteLine(parameters);

            cmd = "someProcess.exe -a somePipe -p 12345 -t 00:00:03 -s";
            parameters = cmd.DeserializeCommandLine<AgentStartupParameters>();

            Assert.That(parameters.Address, Is.EqualTo("somePipe"));
            Assert.That(parameters.ParentProcessId, Is.EqualTo(12345));
            Assert.That(parameters.Timeout, Is.EqualTo(TimeSpan.FromSeconds(3)));
            Assert.That(parameters.ShutdownOnParentProcessExited, Is.EqualTo(true));
            Console.WriteLine(parameters);

            cmd = "someProcess.exe -a somePipe -t 00:00:03 -s";
            parameters = cmd.DeserializeCommandLine<AgentStartupParameters>();

            Assert.That(parameters.Address, Is.EqualTo("somePipe"));
            Assert.That(parameters.ParentProcessId, Is.EqualTo(0));
            Assert.That(parameters.Timeout, Is.EqualTo(TimeSpan.FromSeconds(3)));
            Assert.That(parameters.ShutdownOnParentProcessExited, Is.EqualTo(true));
            Console.WriteLine(parameters);

            cmd = "someProcess.exe -a somePipe -s";
            parameters = cmd.DeserializeCommandLine<AgentStartupParameters>();

            Assert.That(parameters.Address, Is.EqualTo("somePipe"));
            Assert.That(parameters.ParentProcessId, Is.EqualTo(0));
            Assert.That(parameters.Timeout, Is.EqualTo(TimeSpan.FromSeconds(0)));
            Assert.That(parameters.ShutdownOnParentProcessExited, Is.EqualTo(true));
            Console.WriteLine(parameters);


            cmd = "someProcess.exe -a somePipe";
            parameters = cmd.DeserializeCommandLine<AgentStartupParameters>();

            Assert.That(parameters.Address, Is.EqualTo("somePipe"));
            Assert.That(parameters.ParentProcessId, Is.EqualTo(0));
            Assert.That(parameters.Timeout, Is.EqualTo(TimeSpan.FromSeconds(0)));
            Assert.That(parameters.ShutdownOnParentProcessExited, Is.EqualTo(false));
            Console.WriteLine(parameters);

            cmd = "someProcess.exe -a https://mysite.com:5050/endpoint/1/";
            parameters = cmd.DeserializeCommandLine<AgentStartupParameters>();

            Assert.That(parameters.Address, Is.EqualTo("https://mysite.com:5050/endpoint/1/"));
            Assert.That(parameters.ParentProcessId, Is.EqualTo(0));
            Assert.That(parameters.Timeout, Is.EqualTo(TimeSpan.FromSeconds(0)));
            Assert.That(parameters.ShutdownOnParentProcessExited, Is.EqualTo(false));
            Console.WriteLine(parameters);


            cmd = "someProcess.exe -a https://mysite.com:5050/endpoint/1/";
            parameters = cmd.DeserializeCommandLine<AgentStartupParameters>();

            Assert.That(parameters.Address, Is.EqualTo("https://mysite.com:5050/endpoint/1/"));
            Assert.That(parameters.ParentProcessId, Is.EqualTo(0));
            Assert.That(parameters.Timeout, Is.EqualTo(TimeSpan.FromSeconds(0)));
            Assert.That(parameters.ShutdownOnParentProcessExited, Is.EqualTo(false));
            Console.WriteLine(parameters);

        }
    }
}
