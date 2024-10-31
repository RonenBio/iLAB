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
    public class Handle_Remove_Labware_Scan : WorkflowScript
    {
        private DataServicesHelper _dataservicesHelper;
    
	public Handle_Remove_Labware_Scan(WorkflowEnvironment environment) : base(environment) {}
	
	public void Run(Dictionary<string, object> parameters)
	{
	        _dataservicesHelper = new DataServicesHelper(Environment);
		string scan = parameters["Scan"] as string;
		Log(scan);
		if (string.IsNullOrWhiteSpace(scan))
		{
		    return;
		}
		else if (scan.Equals("Restart"))
		{
		    CloseScreen("RemoveLabware");
		    return;
		}
		
		Parameter scanParameter = Program.Parameters.Get("Scan");
		scanParameter.Value = string.Empty;
		
		Screen screen = GetScreen("RemoveLabware");
		Control imageBaseControl = screen.GetControl($"{scan}Image");
		imageBaseControl.IsVisible = false;
		TextBoxControl control = screen.GetControl($"{scan}TextBox") as TextBoxControl;
		control.RuntimeText = string.Empty;
		control.DefaultText = string.Empty;
		
		bool parsed = int.TryParse(scan.Split(new string[] { "Shelf" }, StringSplitOptions.None)[0].Where(x => char.IsDigit(x)).ToArray()[0].ToString(), out int hotelIndex); // Extract 1 from Hotel1Shelf4
		bool parsedShelf = int.TryParse(scan.Split(new string[] { "Shelf" }, StringSplitOptions.None)[1].Where(x => char.IsDigit(x)).ToArray()[0].ToString(), out int shelfIndex); // Extract 4 from Hotel1Shelf4
		Log(scan + hotelIndex.ToString() + parsedShelf.ToString());
		if (!parsed || !parsedShelf)
		{
			// TODO: Get the position of the barcode and set it to empty + hide image.
		}
		    
		
		PublishHotelChange(hotelIndex, shelfIndex, "");
	    	Control scanControl = screen.GetControl("ScanTextBox");
		scanControl.Focus();
		screen.FocusControl = "ScanTextBox";
	}
	
	private void PublishHotelChange(int hotelIndex, int shelfIndex, string barcodeAdded)
	{
		try
        		{
	        		string tableName = "Plate_Storage_Hotels";
			Identity tableIdentity = Environment.QueryClient.GetIdentity(tableName);
			
			DataTable table = JsonConvert.DeserializeObject<DataTable>(tableIdentity.Properties.GetValue<string>("Storage"));
			
			string hotel = "Hotel";
			string shelf = "Shelf";
			string barcode = "Barcode";
			foreach (DataRow row in table.Rows)
			{
				if ((int.Parse(row[shelf].ToString()) != shelfIndex) || (row[hotel].ToString().Replace(" ", "")) != $"Hotel{hotelIndex}")
				{
					continue;
				}
				
			    row[barcode] = barcodeAdded;
			}
			
			tableIdentity.Properties.SetValue("Storage", JsonConvert.SerializeObject(table));
			Environment.AccessioningClient.Register(tableIdentity, new EventContext() {  });
			try
			{
				_dataservicesHelper.SubmitOrderFromTemplate("Script.Get Plate Storage From DS");
			}
			catch (Exception e)
			{
				Log(JsonConvert.SerializeObject(e.Message).ToString());
				
			}
		}
		catch (Exception ex)
		{
			Log(JsonConvert.SerializeObject(ex.Message).ToString());
			File.AppendAllLines(@"C:\Biosero\Lab Experience\Error.txt", new string[] { JsonConvert.SerializeObject(ex.Message) });
		}
	}
	
	private void CloseScreen(string screenName)
	{
	    Screen screen = GetScreen(screenName);
	    screen.Close();
	}
    }
}