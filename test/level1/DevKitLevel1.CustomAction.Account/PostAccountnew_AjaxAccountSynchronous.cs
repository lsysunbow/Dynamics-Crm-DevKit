﻿using System;
using Microsoft.Xrm.Sdk;
using DevKitLevel1.Shared;

namespace DevKitLevel1.CustomActionAccount
{
    [CrmPluginRegistration("new_AjaxAccount", "account", StageEnum.PostOperation, ExecutionModeEnum.Synchronous, "",
    "DevKitLevel1.CustomActionAccount.Account.PostAccountnew_AjaxAccountSynchronous", 1, IsolationModeEnum.Sandbox)]
    public class PostAccountnew_AjaxAccountSynchronous : IPlugin
    {
        /*
          InputParameters:
              Target    Microsoft.Xrm.Sdk.EntityReference - require
           OutputParameters:
        */
        private readonly string _unsecureString = null;
        private readonly string _secureString = null;

        public PostAccountnew_AjaxAccountSynchronous(string unsecureString, string secureString)
        {
            if (!string.IsNullOrWhiteSpace(unsecureString)) _unsecureString = unsecureString;
            if (!string.IsNullOrWhiteSpace(secureString)) _secureString = secureString;
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            if (context == null) throw new InvalidPluginExecutionException("Initialize IPluginExecutionContext fail.");
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            if (serviceFactory == null) throw new InvalidPluginExecutionException("Initialize IOrganizationServiceFactory fail.");
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            if (service == null) throw new InvalidPluginExecutionException("Initialize IOrganizationService fail.");
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            if (tracing == null) throw new InvalidPluginExecutionException("Initialize ITracingService fail.");
            if (context.Stage != (int)StageEnum.PostOperation) throw new InvalidPluginExecutionException("Stage does not equals PostOperation");
            if (context.PrimaryEntityName.ToLower() != "account".ToLower()) throw new InvalidPluginExecutionException("PrimaryEntityName does not equals account");
            if (context.MessageName.ToLower() != "new_AjaxAccount".ToLower()) throw new InvalidPluginExecutionException("MessageName does not equals new_AjaxAccount");
            if (context.Mode != (int)ExecutionModeEnum.Synchronous) throw new InvalidPluginExecutionException("Execution does not equals Synchronous");

            Debugger.Begin(tracing, context);

            var outputs = ExecuteCustomAction(context, serviceFactory, service, tracing);
            foreach (var output in outputs)
                if (context.OutputParameters.Contains(output.Key))
                    context.OutputParameters[output.Key] = output.Value;

            Debugger.End(tracing, context);
        }

        private ParameterCollection ExecuteCustomAction(IPluginExecutionContext context, IOrganizationServiceFactory serviceFactory, IOrganizationService service, ITracingService tracing)
        {
            var outputs = new ParameterCollection();
            //YOUR CUSTOM ACTION BEGIN HERE

            return outputs;
        }
    }
}