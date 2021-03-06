﻿using System.Collections.Generic;
using NUnit.Framework;
using PipelineProcessor2.JsonTypes;
using PipelineProcessor2.Nodes.Internal;
using PipelineProcessor2.Pipeline;
using PipelineProcessor2.Pipeline.Detectors;
using PipelineProcessor2.Pipeline.Exceptions;
using PipelineProcessor2.Plugin;
using PipelineTests.TestNodes;

namespace PipelineTests.Pipeline.Detectors
{
    [TestFixture]
    public class SyncBlockBuildTest
    {
#if DEBUG
        private const int DataSize = 5;

        private static BuildInputPlugin inputPlugin = new BuildInputPlugin(DataSize, "TestInput1"),
            inputPlugin2 = new BuildInputPlugin(DataSize * 2, "TestInput2");
        private static PrematureEndPlugin
            EarlyEnd = new PrematureEndPlugin(DataSize, DataSize / 2, "LowShow"),
            LateEnd = new PrematureEndPlugin(DataSize, DataSize * 3, "HighShow"),
            EarlyData = new PrematureEndPlugin(DataSize / 2, DataSize, "LowData"),
            LateData = new PrematureEndPlugin(DataSize * 23, DataSize, "HighData");
        private static ErrorInputPlugin ErrorPlugin = new ErrorInputPlugin(DataSize, "ErrorPlugin");

        [OneTimeSetUp]
        public void Setup()
        {
            PluginStore.Init();
            PluginStore.AddPlugin(inputPlugin);
            PluginStore.AddPlugin(inputPlugin2);
            PluginStore.AddPlugin(EarlyEnd);
            PluginStore.AddPlugin(LateEnd);
            PluginStore.AddPlugin(LateData);
            PluginStore.AddPlugin(EarlyData);
            PluginStore.AddPlugin(ErrorPlugin);
        }

        [TearDown]
        public void CleanUp()
        {
            PipelineState.ClearAll();
        }

        [Test]
        public void ManyToSingle()
        {
            // Multi            | Single
            // S -> Process -> Sync -> Process -> End

            List<GraphNode> nodes = new List<GraphNode>();
            nodes.Add(TestHelpers.BuildGraphNode(0, "input", inputPlugin.TypeName));
            nodes.Add(TestHelpers.BuildGraphNode(1, "process"));
            nodes.Add(TestHelpers.BuildGraphNode(2, "sync", SyncNode.TypeName));
            nodes.Add(TestHelpers.BuildGraphNode(3, "process"));
            nodes.Add(TestHelpers.BuildGraphNode(4, "output"));

            List<NodeLinkInfo> links = new List<NodeLinkInfo>();
            links.Add(TestHelpers.MatchSlots(nodes[0], nodes[1], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[1], nodes[2], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[2], nodes[3], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[3], nodes[4], 0, 0));

            PipelineState.UpdateActiveGraph(nodes.ToArray(), links.ToArray());

            PipelineExecutor[] results = PipelineState.BuildPipesTestOnly();
            Assert.AreEqual(DataSize + 1, results.Length);

            SpecialNodeData nodeData = PipelineState.SpecialNodeDataTestOnly;
            Assert.AreEqual(1, nodeData.SyncInformation.SyncNodes.Length);
            Assert.AreNotSame(inputPlugin2.InputDataQuantity(""),
                nodeData.SyncInformation.SyncNodes[0].TriggeredPipelines.Length);
        }

        [Test]
        public void ManyToMore()
        {
            // Multi            | Many
            // S -> Process -> Sync -> Process -> End
            //                  Input ----ᴧ

            List<GraphNode> nodes = new List<GraphNode>();
            nodes.Add(TestHelpers.BuildGraphNode(0, "input", inputPlugin.TypeName));
            nodes.Add(TestHelpers.BuildGraphNode(1, "process"));
            nodes.Add(TestHelpers.BuildGraphNode(2, "sync", SyncNode.TypeName));
            nodes.Add(TestHelpers.BuildGraphNode(3, "input", inputPlugin2.TypeName));
            nodes.Add(TestHelpers.BuildGraphNode(4, "process"));
            nodes.Add(TestHelpers.BuildGraphNode(5, "output"));

            List<NodeLinkInfo> links = new List<NodeLinkInfo>();
            links.Add(TestHelpers.MatchSlots(nodes[0], nodes[1], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[1], nodes[2], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[2], nodes[4], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[3], nodes[4], 0, 1));
            links.Add(TestHelpers.MatchSlots(nodes[4], nodes[5], 0, 0));

            PipelineState.UpdateActiveGraph(nodes.ToArray(), links.ToArray());

            PipelineExecutor[] results = PipelineState.BuildPipesTestOnly();
            Assert.AreEqual(DataSize + (DataSize * 2), results.Length);

            SpecialNodeData nodeData = PipelineState.SpecialNodeDataTestOnly;
            Assert.AreEqual(1, nodeData.SyncInformation.SyncNodes.Length);
            Assert.AreEqual(inputPlugin2.InputDataQuantity(""),
                nodeData.SyncInformation.SyncNodes[0].TriggeredPipelines.Length);
        }

        [Test]
        public void ManyToMany()
        {
            // Multi            | Multi
            // S -> Process -> Sync -> Process -> End
            //         v-----------------ᴧ

            List<GraphNode> nodes = new List<GraphNode>();
            nodes.Add(TestHelpers.BuildGraphNode(0, "input", inputPlugin.TypeName));
            nodes.Add(TestHelpers.BuildGraphNode(1, "process"));
            nodes.Add(TestHelpers.BuildGraphNode(2, "sync", SyncNode.TypeName));
            nodes.Add(TestHelpers.BuildGraphNode(3, "process"));
            nodes.Add(TestHelpers.BuildGraphNode(4, "output"));

            List<NodeLinkInfo> links = new List<NodeLinkInfo>();
            links.Add(TestHelpers.MatchSlots(nodes[0], nodes[1], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[1], nodes[2], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[1], nodes[3], 0, 1));
            links.Add(TestHelpers.MatchSlots(nodes[2], nodes[3], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[3], nodes[4], 0, 0));

            PipelineState.UpdateActiveGraph(nodes.ToArray(), links.ToArray());

            PipelineExecutor[] results = PipelineState.BuildPipesTestOnly();
            Assert.AreEqual(DataSize, results.Length);

            SpecialNodeData nodeData = PipelineState.SpecialNodeDataTestOnly;
            Assert.AreEqual(1, nodeData.SyncInformation.SyncNodes.Length);
            Assert.AreSame(nodeData.SyncInformation.NodeGroups[0].pipes,
                nodeData.SyncInformation.SyncNodes[0].TriggeredPipelines);
        }

        [Test]
        public void ManyToManyWithInput()
        {
            // Multi            | Multi
            //         ᴧ------------------v
            // S -> Process -> Sync -> Process -> End
            //                  Input ----ᴧ

            List<GraphNode> nodes = new List<GraphNode>();
            nodes.Add(TestHelpers.BuildGraphNode(0, "input", inputPlugin.TypeName));
            nodes.Add(TestHelpers.BuildGraphNode(1, "process"));
            nodes.Add(TestHelpers.BuildGraphNode(2, "sync", SyncNode.TypeName));
            nodes.Add(TestHelpers.BuildGraphNode(3, "input", inputPlugin2.TypeName));
            nodes.Add(TestHelpers.BuildGraphNode(4, "process"));
            nodes.Add(TestHelpers.BuildGraphNode(5, "output"));

            List<NodeLinkInfo> links = new List<NodeLinkInfo>();
            links.Add(TestHelpers.MatchSlots(nodes[0], nodes[1], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[1], nodes[2], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[1], nodes[4], 0, 2));
            links.Add(TestHelpers.MatchSlots(nodes[2], nodes[4], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[3], nodes[4], 0, 1));
            links.Add(TestHelpers.MatchSlots(nodes[4], nodes[5], 0, 0));

            PipelineState.UpdateActiveGraph(nodes.ToArray(), links.ToArray());
            Assert.Throws<PipelineException>(() => PipelineState.BuildPipesTestOnly());

            SpecialNodeData nodeData = PipelineState.SpecialNodeDataTestOnly;
            Assert.AreEqual(1, nodeData.SyncInformation.SyncNodes.Length);
        }

        [Test]
        public void ManyToSingleToMany()
        {
            // Many             | Single           | Many
            // S -> Process -> Sync -> Process -> Sync -> Process -> End
            //        v-------------------------------------ᴧ

            List<GraphNode> nodes = new List<GraphNode>();
            nodes.Add(TestHelpers.BuildGraphNode(0, "input", inputPlugin.TypeName));
            nodes.Add(TestHelpers.BuildGraphNode(1, "process"));
            nodes.Add(TestHelpers.BuildGraphNode(2, "sync", SyncNode.TypeName));
            nodes.Add(TestHelpers.BuildGraphNode(3, "process"));
            nodes.Add(TestHelpers.BuildGraphNode(4, "sync", SyncNode.TypeName));
            nodes.Add(TestHelpers.BuildGraphNode(5, "process"));
            nodes.Add(TestHelpers.BuildGraphNode(6, "output"));

            List<NodeLinkInfo> links = new List<NodeLinkInfo>();
            links.Add(TestHelpers.MatchSlots(nodes[0], nodes[1], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[1], nodes[2], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[1], nodes[5], 1, 1));
            links.Add(TestHelpers.MatchSlots(nodes[2], nodes[3], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[3], nodes[4], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[4], nodes[5], 0, 0));
            links.Add(TestHelpers.MatchSlots(nodes[5], nodes[6], 0, 0));

            PipelineState.UpdateActiveGraph(nodes.ToArray(), links.ToArray());

            PipelineExecutor[] results = PipelineState.BuildPipesTestOnly();
            Assert.AreEqual(DataSize + 1, results.Length);

            SpecialNodeData nodeData = PipelineState.SpecialNodeDataTestOnly;

            Assert.AreEqual(2, nodeData.SyncInformation.SyncNodes.Length);
            Assert.AreEqual(3, nodeData.SyncInformation.NodeGroups.Count);

            Assert.AreSame(nodeData.SyncInformation.NodeGroups[0].pipes,
                nodeData.SyncInformation.SyncNodes[1].TriggeredPipelines);
        }

#endif

        [Test]
        public void SyncGeneratorInputException()
        {
            // S -> Sync -> End
            //Gen-> 

            List<DependentNode> nodes = new List<DependentNode>();
            DependentNode start = new DependentNode(0, "start"),
                gen = new DependentNode(1, "gen"),
                sync2 = new DependentNode(6, SyncNode.TypeName);

            nodes.Add(start);
            nodes.Add(gen);
            nodes.Add(sync2);

            TestHelpers.MatchSlots(start, sync2, 0, 0);
            TestHelpers.MatchSlots(gen, sync2, 0, 1);

            DataStore staticData = new DataStore(true);
            Assert.Throws<InvalidConnectionException>(() => SpecialNodeSearch.CheckForSpecialNodes(TestHelpers.ConvertToDictionary(nodes), staticData));
        }
    }
}
