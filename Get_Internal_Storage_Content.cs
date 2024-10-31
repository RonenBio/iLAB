reference Newtonsoft.Json.dll
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Linq;
using Biosero.Workflow;
using Biosero.Workflow.ScreenControls;
using Biosero.DataModels.Events;
using Biosero.DataModels.Resources;
using Newtonsoft.Json;

namespace Biosero.Scripting
{
    public class Get_Internal_Storage_Content : WorkflowScript
    {
        private const string Barcode = "Barcode";
        private const string Zone = "Zone";
        private const string Labware = "Labware";
        private const string SampleRack = "Sample Rack";
        private const string UIRack = "UI Rack";

        public Get_Internal_Storage_Content(WorkflowEnvironment environment) : base(environment) {}

        public void Run(Dictionary<string, object> parameters)
        {
            try
            {
                Identity tableIdentity = Environment.QueryClient.GetIdentity("Internal_Hotel_Hotels");
                
                DataTable table = JsonConvert.DeserializeObject<DataTable>(tableIdentity.Properties.GetValue<string>("Storage"));
                
                for (int batchNumber = 1; batchNumber <= 1; batchNumber++)
                {
                    var sampleRacks = table.Rows.Cast<DataRow>()
                        .Where(r => !string.IsNullOrWhiteSpace(r[Barcode].ToString()) &&
                                             r[Zone].ToString().Contains($"Batch {batchNumber}") &&
                                             r[Labware].ToString().Equals(SampleRack))
                        .Select(r => r[Barcode].ToString());
                    var uiRacks = table.Rows.Cast<DataRow>()
                        .Where(r => !string.IsNullOrWhiteSpace(r[Barcode].ToString()) &&
                                             r[Zone].ToString().Contains($"Batch {batchNumber}") &&
                                             r[Labware].ToString().Equals(UIRack))
                        .Select(r => r[Barcode].ToString());
                    
                    string state = string.Empty;
                    if (sampleRacks.Count() > 0)
                    {
                        state = GetProcessStage(sampleRacks.FirstOrDefault());
                    }
                    
                    parameters[$"Batch {batchNumber} Sample Racks"] = string.Join(System.Environment.NewLine + System.Environment.NewLine, sampleRacks);
                    parameters[$"Batch {batchNumber} UI Racks"] = string.Join(System.Environment.NewLine + System.Environment.NewLine, uiRacks);
                    parameters[$"Batch {batchNumber} State"] = state;
                }
            }
            catch (Exception e)
            {
                Log(JsonConvert.SerializeObject(e.Message));
            }
        }
        
        private string GetProcessStage(string rackBarcode)
        {
            string identifier = Environment.QueryClient.GetIdentityByName(rackBarcode).Identifier;
            
            EventSearchParameters searchParams = new EventSearchParameters()
            {
            	Topic = "AZ.DataModels.Events.ProcessStageChangedEvent",
            	SubjectsContains = identifier
            };
            
            var processStageChangedEvents = Environment.QueryClient.GetEvents(searchParams, 10000, 0)?.ToList() ?? new List<EventMessage>();
            
            EventMessage eventMessage = processStageChangedEvents?.LastOrDefault();
            if (eventMessage == null)
            {
                return "Submitted";
            }
            
            dynamic processStageChangedEvent = JsonConvert.DeserializeObject<dynamic>(eventMessage.Data);
            string processStage = processStageChangedEvent.ProcessStage.ToString();
            if (processStage.Split('_').Count() < 2)
            {
            	return "Submitted";
            }
            
            return processStage.Split('_')[1];
        }
    }
}