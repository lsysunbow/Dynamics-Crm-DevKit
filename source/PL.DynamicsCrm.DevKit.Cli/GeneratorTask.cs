﻿using System.Collections.Generic;
using System.IO;
using Microsoft.Xrm.Tooling.Connector;
using PL.DynamicsCrm.DevKit.Cli.Models;
using PL.DynamicsCrm.DevKit.Shared;

namespace PL.DynamicsCrm.DevKit.Cli
{
    class GeneratorTask
    {
        private CrmServiceClient CrmServiceClient { get; }
        private Generator GeneratorJson { get; }
        private string CurrentDirectory { get; }
        private string Version { get; }

        public GeneratorTask(CrmServiceClient crmServiceClient, string currentDirectory, Generator lateBoundJson, string version)
        {
            CrmServiceClient = crmServiceClient;
            CurrentDirectory = currentDirectory;
            GeneratorJson = lateBoundJson;
            Version = version;
        }

        internal void Run()
        {
            CliLog.WriteLine(CliLog.ColorGreen, new string('*', CliLog.StarLength));


            if (GeneratorJson.type.ToLower() == "csharp" || GeneratorJson.type.ToLower() == "c#")
                GeneratorLateBound();
            else if (GeneratorJson.type.ToLower() == "jsform")
                GeneratorJsForm();
            else if (GeneratorJson.type.ToLower() == "jswebapi")
                GeneratorWebApi();


            CliLog.WriteLine(CliLog.ColorGreen, new string('*', CliLog.StarLength));
        }

        private void GeneratorWebApi()
        {
            CliLog.WriteLine(CliLog.ColorGreen, "START GENERATOR - JS WEBAPI - TASKS");
            CliLog.WriteLine(CliLog.ColorGreen, new string('*', CliLog.StarLength));
            var entities = new List<string>();
            var pattern = "*.webapi.js";
            var folder = $"{CurrentDirectory}\\{GeneratorJson.rootfolder}";
            var files = Directory.GetFiles(folder, pattern);
            foreach (var file in files)
            {
                var fInfo = new FileInfo(file);
                var columns = fInfo.Name.Split(".".ToCharArray());
                entities.Add(columns[0]);
            }
            if (entities.Count == 0)
            {
                CliLog.WriteLine(CliLog.ColorRed, "NOT FOUND ENTIIES!!!!");
                return;
            }
            CliLog.WriteLine(CliLog.ColorGreen, "Found: ", CliLog.ColorCyan, entities.Count, CliLog.ColorGreen, " entities");
            var i = 1;
            foreach (var entity in entities)
            {
                GeneratorJsWebApi(entity, i, entities.Count);
                i++;
            }
            CliLog.WriteLine(CliLog.ColorGreen, new string('*', CliLog.StarLength));
            CliLog.WriteLine(CliLog.ColorGreen, "END GENERATOR - JS WEBAPI - TASKS");
        }

        private void GeneratorJsWebApi(string entity, int i, int count)
        {
            CliLog.WriteLine(CliLog.ColorCyan, string.Format("{0,0}|{1," + count.ToString().Length + "}", "", i) + ": Processing ", CliLog.ColorGreen, entity);
            var fileIntellisense = $"{CurrentDirectory}\\{GeneratorJson.rootfolder}\\{entity}.intellisense.js";
            var lines = File.ReadAllLines(fileIntellisense);
            var json = lines[lines.Length - 1];
            var comment = SimpleJson.DeserializeObject<CommentIntellisense>(json.Substring("//".Length).Replace("'", "\""));
            var parts = GeneratorJson.rootnamespace.Split(".".ToCharArray());
            var projectName = parts.Length > 1 ? parts[1] : parts[0];
            var jsWebApi = new JsWebApi(CrmServiceClient.OrganizationServiceProxy, projectName, entity, comment.IsDebugWebApi, comment.JsForm, comment.IsDebugForm);
            jsWebApi.GeneratorCode();
            var fileWebApi = $"{CurrentDirectory}\\{GeneratorJson.rootfolder}\\{entity}.webapi.js";
            File.WriteAllText(fileIntellisense, jsWebApi.WebApiCodeIntellisense, System.Text.Encoding.UTF8);
            File.WriteAllText(fileWebApi, jsWebApi.WebApiCode, System.Text.Encoding.UTF8);

        }

        private void GeneratorJsForm()
        {
            CliLog.WriteLine(CliLog.ColorGreen, "START GENERATOR - JS FORM - TASKS");
            CliLog.WriteLine(CliLog.ColorGreen, new string('*', CliLog.StarLength));
            var entities = new List<string>();
            const string pattern = "*.form.js";
            var folder = $"{CurrentDirectory}\\{GeneratorJson.rootfolder}";
            var files = Directory.GetFiles(folder, pattern);
            foreach (var file in files)
            {
                var fInfo = new FileInfo(file);
                var columns = fInfo.Name.Split(".".ToCharArray());
                entities.Add(columns[0]);
            }
            if (entities.Count == 0)
            {
                CliLog.WriteLine(CliLog.ColorRed, "NOT FOUND ENTIIES!!!!");
                return;
            }

            CliLog.WriteLine(CliLog.ColorGreen, "Found: ", CliLog.ColorCyan, entities.Count, CliLog.ColorGreen, " entities");
            var i = 1;
            foreach (var entity in entities)
            {
                GeneratorJsForm(entity, i, entities.Count);
                i++;
            }
            CliLog.WriteLine(CliLog.ColorGreen, new string('*', CliLog.StarLength));
            CliLog.WriteLine(CliLog.ColorGreen, "END GENERATOR - JS FORM - TASKS");
        }

        private void GeneratorJsForm(string entity, int i, int count)
        {
            CliLog.WriteLine(CliLog.ColorCyan, string.Format("{0,0}|{1," + count.ToString().Length + "}", "", i) + ": Processing ", CliLog.ColorGreen, entity);
            var fileIntellisense = $"{CurrentDirectory}\\{GeneratorJson.rootfolder}\\{entity}.intellisense.js";
            var lines = File.ReadAllLines(fileIntellisense);
            var json = lines[lines.Length - 1];
            var comment = SimpleJson.DeserializeObject<CommentIntellisense>(json.Substring("//".Length).Replace("'", "\""));
            var parts = GeneratorJson.rootnamespace.Split(".".ToCharArray());
            var projectName = parts.Length > 1 ? parts[1] : parts[0];
            var jsForm = new JsForm(CrmServiceClient.OrganizationServiceProxy, projectName, entity);
            jsForm.GeneratorCode(comment.JsForm, comment.IsDebugForm, comment.JsWebApi, comment.IsDebugWebApi);
            var fileForm = $"{CurrentDirectory}\\{GeneratorJson.rootfolder}\\{entity}.form.js";
            File.WriteAllText(fileIntellisense, jsForm.FormCodeIntellisense, System.Text.Encoding.UTF8);
            File.WriteAllText(fileForm, jsForm.FormCode, System.Text.Encoding.UTF8);
        }

        private void GeneratorLateBound()
        {
            CliLog.WriteLine(CliLog.ColorGreen, "START GENERATOR - LATE BOUND - TASKS");
            CliLog.WriteLine(CliLog.ColorGreen, new string('*', CliLog.StarLength));
            var entities = new List<string>();
            const string pattern = "*.generated.cs";
            var folder = $"{CurrentDirectory}\\{GeneratorJson.rootfolder}";
            var files = Directory.GetFiles(folder, pattern);
            foreach (var file in files)
            {
                var fInfo = new FileInfo(file);
                var columns = fInfo.Name.Split(".".ToCharArray());
                entities.Add(columns[0]);
            }
            if (entities.Count == 0)
            {
                CliLog.WriteLine(CliLog.ColorRed, "NOT FOUND ENTIIES!!!!");
                return;
            }

            CliLog.WriteLine(CliLog.ColorGreen, "Found: ", CliLog.ColorCyan, entities.Count, CliLog.ColorGreen, " entities");
            var i = 1;
            foreach (var entity in entities)
            {
                GeneratorLateBound(entity, i, entities.Count);
                i++;
            }
            CliLog.WriteLine(CliLog.ColorGreen, new string('*', CliLog.StarLength));
            CliLog.WriteLine(CliLog.ColorGreen, "END GENERATOR - LATE BOUND - TASKS");
        }

        private void GeneratorLateBound(string entity, int i, int count)
        {
            CliLog.WriteLine(CliLog.ColorCyan, string.Format("{0,0}|{1," + count.ToString().Length + "}", "", i) + ": Processing ", CliLog.ColorGreen, entity);

            var lateBound = new GeneratedCSharpLateBound();
            var rootNameSpace = GeneratorJson.rootnamespace + ".Shared.Entities";
            var sharedNameSpace = GeneratorJson.rootnamespace + ".";
            var generated = lateBound.Go(CrmServiceClient.OrganizationServiceProxy, entity, rootNameSpace, sharedNameSpace);
            var file = $"{CurrentDirectory}\\{GeneratorJson.rootfolder}\\{entity}.generated.cs";
            File.WriteAllText(file, generated, System.Text.Encoding.UTF8);
        }
    }
}
