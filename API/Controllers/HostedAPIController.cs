﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NodeBlock.Engine.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HostedAPIController : ControllerBase
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [HttpPost("{graphId}/{*graphEndpoint}")]
        public async Task<IActionResult> RequestHostedGraphAPI(string graphId, string graphEndpoint)
        {
            try
            {
                var graphContext = GraphsContainer.GetRunningGraphByHash(graphId);
                if (graphContext == null)
                    return BadRequest(new { success = false, message = string.Format("Graph {0} not loaded in the GraphLinq engine", graphId) });

                if (!graphContext.graph.HasHostedAPI())
                    return BadRequest(new { success = false, message = string.Format("Graph {0} doesn't have a hosted API", graphId) });

                var graphHostedApi = graphContext.graph.GetHostedAPI().HostedAPI;
                if (!graphHostedApi.Endpoints.ContainsKey(graphEndpoint))
                    return BadRequest(new { success = false, message = string.Format("Graph {0} doesn't have this endpoint available", graphId) });

                var endpoint = graphHostedApi.Endpoints[graphEndpoint];
                var rawBody = "";
                using (StreamReader reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8))
                {
                    rawBody = await reader.ReadToEndAsync();
                }

                var context = await endpoint.OnRequest(HttpContext, rawBody);
                if (context == null) return BadRequest();

                switch (context.ResponseFormatType)
                {
                    case HostedAPI.RequestContext.ResponseFormatTypeEnum.JSON:
                        return Content(context.Body, "application/json");

                    default:
                        return Ok(context.Body);
                }
            }
            catch(Exception ex)
            {
                logger.Error(ex);
                return BadRequest("Unknown error");
            }
        }
    }
}
