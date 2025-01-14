﻿using NodeBlock.Engine.Nodes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using NodeBlock.Engine.Debugging;
using Nethereum.JsonRpc.Client.Streaming;

namespace NodeBlock.Engine
{
    public interface IEventEthereumNode
    {
        BlockGraph Graph { get; set; }
        void OnEventNode(object sender, dynamic e);
    }

    public abstract class Node : ICloneable
    {
        public string Id;
        public BlockGraph Graph { get; set;  }
        public string NodeType { get; set; }
        public string FriendlyName { get; set; }
        public string NodeGroupName { get; set; }
        public string NodeBlockType { get; set; }
        public string NodeDescription { get; set; }
        public Dictionary<string, NodeParameter> InParameters { get; set; }
        public Dictionary<string, NodeParameter> OutParameters { get; set; }
        public Node OutNode { get; set; }
        public bool IsEventNode = false;
        public Node LastExecutionFrom = null;
        public bool CanBeSerialized = true;
        public int NodeCycleLimit;
        public long LastCycleAt;
        public TraceItem CurrentTraceItem = null;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public Node(string id, BlockGraph graph, string nodeType)
        {
            this.Id = id;
            this.Graph = graph;
            this.NodeType = nodeType;

            var nodeDefinition = (this.GetType().GetCustomAttributes(typeof(Attributes.NodeDefinition), true)[0] as Attributes.NodeDefinition);
            if (nodeDefinition != null)
            {
                this.FriendlyName = nodeDefinition.FriendlyName;
                this.NodeBlockType = nodeDefinition.NodeType;
                this.NodeGroupName = nodeDefinition.GroupName;
            }

            if (this.GetType().GetCustomAttributes(typeof(Attributes.NodeCycleLimit), true).Length > 0)
            {
                var nodeCycleLimit = (this.GetType().GetCustomAttributes(typeof(Attributes.NodeCycleLimit), true)[0] as Attributes.NodeCycleLimit);
                if (nodeCycleLimit != null)
                {
                    this.NodeCycleLimit = nodeCycleLimit.LimitPerCycle;
                }
            }

            if (this.GetType().GetCustomAttributes(typeof(Attributes.NodeGraphDescription), true).Length > 0)
            {
                var nodeDescription = (this.GetType().GetCustomAttributes(typeof(Attributes.NodeGraphDescription), true)[0] as Attributes.NodeGraphDescription);
                if (nodeDescription != null)
                {
                    this.NodeDescription = nodeDescription.Description;
                }
            }


            this.InParameters = new Dictionary<string, NodeParameter>();
            this.OutParameters = new Dictionary<string, NodeParameter>();
        }

        public virtual bool CanBeExecuted
        {
            get
            {
                return false;
            }
        }

        public virtual bool CanExecute
        {
            get
            {
                return false;
            }
        }

        public bool Execute(Node executedFromNode = null)
        {
            LastExecutionFrom = executedFromNode;

            // Add the node to the cycle
            var cycle = this.Graph.GetCurrentCycle();
            TraceItem traceItem = null;
            if(cycle != null)
            {
                traceItem = cycle.AddExecutedNode(this);
            }

            if (this.NodeType != typeof(EntryPointNode).Name)
            {
                if (!this.CanBeExecuted) return false;
            }

            //stop the execution if the state isnt started
            if (!(this.Graph.currentContext.currentGraphState == Enums.GraphStateEnum.STARTED))
            {
                this.Graph.Stop();
                return false;
            }

            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            this.CurrentTraceItem = traceItem;
            try
            {
                if (!this.OnExecution())
                {
                    return false;
                }
            }
            catch(Exception ex)
            {
                logger.Error(ex, "Error on node execution");
                if (this.CurrentTraceItem != null)
                {
                    this.CurrentTraceItem.ExecutionException = ex;
                    return false;
                }
            }
            this.CurrentTraceItem = null;
            var elapsedTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime;

            if(traceItem != null)
            {
                traceItem.ExecutionTime = elapsedTime;
            }

            return Next();
        }

        public bool Next()
        {
            if (this.OutNode != null)
            {
                return this.OutNode.Execute(this);
            }
            else
            {
                return true;
            }
        }

        public Dictionary<string, NodeParameter> InstanciateParametersForCycle()
        {
            var instanciatedCycleParameters = new Dictionary<string, NodeParameter>();
            this.OutParameters.ToList().ForEach(x =>
            {
                instanciatedCycleParameters.Add(x.Key, x.Value.Clone() as NodeParameter);
            });
            return instanciatedCycleParameters;
        }

        public virtual void SetupEvent()
        {
            throw new NotImplementedException();
        }

        public virtual void SetupConnector()
        {
            throw new NotImplementedException();
        }

        public virtual bool OnExecution()
        {
            throw new NotImplementedException();
        }

        public virtual object ComputeParameterValue(NodeParameter parameter, object value)
        {
            return value;
        }

        public virtual void BeginCycle()
        {
            this.Execute();
        }

        public virtual void OnStop()
        {
            return;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
