﻿using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using NT.BTT.Shared;

namespace NT.BTT.WorkflowAccount
{
    [CrmPluginRegistration("CheckData", "CheckData", "", "NT.BTT.WorkflowAccount", IsolationModeEnum.Sandbox)]
    public class CheckData : CodeActivity
    {
        //https://docs.microsoft.com/en-us/dynamics365/customer-engagement/developer/workflow/add-metadata-custom-workflow-activity

        //[Default("Default Value")]
        //[Input("Input Value")]
        //[ReferenceTarget("account")]
        //public InArgument<EntityReference> InputValue { get; set; }

        //[Default("Default OutputValue"), Output("OutputValue")]
        //public OutArgument<string> OutputValue { get; set; }

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
            Debugger.Trace(tracing, "Begin Execute Workflow: CheckData");
            //YOUR CUSTOM-WORKFLOW-CODE GO HERE

            Debugger.Trace(tracing, "End Execute Workflow: CheckData");
        }
    }
}