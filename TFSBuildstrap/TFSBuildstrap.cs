using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Build.Client;
using System.Threading;
using System.Diagnostics;

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
            if (args.Length < 3)
            {
                Debug.WriteLine("Required args are: [TFSURL] [PROJECTNAME] [BUILDCONFIGURATION]");
                return;
            }
            try
            {
                System.Environment.Exit(RunBuild(args[0], args[1], args[2]));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static int RunBuild(String t, String teamProject, String buildDefinition)
        {

            Console.WriteLine("##teamcity[progressStart 'Build in progress...']");
            int exitFlag = 0;
            // Get the specified team foundation server.
            TeamFoundationServer tfs = TeamFoundationServerFactory.GetServer(t);

            tfs.EnsureAuthenticated();

            // Get the IBuildServer - the main point of entry to the Team Build OM.
            IBuildServer buildServer = (IBuildServer)tfs.GetService(typeof(IBuildServer));

            // Get the build definition for which a build is to be queued.
            IBuildDefinition definition = buildServer.GetBuildDefinition(teamProject, buildDefinition);

            // Create a build request for the build definition.
            IBuildRequest request = definition.CreateBuildRequest();

            // Queue a build.
            IQueuedBuild qb = buildServer.QueueBuild(request, QueueOptions.None);

            Debug.WriteLine(String.Format("Build successfully queued. Position: {0}.", qb.QueuePosition));

            var buildStatusWatcher = new BuildStatusWatcher(qb.Id);

            buildStatusWatcher.Connect(buildServer, teamProject);

            do
            {

               Thread.Sleep(5000);
            } while (buildStatusWatcher.Status != QueueStatus.Completed && buildStatusWatcher.Status != QueueStatus.Canceled);

            if (buildStatusWatcher.Status == QueueStatus.Canceled)
            {
                exitFlag = 1;
            }

            Debug.WriteLine(String.Format("Log location for build at {0}", buildStatusWatcher.Build.LogLocation));

            buildStatusWatcher.Disconnect();

            Console.WriteLine("##teamcity[progressStart 'Build in progress...']");

            return exitFlag;
        }
    }
}
