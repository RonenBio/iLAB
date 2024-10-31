reference Newtonsoft.Json.dll
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Linq;
using Biosero.Workflow;
using Biosero.DataModels.Ordering;
using Biosero.DataModels.Parameters;
using Biosero.DataModels.Resources;
using Newtonsoft.Json;

namespace Biosero.Scripting
{
    public class Create_Order : WorkflowScript
    {
        public Create_Order(WorkflowEnvironment environment) : base(environment) {}

        public void Run(Dictionary<string, object> parameters)
        {
            try
            {
                bool requestLabware = (parameters["Next Step"] as string) == "RequestLabware";
                List<string> sampleRacks = new List<string>();
                List<string> uiRacks = new List<string>();
                string hplcPlate = string.Empty;
                if (!requestLabware && !ValidateLabware(int.Parse(parameters["Zone To Edit"].ToString()), out List<string> errors, out sampleRacks, out uiRacks, out hplcPlate))
                {
                    parameters["Batch Submission Text"] = "Unable to validate labware to create batch: " +
                        System.Environment.NewLine +
                        System.Environment.NewLine +
                        string.Join(System.Environment.NewLine, errors);
                    return;
                }
                
                string templateName = requestLabware ? "Unload Labware" : "Scan Labware";
                OrderTemplate template = GetTemplate(templateName);
                
                SetInputParameters(template, parameters, sampleRacks, uiRacks, hplcPlate);
                
                Order order = MakeOrderFromTemplate(template);
                
                SubmitOrder(order);
                
                parameters["Batch Submission Text"] = "Batch Created!";
            }
            catch (Exception e)
            {
                File.AppendAllLines(@"C:\Biosero\Lab Experience\Error.txt", new string[] { e.Message });
            }
        }
        
        private bool ValidateLabware(int batchZone, out  List<string> errors, out List<string> sampleRacks, out List<string> uiRacks, out string hplcPlate)
        {
            errors = new List<string>();
            sampleRacks = new List<string>();
            uiRacks = new List<string>();
            hplcPlate = string.Empty;
            
            string tableName = "Plate_Storage_Hotels";
            Identity tableIdentity = Environment.QueryClient.GetIdentity(tableName);
            
            DataTable table = JsonConvert.DeserializeObject<DataTable>(tableIdentity.Properties.GetValue<string>("Storage"));
            
            string hotel = "Hotel", shelf = "Shelf", barcode = "Barcode";
            List<string> hplcPlateHotels = new List<string>() { "Hotel 1" };
            List<string> hplcPlateShelves = batchZone == 1 ? new List<string>() { "4" } : new List<string>() { "2" };
            List<string> sampleRackHotels = new List<string>() { "Hotel 1", "Hotel 2", "Hotel 3", "Hotel 4" };
            List<string> sampleRackShelves = batchZone == 1 ? new List<string>() { "3" } : new List<string>() { "1" };
            List<string> uiRackHotels = new List<string>() { "Hotel 2", "Hotel 3" };
            List<string> uiRackShelves = batchZone  == 1 ? new List<string> { "4" } : new List<string> { "2" };
            foreach (DataRow row in table.Rows)
            {
                // Sample Rack
                if (sampleRackHotels.Contains(row[hotel].ToString()) && sampleRackShelves.Contains(row[shelf].ToString())  && !string.IsNullOrWhiteSpace(row[barcode] as string))
                {
                    sampleRacks.Add(row[barcode] as string);
                }
                // UI Rack
                else if (uiRackHotels.Contains(row[hotel].ToString()) && uiRackShelves.Contains(row[shelf].ToString()) && !string.IsNullOrWhiteSpace(row[barcode] as string))
                {
                    uiRacks.Add(row[barcode] as string);
                }
                else if (hplcPlateHotels.Contains(row[hotel].ToString()) && hplcPlateShelves.Contains(row[shelf].ToString()) && !string.IsNullOrWhiteSpace(row[barcode] as string))
                {
                    hplcPlate = row[barcode] as string;
                }
            }
            
            if (sampleRacks.Count() == 0)
            {
                errors.Add("No sample racks in batch.");
            }
            
            if (uiRacks.Count() < 2)
            {
                errors.Add("Need to fill both UI rack locations.");
            }
            
            if (string.IsNullOrWhiteSpace(hplcPlate))
            {
                errors.Add("HPLC Plate not supplied.");
            }
            
            return errors.Count() == 0;
        }
        
        private OrderTemplate GetTemplate(string templateName)
            => Environment.OrderClient.GetOrderTemplates(int.MaxValue, 0).ToList().FirstOrDefault(t => t.Name == templateName);
        
        private void SetInputParameters(OrderTemplate template, Dictionary<string, object> parameters, List<string> sampleRacks, List<string> uiRacks, string hplcPlate)
        {
            switch (template.Name)
            {
                case "Scan Labware":
                    template.InputParameters.SetValue("Scan Labware.Priority", parameters["Priority"] as string);
                    template.InputParameters.SetValue("Scan Labware.Sample Racks", string.Join(",", sampleRacks));
                    template.InputParameters.SetValue("Scan Labware.UI Racks", string.Join(",", uiRacks));
                    template.InputParameters.SetValue("Scan Labware.HPLC Plate", hplcPlate);
                	    break;
                	case "Unload Labware":
                	    template.InputParameters.SetValue("Unload Labware.Batch Number", int.Parse((parameters["Batch"] as string).Last().ToString()));
                	    break;
             }
        }
        
        private Order MakeOrderFromTemplate(OrderTemplate template)
        { 
            var inputParameters = new ParameterCollection();
            if (template.InputParameters != null)
            {
                inputParameters = template.InputParameters.Clone();
            }

            var outputParameters = new ParameterCollection();
            if (template.OutputParameters != null)
            {
                outputParameters = template.OutputParameters.Clone();
            }
            
            Order order = new Order()
            {
                CreatedBy = "Lab Experience",
                InputParameters = inputParameters,
                OutputParameters = outputParameters,
                RestrictToModuleIds = template.RequiredCapabilities,
                TemplateName = template.Name
            };

            return order;
        }
        
        private void SubmitOrder(Order order)
        {
            File.AppendAllLines(@"C:\Biosero\Lab Experience\CreateOrderLog.txt", new string[] { DateTime.Now.ToString() + ": " + order.TemplateName });
            Environment.OrderClient.CreateOrder(order);
        }
    }
}