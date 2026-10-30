using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Core.Sys.Caches.Sys;
using Core.Sys.Models.Sys;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using TSFramework.Libs.BaseApps;
using TSFramework.Libs.Models.Job;
using TSFramework.Libs.Processors;
using TSFramework.Libs.Providers;
using TSFramework.Libs.Utils;
using TSFramework.Libs.Providers; 

namespace CenIT.Solution.TOC.WebApp
{
    public class WebApiApplication : BaseHttpApplication
    {
        protected void Application_Start()
        {
            MvcHandler.DisableMvcResponseHeader = true;
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            GlobalHost.DependencyResolver.Register(typeof(IUserIdProvider), () => new SignalRUserProvider());

            #region Register Jobs

            var jobLibrariesPathFolder = ConfigurationManager.AppSettings["JobFolderPath"] ?? "/Libraries/Jobs";
            var registerJobs = int.Parse(ConfigurationManager.AppSettings["App_Register_Job"] ?? "0") != 0;
            if (registerJobs)
            {
                var jobCache = new SysJobCache();
                var lstActiveJobs = jobCache.GetAll().Where(j => j.IsActive).ToList();

                lstActiveJobs.ForEach(j =>
                {
                    try
                    {
                        var sFileName = j.JobLibrary;
                        var jobLibrariesAbsolutePathFolder = Server.MapPath(jobLibrariesPathFolder);
                        var jobLibrariesAbsoluteFilePath = string.Concat(jobLibrariesAbsolutePathFolder, "/", sFileName);

                        if (File.Exists(jobLibrariesAbsoluteFilePath))
                        {
                            var jobPlugable = JobPlugableProvider.GetJobPlugable(jobLibrariesAbsoluteFilePath);
                            JobSchedulerProvider.UpdateCronExpression(j.JobId.ToString(), j.CronExpression);

                            if (jobPlugable != null)
                                ExecuteJob(jobPlugable, j);
                        }
                    }
                    catch (Exception ex)
                    {
                        var jobName = string.IsNullOrWhiteSpace(j?.JobLibrary) ? "Application_Start" : j.JobLibrary;
                        AppProcessor.JobLogger.Error(jobName, ex);
                    }
                });
            }

            #endregion
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if (ConfigurationManager.AppSettings["MaintenanceMode"] != "true")
            {
                var ipRequestCache = new SysIpRequestCache();
                var ipRequestModel = ipRequestCache.GetByIp(Request.UserHostAddress);
                if (ipRequestModel == null || !ipRequestModel.IsLock) return;

                Response.Clear();
                Response.Status = "301 Moved Permanently";
            }

            if (!Request.IsLocal) HttpContext.Current.RewritePath("AppOffline.htm");
        }

        private void ExecuteJob(IJobPlugable jobPlugable, SysJobModel appJobModel)
        {
            NameValueCollection collectionParrams = null;
            if (!string.IsNullOrEmpty(appJobModel.JobParrams) && UtilString.IsValidJson(appJobModel.JobParrams))
            {
                var dictParrams = JsonConvert.DeserializeObject<Dictionary<string, string>>(appJobModel.JobParrams);
                if (dictParrams != null)
                {
                    collectionParrams = new NameValueCollection(dictParrams.Count);
                    foreach (var k in dictParrams) collectionParrams.Add(k.Key, k.Value);
                }
            }

            var instanceJob = jobPlugable?.BuildJob(appJobModel.JobId.ToString(), appJobModel.JobDescription,
                appJobModel.CronExpression, collectionParrams);
            if (instanceJob == null) return;

            instanceJob.MainJob.IsActive = appJobModel.IsActive;
            JobSchedulerProvider.RegisterJobScheduler(instanceJob.JobType, instanceJob.MainJob);
        }

    }
}
