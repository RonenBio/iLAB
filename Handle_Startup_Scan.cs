using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Biosero.Workflow;
using Biosero.DataModels.Parameters;

namespace Biosero.Scripting
{
    public class Handle_Startup_Scan : WorkflowScript
    {
        public Handle_Startup_Scan(WorkflowEnvironment environment) : base(environment) {}

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
            
            ResetParameter("Scan");
            
            Parameter nextStepParameter = Program.Parameters.Get("Next Step");
            nextStepParameter.Value = scan;
            
            CloseScreen("Startup");
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