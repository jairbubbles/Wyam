﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wyam.Core;
using Wyam.Core.Modules;
using Delegate = Wyam.Core.Modules.Delegate;


namespace Wyam.Core.Tests
{
    [TestFixture]
    public class EngineFixture
    {
        [Test]
        public void ConfigureSetsPrimitiveMetadata()
        {
            // Given
            Engine engine = new Engine();
            string configScript = @"
                Metadata[""TestString""] = ""teststring"";
                Metadata[""TestInt""] = 1234;
                Metadata[""TestFloat""] = 1234.567;
                Metadata[""TestBool""] = true;
            ";

            // When
            engine.Configure(configScript);

            // Then
            Assert.AreEqual("teststring", engine.Metadata["TestString"]);
            Assert.AreEqual(1234, engine.Metadata["TestInt"]);
            Assert.AreEqual(1234.567, engine.Metadata["TestFloat"]);
            Assert.AreEqual(true, engine.Metadata["TestBool"]);
        }

        [Test]
        public void ConfigureSetsAnonymousObjectMetadata()
        {
            // Given
            Engine engine = new Engine();
            string configScript = @"
                Metadata[""TestAnonymous""] = new { A = 1, B = ""b"" };
            ";

            // When
            engine.Configure(configScript);

            // Then
            Assert.AreEqual(1, ((dynamic)engine.Metadata["TestAnonymous"]).A);
            Assert.AreEqual("b", ((dynamic)engine.Metadata["TestAnonymous"]).B);
        }

        [Test]
        public void ConfigureAddsPipelineAndModules()
        {
            // Given
            Engine engine = new Engine();
            string configScript = @"
                Pipelines.Create(
                    new ReadFiles(""*.cshtml""),
	                new WriteFiles("".html""));
            ";

            // When
            engine.Configure(configScript);

            // Then
            Assert.AreEqual(1, ((PipelineCollection)engine.Pipelines).Pipelines.Count());
            Assert.AreEqual(2, ((PipelineCollection)engine.Pipelines).Pipelines.First().Count);
        }

        [Test]
        public void ConfigureSupportsGlobalConstructorMethods()
        {
            // Given
            Engine engine = new Engine();
            string configScript = @"
                Pipelines.Create(
                    ReadFiles(""*.cshtml""),
	                WriteFiles("".html""));
            ";

            // When
            engine.Configure(configScript);

            // Then
            Assert.AreEqual(1, ((PipelineCollection)engine.Pipelines).Pipelines.Count());
            Assert.AreEqual(2, ((PipelineCollection)engine.Pipelines).Pipelines.First().Count);
        }

        [Test]
        public void ExecuteResultsInCorrectCounts()
        {
            // Given
            Engine engine = new Engine();
            CountModule a = new CountModule("A")
            {
                AdditionalOutputs = 1
            };
            CountModule b = new CountModule("B")
            {
                AdditionalOutputs = 2
            };
            CountModule c = new CountModule("C")
            {
                AdditionalOutputs = 3
            };
            engine.Pipelines.Create(a, b, c);

            // When
            engine.Execute();

            // Then
            Assert.AreEqual(1, a.ExecuteCount);
            Assert.AreEqual(1, b.ExecuteCount);
            Assert.AreEqual(1, c.ExecuteCount);
            Assert.AreEqual(1, a.InputCount);
            Assert.AreEqual(2, b.InputCount);
            Assert.AreEqual(6, c.InputCount);
            Assert.AreEqual(2, a.OutputCount);
            Assert.AreEqual(6, b.OutputCount);
            Assert.AreEqual(24, c.OutputCount);
        }

        [Test]
        public void CompletedMetadataIsPopulatedAfterRun()
        {
            // Given
            Engine engine = new Engine();
            int c = 0;
            engine.Pipelines.Create(
                new Delegate(x => new[]
                {
                    x.Clone(null, new Dictionary<string, object> { { c.ToString(), c++ } }), 
                    x.Clone(null, new Dictionary<string, object> { { c.ToString(), c++ } })
                }),
                new Delegate(x => new[]
                {
                    x.Clone(null, new Dictionary<string, object> { { c.ToString(), c++ } })
                }));

            // When
            engine.Execute();

            // Then
            Assert.AreEqual(2, engine.CompletedContexts.Count);

            Assert.IsTrue(engine.CompletedContexts[0].Metadata.ContainsKey("0"));
            Assert.AreEqual(0, engine.CompletedContexts[0].Metadata["0"]);
            Assert.IsTrue(engine.CompletedContexts[0].Metadata.ContainsKey("2"));
            Assert.AreEqual(2, engine.CompletedContexts[0].Metadata["2"]);
            Assert.IsFalse(engine.CompletedContexts[0].Metadata.ContainsKey("1"));
            Assert.IsFalse(engine.CompletedContexts[0].Metadata.ContainsKey("3"));

            Assert.IsTrue(engine.CompletedContexts[1].Metadata.ContainsKey("1"));
            Assert.AreEqual(1, engine.CompletedContexts[1].Metadata["1"]);
            Assert.IsTrue(engine.CompletedContexts[1].Metadata.ContainsKey("3"));
            Assert.AreEqual(3, engine.CompletedContexts[1].Metadata["3"]);
            Assert.IsFalse(engine.CompletedContexts[1].Metadata.ContainsKey("0"));
            Assert.IsFalse(engine.CompletedContexts[1].Metadata.ContainsKey("2"));
        }

        [Test]
        public void CompletedContentIsPopulatedAfterRun()
        {
            // Given
            Engine engine = new Engine();
            int c = 0;
            engine.Pipelines.Create(
                new Delegate(x => new[]
                {
                    x.Clone((c++).ToString()), 
                    x.Clone((c++).ToString())
                }),
                new Delegate(x => new[]
                {
                    x.Clone((c++).ToString())
                }));

            // When
            engine.Execute();

            // Then
            Assert.AreEqual(2, engine.CompletedContexts.Count);
            Assert.AreEqual("2", engine.CompletedContexts[0].Content);
            Assert.AreEqual("3", engine.CompletedContexts[1].Content);
        }
    }
}