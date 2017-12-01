using Microsoft.Azure.Management.Logic;
using Microsoft.Azure.Management.Logic.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LogicApp.AcceptanceTests.Utilities.LogicAppDiagnostics
{
    public class LogicAppManager
    {

        private static LogicManagementClient _client;
        private static string _runId;
        private static string _resourceGroupName;
        private static string _logicAppName;

        public static string RunId
        {
            get
            {
                return _runId;
            }
           
        }

        public enum TriggerType
        {
            Http,
            Other
        }

        /// <summary>
        /// Initializes the LogicManagementClient and assigns it to a static variable. This will be used the test framework to retrieve data using Azure Logic Apps SDK
        /// </summary>
        public static void Start()
        {
            ServiceClientCredentials credentials = GetAzureCredentials();
            _client = new LogicManagementClient(credentials);
            _client.SubscriptionId = Config.SubscriptionId;
        }

        /// <summary>
        /// When a test completes, this method should be invoked to dispose the instance of the LogicManagementClient
        /// </summary>
        public static void Stop()
        {
            _client.Dispose();
        }

     
        public static void Clear()
        {
            _runId = null;
        }

        /// <summary>
        /// Check if the logic app is enabled
        /// </summary>
        /// <param name="resourceGroupName">Pass the Resource Group Name</param>
        /// <param name="logicAppName">Pass the Logic App Name</param>
        /// <returns></returns>
        public static bool CheckIfLogicAppIsEnabled(string resourceGroupName, string logicAppName)
        {
            bool enabled = false;
            var list = _client.Workflows.ListByResourceGroupWithHttpMessagesAsync(resourceGroupName);
            var workFlows = list.Result.Body;

            workFlows.ToList().ForEach(x =>
            {
                if (x.Name == logicAppName)
                {
                    if (x.State.Value.ToString() == "Enabled")
                    {
                        enabled = true;
                    }
                }
            });

            _resourceGroupName = resourceGroupName;
            _logicAppName = logicAppName;

            return enabled;
        }

        /// <summary>
        /// Execute a logic app. This method can be used to invoke logic apps which has a callback url and logic apps which has any other kind of trigger like servicebus trigger.
        /// </summary>
        /// <param name="type">Set either HTTP or other</param>
        /// <param name="triggerName">The triggername that you specified in the logic app</param>
        /// <param name="content">Message payload</param>
        /// <returns></returns>
        public async static Task<string> ExecuteLogicApp(TriggerType type, string triggerName, HttpContent content)
        {
            if(type == TriggerType.Http)
            {
                var url = GetCallBackUrl(triggerName);
                var client = new HttpClient();
                var uri = new Uri(url.Result.Body.Value);

                var result = client.PostAsync(uri, content).Result;
                _runId = result.Headers.GetValues("x-ms-workflow-run-id").First();
                Console.WriteLine("The Run identifier is: " + _runId);

                WorkflowStatus? status;

                while (true)
                {
                    status = _client.WorkflowRuns.Get(_resourceGroupName, _logicAppName, _runId).Status;
                    if (status == WorkflowStatus.Succeeded || status == WorkflowStatus.Failed)
                    {
                        Console.WriteLine("Logic App execution for run identifier: " + _runId + " is completed");
                        break;
                    }
                    else
                        Thread.Sleep(50);
                }
                
                return result.StatusCode.ToString();
            }
            else
            {
                return await ExecuteLogicAppWithOtherTriggerType(triggerName);
            }
        }

        /// <summary>
        /// Get the callbackurl for the Logic App
        /// </summary>
        /// <param name="triggerName"></param>
        /// <returns></returns>
        private async static Task<AzureOperationResponse<WorkflowTriggerCallbackUrl>> GetCallBackUrl(string triggerName)
        {
            return await _client.WorkflowTriggers.
                ListCallbackUrlWithHttpMessagesAsync(_resourceGroupName, _logicAppName, triggerName);
        }

        /// <summary>
        /// Execute a logic app which is triggered by other compenents like SFTP, Azure service bus etc. 
        /// </summary>
        /// <param name="triggerName"></param>
        /// <returns></returns>
        private async static Task<string> ExecuteLogicAppWithOtherTriggerType(string triggerName)
        {
            var result = await _client.WorkflowTriggers.
                RunAsync(_resourceGroupName, _logicAppName, triggerName);
            return result.ToString();
        }

        /// <summary>
        /// Check the status of any action of  a completed logic app by passing the run identifier
        /// </summary>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public async static Task<string> CheckLogicAppAction(string actionName)
        {
            var r = await _client.WorkflowRunActions.ListAsync(_resourceGroupName, _logicAppName, _runId);

            return r.Where(x => x.Name == actionName)
                .Select(x => x.Status)
                .First().ToString();
        }

        /// <summary>
        /// Pass the SPN and azure subscription details and generate a OAUTH2.0 token
        /// </summary>
        /// <returns></returns>
        private static ServiceClientCredentials GetAzureCredentials()
        {
            var authContext = new AuthenticationContext(string.Format("https://login.windows.net/{0}", Config.TenantId));
            var credential = new ClientCredential(Config.WebApiApplicationId, Config.Secret);
            AuthenticationResult authResult = authContext.AcquireTokenAsync("https://management.core.windows.net/", credential).Result;
            string token = authResult.CreateAuthorizationHeader().Substring("Bearer ".Length);

            return new TokenCredentials(token);
        }
    }
}
