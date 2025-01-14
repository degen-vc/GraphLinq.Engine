﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace NodeBlock.Engine.Interop
{
    public class NodeSchema
    {
        [JsonIgnore]
        private Node node;

        [JsonProperty(PropertyName = "id")]
        public string Id;

        [JsonProperty(PropertyName = "type")]
        public string Type;

        [JsonProperty(PropertyName = "out_node")]
        public string OutNode;

        [JsonProperty(PropertyName = "can_be_executed")]
        public bool CanBeExecuted;

        [JsonProperty(PropertyName = "can_execute")]
        public bool CanExecute;

        [JsonProperty(PropertyName = "in_parameters")]
        public List<NodeParameterSchema> InParameters;

        [JsonProperty(PropertyName = "out_parameters")]
        public List<NodeParameterSchema> OutParameters;

        public NodeSchema() { }

        public NodeSchema(Node node)
        {
            this.node = node;
            this.Id = node.Id;
            this.Type = node.NodeType.ToString();
            this.CanBeExecuted = node.CanBeExecuted;
            this.CanExecute = node.CanExecute;
            this.InParameters = new List<NodeParameterSchema>();
            this.OutParameters = new List<NodeParameterSchema>();
            if (node.OutNode != null) this.OutNode = node.OutNode.Id;

            foreach (var parameters in this.node.InParameters)
            {
                this.InParameters.Add(new NodeParameterSchema(node, parameters.Value));
            }

            foreach (var parameters in this.node.OutParameters)
            {
                this.OutParameters.Add(new NodeParameterSchema(node, parameters.Value));
            }
        }
    }
}
