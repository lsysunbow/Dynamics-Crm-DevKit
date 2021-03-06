﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using PL.DynamicsCrm.DevKit.Cli.Models;

namespace PL.DynamicsCrm.DevKit.Cli
{
    public class WebResourceTask
    {
        public WebResourceTask(CrmServiceClient crmServiceClient, string currentDirectory, WebResource webResourceJson, string version)
        {
            CrmServiceClient = crmServiceClient;
            CurrentDirectory = currentDirectory;
            WebResourceJson = webResourceJson;
            Version = version;
            WebResourcesToPublish = new List<Guid>();
        }

        private CrmServiceClient CrmServiceClient { get; }
        private WebResource WebResourceJson { get; }
        private string CurrentDirectory { get; }
        private string Version { get; }
        private List<Guid> WebResourcesToPublish { get; }

        private List<WebResourceFile> WebResourceFiles
        {
            get
            {
                var items = new List<WebResourceFile>();
                var includefiles = new List<string>();
                foreach (var pattern in WebResourceJson.includefiles)
                {
                    var filePattern = $"{CurrentDirectory}\\{WebResourceJson.rootfolder}\\{pattern}";
                    filePattern = filePattern.Replace(@"\\", @"\");
                    includefiles.AddRange(GetFiles(filePattern));
                }
                var excludefiles = new List<string>();
                foreach (var pattern in WebResourceJson.excludefiles)
                {
                    var filePattern = $"{CurrentDirectory}\\{WebResourceJson.rootfolder}\\{pattern}";
                    filePattern = filePattern.Replace(@"\\", @"\");
                    excludefiles.AddRange(GetFiles(filePattern));
                }
                var files = includefiles.Where(file => !excludefiles.Contains(file)).ToList();
                foreach (var file in files)
                {
                    var name = WebResourceJson.prefix + "/" + file
                                   .Substring($"{CurrentDirectory}\\{WebResourceJson.rootfolder}\\".Replace(@"\\", @"\")
                                       .Length).Replace("\\", "/");
                    var webResourceFile = new WebResourceFile
                    {
                        file = file,
                        version = Version,
                        uniquename = name,
                        displayname = name
                    };
                    items.Add(webResourceFile);
                }
                return items;
            }
        }

        public void Run()
        {
            CliLog.WriteLine(CliLog.ColorGreen, new string('*', CliLog.StarLength));
            CliLog.WriteLine(CliLog.ColorGreen, "START WEBRESOURCE TASKS");
            CliLog.WriteLine(CliLog.ColorGreen, new string('*', CliLog.StarLength));
            var files = WebResourceFiles;
            var totalWebResources = files.Count;
            CliLog.WriteLine(CliLog.ColorGreen, "Found: ", CliLog.ColorCyan, totalWebResources, CliLog.ColorGreen, " webresources");
            var i = 1;
            foreach (var webResourceFile in files)
            {
                DeployWebResource(webResourceFile, i, totalWebResources);
                i++;
            }
            if (IsSupportWebResourceDependency)
            {
                var dependencies = MergeDependencies(WebResourceJson.dependencies);
                CliLog.WriteLine(CliLog.ColorGreen, "Found: ", CliLog.ColorCyan, dependencies.Count, CliLog.ColorGreen, " dependencies");
                var j = 1;
                foreach (var dependency in dependencies)
                {
                    UpdateDependency(dependency, j, dependencies.Count);
                    j++;
                }
            }
            if (WebResourcesToPublish.Count > 0)
                PublishWebResources();
            CliLog.WriteLine(CliLog.ColorGreen, new string('*', CliLog.StarLength));
            CliLog.WriteLine(CliLog.ColorGreen, "END WEBRESOURCE TASKS");
            CliLog.WriteLine(CliLog.ColorGreen, new string('*', CliLog.StarLength));
        }

        private List<Dependency> MergeDependencies(IEnumerable<Dependency> dependencies)
        {
            var list = new List<Dependency>();
            foreach(var dependency in dependencies)
            {
                foreach(var webreource in dependency.webresources)
                {
                    var found = list.FirstOrDefault(d => d.webresources.Contains(webreource));
                    if (found == null)
                    {
                        list.Add(new Dependency
                        {
                            webresources = new List<string>() { webreource },
                            dependencies = dependency.dependencies
                        });
                    }
                    else
                    {
                        var temp = new List<string>(found.dependencies);
                        temp.AddRange(dependency.dependencies);
                        found.dependencies = temp;
                    }
                }
            }
            return list;
        }

        private void UpdateDependency(Dependency dependency, int j, int count)
        {
            var len = count.ToString().Length;
            var dependencyXml = GetDependencyXml(dependency.dependencies);
            foreach (var webResourceName in dependency.webresources)
            {
                var fetchXml = $@"
<fetch>
  <entity name='webresource'>
    <attribute name='dependencyxml' />
    <filter type='and'>
      <condition attribute='name' operator='eq' value='{webResourceName}'/>
    </filter>
  </entity>
</fetch>";
                var rows = CrmServiceClient.RetrieveMultiple(new FetchExpression(fetchXml));
                var existingDependencyXml = string.Empty;
                if (rows.Entities.Count > 0)
                    existingDependencyXml = rows.Entities[0].GetAttributeValue<string>("dependencyxml");
                if (existingDependencyXml != dependencyXml)
                {
                    var webResourceId = rows.Entities[0].Id;
                    var enttiy = new Entity("webresource", webResourceId)
                    {
                        ["dependencyxml"] = dependencyXml
                    };
                    CliLog.WriteLine(CliLog.ColorCyan, string.Format("{0,0}|{1," + len + "}", "", j), ": ", CliLog.ColorBlue, "Updated Dependency Webresource ", CliLog.ColorCyan, webResourceName);
                    CrmServiceClient.Update(enttiy);
                    if (!WebResourcesToPublish.Contains(webResourceId))
                        WebResourcesToPublish.Add(webResourceId);
                }
                else
                {
                    CliLog.WriteLine(CliLog.ColorCyan, string.Format("{0,0}|{1," + len + "}", "", j) + ": Done");
                }
            }
        }

        private string GetDependencyXml(IEnumerable<string> dependencies)
        {
            var library = string.Empty;
            foreach (var dependency in dependencies)
            {
                var fetchData = new
                {
                    name = dependency
                };
                var fetchXml = $@"
<fetch>
  <entity name='webresource'>
    <attribute name='webresourceid' />
    <attribute name='languagecode' />
    <attribute name='name' />
    <attribute name='displayname' />
    <attribute name='description' />
    <attribute name='webresourceidunique' />
    <filter type='and'>
      <condition attribute='name' operator='eq' value='{fetchData.name}'/>
    </filter>
  </entity>
</fetch>";
                var rows = CrmServiceClient.RetrieveMultiple(new FetchExpression(fetchXml));
                if (rows.Entities.Count == 0) return string.Empty;
                var entity = rows.Entities[0];
                var name = entity.GetAttributeValue<string>("name");
                var displayname = entity.GetAttributeValue<string>("displayname");
                var description = entity.GetAttributeValue<string>("description");
                var webresourceidunique = entity.GetAttributeValue<Guid>("webresourceidunique");
                var languagecode = entity.GetAttributeValue<int?>("languagecode");
                library += $"<Library name='{name}' displayName='{displayname}' languagecode='{languagecode}' description='{description}' libraryUniqueId='{{{webresourceidunique}}}'/>";
            }
            var dependencyXml = $"<Dependencies><Dependency componentType='WebResource'>{library}</Dependency></Dependencies>";
            dependencyXml = dependencyXml.Replace("'", "\"");
            return dependencyXml;
        }

        private Entity DeployWebResource(WebResourceFile webResourceFile, int current, int totalWebResources)
        {
            var len = totalWebResources.ToString().Length;
            if (webResourceFile.uniquename.StartsWith("/")) webResourceFile.uniquename = webResourceFile.uniquename.Substring(1);
            var fetchData = new
            {
                name = webResourceFile.uniquename
            };
            var fetchXml = $@"
<fetch>
  <entity name='webresource'>
    <attribute name='webresourceid' />
    <attribute name='content' />
    <filter type='and'>
      <condition attribute='name' operator='eq' value='{fetchData.name}'/>
    </filter>
  </entity>
</fetch>";
            var rows = CrmServiceClient.RetrieveMultiple(new FetchExpression(fetchXml));
            var content = string.Empty;
            var webResourceId = Guid.Empty;
            if (rows.Entities.Count > 0)
            {
                var entity = rows.Entities[0];
                webResourceId = entity.Id;
                content = entity?["content"]?.ToString();
            }
            var fileContent = Convert.ToBase64String(File.ReadAllBytes(webResourceFile.file));
            if (fileContent == content)
            {
                CliLog.WriteLine(CliLog.ColorCyan, string.Format("{0,0}|{1," + len + "}", "", current) + ": Done");
                return null;
            }
            var webResource = new Entity("webresource")
            {
                ["name"] = webResourceFile.uniquename,
                ["displayname"] = webResourceFile.displayname,
                ["description"] = webResourceFile.version,
                ["content"] = fileContent
            };
            var webResourceFileInfo = new FileInfo(webResourceFile.file);
            var filetype = WebResourceWebResourceType.ScriptJScript;
            switch (webResourceFileInfo.Extension.ToLower().TrimStart('.'))
            {
                case "html":
                case "htm":
                    filetype = WebResourceWebResourceType.WebpageHtml;
                    break;
                case "js":
                    filetype = WebResourceWebResourceType.ScriptJScript;
                    break;
                case "png":
                    filetype = WebResourceWebResourceType.PngFormat;
                    break;
                case "gif":
                    filetype = WebResourceWebResourceType.GifFormat;
                    break;
                case "jpg":
                case "jpeg":
                    filetype = WebResourceWebResourceType.JpgFormat;
                    break;
                case "css":
                    filetype = WebResourceWebResourceType.StyleSheetCss;
                    break;
                case "ico":
                    filetype = WebResourceWebResourceType.IcoFormat;
                    break;
                case "xml":
                    filetype = WebResourceWebResourceType.DataXml;
                    break;
                case "xsl":
                case "xslt":
                    filetype = WebResourceWebResourceType.StyleSheetXsl;
                    break;
                case "xap":
                    filetype = WebResourceWebResourceType.SilverlightXap;
                    break;
                case "resx":
                    filetype = WebResourceWebResourceType.StringResx;
                    break;
                case "svg":
                    filetype = WebResourceWebResourceType.SvgFormat;
                    break;
            }
            webResource["webresourcetype"] = new OptionSetValue((int) filetype);
            if (filetype == WebResourceWebResourceType.StringResx)
            {
                var fileName = webResourceFileInfo.Name.Substring(0, webResourceFileInfo.Name.Length - webResourceFileInfo.Extension.Length);
                var arr = fileName.Split(".".ToCharArray());
                if (int.TryParse(arr[arr.Length - 1], out var languagecode))
                {
                    var req = new RetrieveProvisionedLanguagesRequest();
                    var res = (RetrieveProvisionedLanguagesResponse)CrmServiceClient.Execute(req);
                    if (res.RetrieveProvisionedLanguages.Contains(languagecode))
                        webResource["languagecode"] = languagecode;
                    else
                    {
                        CliLog.WriteLine(CliLog.ColorRed, "Language code not found: ", CliLog.ColorBlue, languagecode);
                        return null;
                    }
                }
            }
            if (webResourceId == Guid.Empty)
            {
                CliLog.WriteLine(CliLog.ColorCyan, string.Format("{0,0}|{1," + len + "}", "", current), ": ", CliLog.ColorGreen, "Creating WebResource ", CliLog.ColorCyan, webResourceFile.file, CliLog.ColorGreen, " to ", CliLog.ColorCyan, webResourceFile.uniquename);
                webResourceId = CrmServiceClient.Create(webResource);
                webResource["webresourceid"] = webResourceId;
            }
            else
            {
                webResource["webresourceid"] = webResourceId;
                CliLog.WriteLine(CliLog.ColorCyan, string.Format("{0,0}|{1," + len + "}", "", current), ": ", CliLog.ColorBlue, "Updating WebResource ", CliLog.ColorCyan, webResourceFile.file, CliLog.ColorGreen, " to ", CliLog.ColorCyan, webResourceFile.uniquename);
                CrmServiceClient.Update(webResource);
            }
            WebResourcesToPublish.Add(webResourceId);
            AddWebResourceToSolution(webResource);
            return webResource;
        }

        private void AddWebResourceToSolution(Entity webResource)
        {
            var fetchData = new
            {
                objectid = Guid.Parse(webResource["webresourceid"].ToString()),
                componenttype = 61,
                uniquename = WebResourceJson.solution
            };
            var fetchXml = $@"
<fetch>
  <entity name='solutioncomponent'>
    <attribute name='solutioncomponentid' />
    <filter type='and'>
      <condition attribute='objectid' operator='eq' value='{fetchData.objectid}'/>
      <condition attribute='componenttype' operator='eq' value='{fetchData.componenttype}'/>
    </filter>
    <link-entity name='solution' from='solutionid' to='solutionid'>
      <filter type='and'>
        <condition attribute='uniquename' operator='eq' value='{fetchData.uniquename}'/>
      </filter>
    </link-entity>
  </entity>
</fetch>";
            var rows = CrmServiceClient.RetrieveMultiple(new FetchExpression(fetchXml));
            if (rows.Entities.Count != 0) return;
            var request = new AddSolutionComponentRequest
            {
                AddRequiredComponents = true,
                ComponentType = 61,
                ComponentId = Guid.Parse(webResource["webresourceid"].ToString()),
                SolutionUniqueName = WebResourceJson.solution
            };
            CliLog.WriteLine(CliLog.ColorCyan, "|", CliLog.ColorGreen, "\tAdding WebResource: ", CliLog.ColorCyan,
                $"{webResource["name"]} ", CliLog.ColorGreen, "to solution: ", CliLog.ColorCyan,
                $"{WebResourceJson.solution}");
            CrmServiceClient.Execute(request);
        }

        private void PublishWebResources()
        {
            var stringGuids = WebResourcesToPublish.Select(g => g.ToString());
            var webresources = string.Join("</webresource><webresource>", stringGuids);
            var publish = new PublishXmlRequest
            {
                ParameterXml =
                    "<importexportxml><webresources>" +
                    "<webresource>" + webresources + "</webresource>" +
                    "</webresources></importexportxml>"
            };
            CliLog.WriteLine(CliLog.ColorYellow, "Publishing WebResources");
            CrmServiceClient.Execute(publish);
            CliLog.WriteLine(CliLog.ColorYellow, "Published WebResources");
        }

        private IEnumerable<string> GetFiles(string filePattern)
        {
            var folder = filePattern.Substring(0, filePattern.LastIndexOf("\\", StringComparison.Ordinal));
            var pattern = filePattern.Substring(folder.Length + 1);
            if (!pattern.Contains("**")) return Directory.Exists(folder) ? Directory.GetFiles(folder, pattern, SearchOption.TopDirectoryOnly).ToList() : new List<string>();
            pattern = pattern.Replace("**", "*");
            return Directory.GetFiles(folder, pattern, SearchOption.AllDirectories).ToList();
        }

        private enum WebResourceWebResourceType
        {
            WebpageHtml = 1,
            StyleSheetCss = 2,
            ScriptJScript = 3,
            DataXml = 4,
            PngFormat = 5,
            JpgFormat = 6,
            GifFormat = 7,
            SilverlightXap = 8,
            StyleSheetXsl = 9,
            IcoFormat = 10,
            SvgFormat = 11,
            StringResx = 12
        }

        private bool? _isSupportWebResourceDependency = null;
        private bool IsSupportWebResourceDependency
        {
            get
            {
                if (_isSupportWebResourceDependency != null) return _isSupportWebResourceDependency.Value;
                var request = new RetrieveVersionRequest();
                var response = (RetrieveVersionResponse)CrmServiceClient.Execute(request);
                var version = new Version(response.Version);
                _isSupportWebResourceDependency = version >= new Version("9.0");
                return _isSupportWebResourceDependency.Value;
            }
        }
    }
}