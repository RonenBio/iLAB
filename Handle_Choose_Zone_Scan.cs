using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Biosero.Workflow;
using Biosero.DataModels.Parameters;

namespace Biosero.Scripting
{
    public class Handle_Choose_Zone_Scan : WorkflowScript
    {
        public Handle_Choose_Zone_Scan(WorkflowEnvironment environment) : base(environment) {}

        public void Run(Dictionary<string, object> parameters)
        {
            string scan = parameters["Scan"] as string;
            
            if (string.IsNullOrWhiteSpace(scan))
            {
                return;
            }
            else if (scan.Equals("Restart"))
            {
                CloseScreen("Startup");
                return;
            }
            
            int zone = scan.Contains("ZoneOne") ? 1 : 2;
            parameters["Zone To Edit"] = zone;
            
            ResetParameter("Scan");
            
            CloseScreen("SelectBatchToCreate");
        }
        
        private void ResetParameter (string parameterName)
        {
		Parameter resetParameter = Program.Parameters.Get(parameterName);
		resetParameter.Value = string.Empty;
        }
        
        private void CloseScreen(string screenName)
        {
            Screen screen = GetScreen(screenName);
            screen.Close();
        }
        
    }
}