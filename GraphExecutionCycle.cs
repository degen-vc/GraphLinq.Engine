﻿using Newtonsoft.Json;
using NodeBlock.Engine.Attributes;
using NodeBlock.Engine.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace NodeBlock.Engine
{
    public class GraphExecutionCycle
    {
        public BlockGraph Graph { get; }
        public long Timestamp { get; }
        public Node StartNode { get; }

        public List<Node> ExecutedNodesInCycle;
        public Debugging.GraphTrace Trace;
        public Dictionary<string, NodeParameter> StartNodeInstanciatedParameters;

        public GraphExecutionCycle(BlockGraph graph, long timestamp, Node startNode, Dictionary<string, NodeParameter> parameters = null)
        {
            Graph = graph;
            Timestamp = timestamp;
            StartNode = startNode;
            ExecutedNodesInCycle = new List<Node>();
            this.Trace = new Debugging.GraphTrace(this);
            this.AddExecutedNode(StartNode);
            this.StartNodeInstanciatedParameters = parameters != null ? parameters : this.StartNode.InstanciateParametersForCycle();
        }

        public void Execute()
        {
            this.StartNodeInstanciatedParameters.ToList().ForEach(x =>
            {
                this.StartNode.OutParameters[x.Key].Value = x.Value.Value;
            });

            this.StartNode.BeginCycle();
            BigInteger usedGas = this.GetCycleExecutedGasPrice();
            this.Graph.currentContext.AddCycleCost(decimal.Parse(usedGas.ToString()));
        }

        public BigInteger GetCycleExecutedGasPrice()
        {
            BigInteger total = new BigInteger();
            this.ExecutedNodesInCycle.ForEach(x =>
            {
                if (x.GetType().GetCustomAttributes(typeof(NodeGasConfiguration), true).Length == 0) return;
                var gasConfig = x.GetType().GetCustomAttributes(typeof(NodeGasConfiguration), true)[0] as NodeGasConfiguration;
                total += gasConfig.BlockGasPrice;
            });
            return total;
        }

        public TraceItem AddExecutedNode(Node node)
        {
            this.ExecutedNodesInCycle.Add(node);
            return this.Trace.AppendTrace(node);
        }
    }
}
