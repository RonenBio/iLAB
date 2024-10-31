reference Newtonsoft.Json.dll;
reference AZ.DataModels.dll;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Linq;
using AZ.DataModels.Helpers;
using Biosero.Workflow;
using Biosero.Workflow.ScreenControls;
using Biosero.DataModels.Resources;
using Biosero.DataModels.Parameters;
using Newtonsoft.Json;

namespace Biosero.Scripting
{
    public class Update_Storage_Content : WorkflowScript
    {

        public Update_Storage_Content(WorkflowEnvironment environment) : base(environment) { }
        
        private static bool BreakUpdateLoop(Screen screen)
        {
       	    if (screen == null)
       	    {
       	    	return false;
       	    }
            if (screen.FocusControl.Contains("Hotel") && screen.Name == "LoadNewJob")
            {
                return true;
            }
            return false;
        }
        private void ResetScreenImageUrls(Screen screen)
        {
            for (int hotelIndex = 1; hotelIndex <= 5; hotelIndex++)
            {
            	for (int shelfIndex = 1; shelfIndex <= 4; shelfIndex++)
            	{
            		string url = $"C:\\Biosero\\Lab Experience\\PreAnalysis_AddRemoveLabwareImages\\Hotel{hotelIndex}Shelf{shelfIndex}.png";
            		ImageControl imageControl = screen.GetControl($"Hotel{hotelIndex}Shelf{shelfIndex}Image") as ImageControl;
            		imageControl.Url = url;
            		imageControl.RuntimeUrl = url;
            	}
            }
        }
        public void Run(Dictionary<string, object> parameters)
        {
        	    Action<string> traceLog = (x) => Log(x);
            DataTable cacheTable = new DataTable();
            string screenToGet = parameters["Next Step"] as string;
            if (screenToGet == "EditExistingQue");
            {
            	screenToGet = "LoadNewJob";
            }
            Screen screen = GetScreen(screenToGet);
            if (screen.Name == "LoadNewJob")
            {
            	screen.FocusControl = "ScanTextBox";
            	ResetScreenImageUrls(screen);
            }
            do
            {
                if (BreakUpdateLoop(screen))
                {
                    System.Threading.Thread.Sleep(1000);
                    continue;
                }

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
			
                    hotel.UpdateTextBoxes();
                    
                    if (screen.Name == "LoadNewJob")
                    {
                    	Parameter zoneParameter = Program.Parameters.FirstOrDefault(p => p.Name == "Zone To Edit");
                    	Log($"Edited zone {zoneParameter.Value}");
                    	int editedZone = int.Parse(zoneParameter.Value.ToString());
                        hotel.DetermineVisibilityForPlateLoading(editedZone);
                        
                		List<Control> controls = screen.Controls.Where(x => x.Name.Contains("Priority")).ToList();
                		bool showAddBatchButtons = hotel.ZoneOneIsValidBatch || hotel.ZoneTwoIsValidBatch;
                		controls.ForEach(c => c.IsVisible = showAddBatchButtons);
                		controls.ForEach(c => c.IsVisibleWhenShown = showAddBatchButtons);
                    }
                    else if (screen.Name == "RemoveLabware")
                    {
                        hotel.HideEmptyShelves();
                        hotel.Shelves.Where(x => !String.IsNullOrEmpty(x.Value)).ToList().ForEach(x => x.Key.Show());
                    }
                }
                catch (Exception e)
                {
                    File.AppendAllLines(@"C:\Biosero\Lab Experience\Error.txt", new string[] { JsonConvert.SerializeObject(e.Message) });
                    Log(JsonConvert.SerializeObject(e.Message) + e.ToString());
                    break;
                }
                System.Threading.Thread.Sleep(1000);
            } while (screen.IsOpen);
        }
    }
}