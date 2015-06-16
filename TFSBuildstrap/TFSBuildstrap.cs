//<csscript> 
//  <references> 
//    <reference>System</reference> 
//    <reference>System.Core</reference> 
//    <reference>System.Data</reference> 
//    <reference>System.Data.DataSetExtensions</reference> 
//    <reference>System.Xml</reference> 
//    <reference>System.Xml.Linq</reference> 
//    <reference>Microsoft.CSharp</reference> 
//    <reference>Microsoft.TeamFoundation.Build.Client</reference>
//    <reference>Microsoft.TeamFoundation.Client</reference>
//  </references> 
//  <mode>exe</mode> 
//  <requiredframework>3.5</requiredframework>
//  <requiredplatform>x64</requiredplatform>

//<file>Program.cs</file> 

//</csscript>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Build.Client;
using System.Threading;

namespace TFSBuildstrap
{
    public class BuildResult
    {
        public bool WasSuccessfully { get; set; }
        public IBuildDetail BuildDetail { get; set; }
    }

    public class BuildStatusWatcher
    {
        private IQueuedBuildsView _queuedBuildsView;
        private readonly int _queueBuildId;
        private QueueStatus _status;
        private IBuildDetail _build;

        public BuildStatusWatcher(int queueBuildId)
        {
            _queueBuildId = queueBuildId;
        }

        public IBuildDetail Build
        {
            get { return _build; }
        }

        public QueueStatus Status
        {
            get { return _status; }
        }

        public void Connect(IBuildServer buildServer, string tfsProject)
        {
            _queuedBuildsView = buildServer.CreateQueuedBuildsView(tfsProject);
            _queuedBuildsView.StatusChanged += QueuedBuildsViewStatusChanged;
            _queuedBuildsView.Connect(10000, null);
        }

        public void Disconnect()
        {
            _queuedBuildsView.Disconnect();
        }

        private void QueuedBuildsViewStatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (e.Changed)
            {
                var queuedBuild = _queuedBuildsView.QueuedBuilds.FirstOrDefault(x => x.Id == _queueBuildId);
                if (queuedBuild != null)
                {
                    _status = queuedBuild.Status;
                    _build = queuedBuild.Build;
                }
            }
        }
    }
    
    class TFSBuildstrap
    {
        static void Main(string[] args)
        {
            if (args.Length  < 3) {
                Console.WriteLine("Required args are: [TFSURL] [PROJECTNAME] [BUILDCONFIGURATION]");
                return;
            }
            try
            {
                RunBuild(args[0], args[1], args[2]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void RunBuild(String t, String teamProject, String buildDefinition)
        {
            // Get the specified team foundation server.
            TeamFoundationServer tfs = TeamFoundationServerFactory.GetServer(t);

            // Get the IBuildServer - the main point of entry to the Team Build OM.
            IBuildServer buildServer = (IBuildServer)tfs.GetService(typeof(IBuildServer));

            // Get the build definition for which a build is to be queued.
            IBuildDefinition definition = buildServer.GetBuildDefinition(teamProject, buildDefinition);

            // Create a build request for the build definition.
            IBuildRequest request = definition.CreateBuildRequest();

            // Queue a build.
            IQueuedBuild qb = buildServer.QueueBuild(request, QueueOptions.None);

            Console.WriteLine(String.Format("Build successfully queued. Position: {0}.", qb.QueuePosition));

            var buildStatusWatcher = new BuildStatusWatcher(qb.Id);

            buildStatusWatcher.Connect(buildServer, teamProject);

            do
            {
                Console.WriteLine("Build:{0} is {1} Latest Log at {2}", buildStatusWatcher.Build.BuildNumber,buildStatusWatcher.Status );

                Thread.Sleep(1000);

            } while (buildStatusWatcher.Status != QueueStatus.Completed && buildStatusWatcher.Status != QueueStatus.Canceled);

            Console.WriteLine("Log location for build at {0}", buildStatusWatcher.Build.LogLocation);

            buildStatusWatcher.Disconnect();
        }
    }
}
