﻿using System;
using System.Collections.Generic;
using System.Linq;
using PipelineProcessor2.JsonTypes;
using PipelineProcessor2.Nodes.Internal;
using PipelineProcessor2.Pipeline.Exceptions;

namespace PipelineProcessor2.Pipeline
{
    /// <summary>
    /// Structure for holding all special nodes found in the dependency graph
    /// </summary>
    public class LoopPair
    {
        public LoopStart Start;
        public LoopEnd End;
        public int Iteration, Id, Depth;
        public List<NodeSlot> ContainedNodes;
    }

    public struct SpecialNodeData
    {
        public List<LoopPair> Loops;
        public SyncData SyncInformation;
    }

    /// <summary>
    /// structure which indicates the connection between two nodes
    /// </summary>
    public struct NodeSlot
    {
        public int NodeId;
        public int SlotPos;

        public NodeSlot(int nodeId, int slotPos)
        {
            NodeId = nodeId;
            SlotPos = slotPos;
        }

        public static NodeSlot Invalid => new NodeSlot(-1, -1);

        public static bool isInvalid(NodeSlot check)
        {
            return check.NodeId < 0 || check.SlotPos < -1;
        }
    }

    /// <summary>
    /// container for storing node dependencies, used to represent the pipeline in graph form
    /// </summary>
    public class DependentNode
    {
        /// <summary>
        /// Nodes which require the output of this node to start
        /// </summary>
        public NodeSlot[] Dependents
        {
            get
            {
                NodeSlot[] deps = new NodeSlot[totalDependents];

                int i = 0;
                foreach (KeyValuePair<int, List<NodeSlot>> slot in dependents)
                    foreach (NodeSlot nodeSlot in slot.Value)
                    {
                        deps[i] = nodeSlot;
                        i++;
                    }

                return deps;
            }
        }

        /// <summary>
        /// Nodes which this node requires to start
        /// </summary>
        public NodeSlot[] Dependencies
        {
            get
            {
                if(slots == null)
                {
                    List<int> keys = new List<int>(dependencies.Keys);
                    keys.Sort();

                    slots = new NodeSlot[keys.Count];
                    for(int i = 0; i < keys.Count; i++)
                        slots[i] = dependencies[keys[i]];
                }

                return slots;
            }
        }

        public int Id { get; private set; }
        public string Type { get; private set; }
        public string Value { get; private set; }

        private NodeSlot[] slots = null;
        private Dictionary<int, NodeSlot> dependencies;
        private Dictionary<int, List<NodeSlot>> dependents;
        private int totalDependents = 0;

        public DependentNode(int id, string type)
        {
            Id = id;
            Type = type;
            Value = "";

            dependents = new Dictionary<int, List<NodeSlot>>();
            dependencies = new Dictionary<int, NodeSlot>();
        }

        public DependentNode(GraphNode node)
        {
            Id = node.id;
            Type = node.type;
            Value = node.value;

            dependents = new Dictionary<int, List<NodeSlot>>();
            dependencies = new Dictionary<int, NodeSlot>();
        }

        public void AddDependency(int originId, int originSlot, int targetSlot)
        {
            if (dependencies.ContainsKey(targetSlot))
                throw new DataSlotAlreadyInUseException("Slot " + targetSlot + " of node " + Id + " has already been assigned");

            NodeSlot nodeSlot = new NodeSlot(originId, originSlot);
            dependencies.Add(targetSlot, nodeSlot);
        }

        public void AddDependent(int targetId, int targetSlot, int originSlot)
        {
            NodeSlot nodeSlot = new NodeSlot(targetId, targetSlot);

            if (!dependents.ContainsKey(originSlot)) dependents.Add(originSlot, new List<NodeSlot>());
            if (dependents[originSlot].Contains(nodeSlot))
            {
                //clean up unused slot
                if (dependents[originSlot].Count == 0) dependents.Remove(originSlot);

                throw new DataSlotAlreadyInUseException("Slot " + targetSlot + " of node " + Id + " has already been assigned");
            }

            dependents[originSlot].Add(nodeSlot);
            totalDependents++;
        }
    }


    /// <summary>
    /// collected sync data
    /// </summary>
    public struct SyncData
    {
        public SyncNode[] SyncNodes;
        public List<SyncSplitGroup> NodeGroups;
    }

    /// <summary>
    /// Data for a single sync block node ground, used for determining how to split up pipelines for reduced duplication
    /// </summary>
    public class SyncSplitGroup
    {
        public int SyncNodeId;
        public List<int> ControllingNodes = new List<int>();
        public int[] Dependents;

        //pipeline build
        public bool Input = false;
        public int RequiredPipes = 0, CalledBy = -2;
        public PipelineExecutor[] pipes;
        public SyncSplitGroup linked;
    }
}
