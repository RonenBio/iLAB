using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Biosero.Workflow;
using Biosero.Workflow.ScreenControls;
using Biosero.DataModels.Parameters;

namespace Biosero.Scripting
{
    public class Handle_Request_Labware_Scan : WorkflowScript
    {
        public Handle_Request_Labware_Scan(WorkflowEnvironment environment) : base(environment) {}

        public void Run(Dictionary<string, object> parameters)
        {
            string scan = parameters["Scan"] as string;
            if (string.IsNullOrWhiteSpace(scan))
            {
                return;
            }
            else if (scan.Equals("Restart"))
            {
                CloseScreen("RequestLabware");
                return;
            }
            
            Parameter scanParameter = Program.Parameters.Get("Scan");
            scanParameter.Value = string.Empty;
            Parameter batchParameter = Program.Parameters.Get("Batch");
            batchParameter.Value = scan;
            Parameter nextStepParameter = Program.Parameters.Get("Next Step");
            nextStepParameter = "CreateOrder";
            
            CloseScreen("RequestLabware");
        }
        
        private void CloseScreen(string screenName)
        {
            Screen screen = GetScreen(screenName);
            screen.Close();
        }
    }
}