﻿using System.Activities;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Paz.LuckeyMonkey.Shared;
using Paz.LuckeyMonkey.Shared.Entities;

namespace Paz.LuckeyMonkey.Workflow
{
    [CrmPluginRegistration("RetrieveUsers", "RetrieveUsers", "", "Paz.LuckeyMonkey.Workflow", IsolationModeEnum.Sandbox)]
    public class RetrieveUsers : CodeActivity
    {
        [Output("UserIds")]
        [RequiredArgument]
        public OutArgument<string> UserIds { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            var workflowContext = executionContext.GetExtension<IWorkflowContext>();
            var serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            var tracing = executionContext.GetExtension<ITracingService>();
            ExecuteWorkflow(executionContext, workflowContext, serviceFactory, service, tracing);
        }

        private void ExecuteWorkflow(CodeActivityContext executionContext, IWorkflowContext workflowContext, IOrganizationServiceFactory serviceFactory, IOrganizationService service, ITracingService tracing)
        {
            Debugger.Trace(tracing, "Begin Execute Workflow: RetrieveUsers");
            //YOUR CUSTOM-WORKFLOW-CODE GO HERE
            var fetchData = new
            {
                isdefault = "0"
            };
            var fetchXml = $@"
<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
  <entity name='systemuser'>
    <attribute name='systemuserid' />
    <link-entity name='teammembership' from='systemuserid' to='systemuserid' visible='false' intersect='true'>
      <link-entity name='team' from='teamid' to='teamid' alias='aa'>
        <filter>
          <condition attribute='isdefault' operator='eq' value='{fetchData.isdefault}'/>
        </filter>
        <link-entity name='teammembership' from='teamid' to='teamid' visible='false' intersect='true'>
          <link-entity name='systemuser' from='systemuserid' to='systemuserid' alias='ab'>
            <filter type='and'>
              <condition attribute='systemuserid' operator='eq-userid' />
            </filter>
          </link-entity>
        </link-entity>
      </link-entity>
    </link-entity>
  </entity>
</fetch>";
            var users = service.RetrieveAll<SystemUser>(fetchXml);
            var userIds = string.Join(";",
                users
                .Where(u => u.Id != workflowContext.InitiatingUserId)
                .Select(u => u.Id).Distinct().ToList());
            UserIds.Set(executionContext, userIds);

            Debugger.Trace(tracing, "End Execute Workflow: RetrieveUsers");
        }
    }
}
