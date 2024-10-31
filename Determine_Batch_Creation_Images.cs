using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Biosero.Workflow;
using Biosero.Workflow.ScreenControls;

namespace Biosero.Scripting
{
    public class Determine_Batch_Creation_Images : WorkflowScript
    {
        public Determine_Batch_Creation_Images(WorkflowEnvironment environment) : base(environment) {}

        public void Run(Dictionary<string, object> parameters)
        {
            string priority = parameters["Priority"].ToString();
            if (String.IsNullOrEmpty(priority))
            {
            	return;
            }
            Screen screen = GetScreen("SelectBatchToCreate");
            ImageControl zoneOneScan = screen.Controls.FirstOrDefault(x => x.Name == "ZoneOneScan") as ImageControl;
            ImageControl zoneTwoScan = screen.Controls.FirstOrDefault(x => x.Name == "ZoneTwoScan") as ImageControl;
            
            string imageBasePath = @"C:\Biosero\Lab Experience\PreAnalysis_AddRemoveLabwareImages\";
            
            zoneOneScan.Url = $"{imageBasePath}{priority}PriorityZoneOne.png";
            zoneOneScan.RuntimeUrl = $"{imageBasePath}{priority}PriorityZoneOne.png";
            zoneTwoScan.Url = $"{imageBasePath}{priority}PriorityZoneTwo.png";
            zoneTwoScan.RuntimeUrl = $"{imageBasePath}{priority}PriorityZoneTwo.png";
        }
    }
}