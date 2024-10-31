reference Newtonsoft.Json.dll
reference AZ.DataModels.dll
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Linq;
using AZ.DataModels.Helpers;
using Biosero.Workflow;
using Biosero.Workflow.ScreenControls;
using Biosero.DataModels.Events;
using Biosero.DataModels.Ordering;
using Biosero.DataModels.Parameters;
using Biosero.DataModels.Resources;
using Newtonsoft.Json;

namespace Biosero.Scripting
{
    public class Handle_Add_Labware_Scan : WorkflowScript
    {
        public Handle_Add_Labware_Scan(WorkflowEnvironment environment) : base(environment) {}

        public void Run(Dictionary<string, object> parameters)
        {
        		Action<string> traceLog = (x) => Log(x);
        		Screen screen = GetScreen("LoadNewJob");
		Parameter scanParameter = Program.Parameters.Get("Scan");
		string scan = scanParameter.Value.ToString();
		if (string.IsNullOrWhiteSpace(scan))
		{
			return;
		}
		else if (scan.Equals("Restart"))
		{
			scanParameter.Value = string.Empty;
			CloseScreen("LoadNewJob");
			return;
		}
		else if (scan.Equals("Home"))
		{
			scanParameter.Value = string.Empty;
			screen.FocusControl = "";
			CloseScreen("LoadNewJob");
			Parameter nextStepParameter = Program.Parameters.Get("Next Step");
            		nextStepParameter.Value = scan;
			return;
		}
		
		
		if (scan.Equals("Yes"))
		{
			Parameter batchCompleteParameter = Program.Parameters.Get("Batch Complete");
			batchCompleteParameter.Value = "True";
			CloseScreen("LoadNewJob");
		}
		else if (scan.Contains("Hotel"))
		{
			Log("Change control to " + scan +"TextBox");
			Control control = screen.GetControl($"{scan}TextBox");
			control.Focus();
			screen.FocusControl = $"{scan}TextBox";
			
			    bool parsed = int.TryParse(scan.Split(new string[] { "Shelf" }, StringSplitOptions.None)[0].Where(x => char.IsDigit(x)).ToArray()[0].ToString(), out int hotelIndex); // Extract 1 from Hotel1Shelf4
		            bool parsedShelf = int.TryParse(scan.Split(new string[] { "Shelf" }, StringSplitOptions.None)[1].Where(x => char.IsDigit(x)).ToArray()[0].ToString(), out int shelfIndex); // Extract 4 from Hotel1Shelf4
		
		            if (!parsed || !parsedShelf)
		                return;
		            
		            string imageIdentifier = $"{scan}Image";
		            ImageControl imageBaseControl = screen.Controls.FirstOrDefault(c => c.Name == imageIdentifier) as ImageControl;
	                	    PreAnalysisExternalHotelShelf shelf = new PreAnalysisExternalHotelShelf(hotelIndex, shelfIndex, ref screen, ref traceLog);
		            string imageToShow = @"C:\Biosero\Lab Experience\PreAnalysis_AddRemoveLabwareImages\";
		            if (shelf.IsHplcShelf)
		            {
		            	imageToShow += "HPLCPlateLabware.png";
		            }
		            else if (shelf.IsUiRackShelf)
		            {
		            	imageToShow += "UiRackImage.png";
		            }
		            else if (shelf.IsSampleRackShelf)
		            {
		            	imageToShow += "SampleRackImage.png";
		            }
		            
		            if (imageToShow.Contains("png"))
		            {
		            	imageBaseControl.Url = imageToShow;
		            	imageBaseControl.RuntimeUrl = imageToShow;
		            	imageBaseControl.IsVisible = true;
		            	imageBaseControl.IsVisibleWhenShown = true;
		            }
			
		}
		else if (scan.Contains("Priority"))
		{
			Parameter nextStepParameter = Program.Parameters.Get("Next Step");
			nextStepParameter.Value = "CreateOrder";
        			Parameter priorityParameter = Program.Parameters.Get("Priority");
			if (scan.Contains("High"))
			{
				priorityParameter.Value = "High";
			}
			else if (scan.Contains("Low"))
			{
				priorityParameter.Value = "Low";
			}
			int zoneToCreateBatchFrom = int.Parse(parameters["Zone To Edit"].ToString());
			if (zoneToCreateBatchFrom != 1 || zoneToCreateBatchFrom != 2)
			{
			    try
			    {
				    DataTable table = null;
		        		    try 
		        		    {
		        		    	Identity tableIdentity = Environment.QueryClient.GetIdentity("Plate_Storage_Hotels");
		
		                    	table = JsonConvert.DeserializeObject<DataTable>(tableIdentity.Properties.GetValue<string>("Storage"));
		        		    }
		        		    catch (Exception e)
		        		    {
		        		    	throw new Exception(e.ToString());
		        		    }
		                    if (table == null || table.Rows.Count == 0)
		                    	throw new Exception("Could not get hotel");
		                    
		                    PreAnalysisExternalHotel hotel = PreAnalysisExternalHotel.GetHotelFromDataTable(table, ref traceLog, ref screen);
		                    if (hotel.ZoneOneIsValidBatch && hotel.ZoneTwoIsValidBatch)
		                    {
		                    	parameters["Zone To Edit"] = 0;
		                    }
		                    else if (hotel.ZoneOneIsValidBatch && !hotel.ZoneTwoIsValidBatch)
		                    {
		                    	parameters["Zone To Edit"] = 1;
		                    }
		                    else if (!hotel.ZoneOneIsValidBatch && hotel.ZoneTwoIsValidBatch)
		                    {
		                    	parameters["Zone To Edit"] = 2;
		                    }
		                    else
		                    {
		                    	nextStepParameter.Value = "";
		                    }
	                    }
	                    catch (Exception ex)
	                    {
	                    	Log(ex.ToString());
	                    }
			}
			scanParameter.Value = string.Empty;
			CloseScreen("LoadNewJob");
		}
        }
        
        private void CloseScreen(string screenName)
        {
            Screen screen = GetScreen(screenName);
            screen.Close();
        }
    }
}