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


        public static void Start()
        {
            ServiceClientCredentials credentials = GetAzureCredentials();
            _client = new LogicManagementClient(credentials);
            _client.SubscriptionId = Config.SubscriptionId;
        }

        public static void Stop()
        {
            _client.Dispose();
        }

        public static void Clear()
        {
            _runId = null;
        }


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

        private async static Task<AzureOperationResponse<WorkflowTriggerCallbackUrl>> GetCallBackUrl(string triggerName)
        {
            return await _client.WorkflowTriggers.
                ListCallbackUrlWithHttpMessagesAsync(_resourceGroupName, _logicAppName, triggerName);
        }

        private async static Task<string> ExecuteLogicAppWithOtherTriggerType(string triggerName)
        {
            var result = await _client.WorkflowTriggers.
                RunAsync(_resourceGroupName, _logicAppName, triggerName);
            return result.ToString();
        }

        public async static Task<string> CheckLogicAppAction(string actionName)
        {
            var r = await _client.WorkflowRunActions.ListAsync(_resourceGroupName, _logicAppName, _runId);

            return r.Where(x => x.Name == actionName)
                .Select(x => x.Status)
                .First().ToString();
        }

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
