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
    public class Handle_Add_Labware_Identity_Scan : WorkflowScript
    {
    	private DataServicesHelper _dataservicesHelper;
    
        public Handle_Add_Labware_Identity_Scan(WorkflowEnvironment environment) : base(environment) { }

        public void Run(Dictionary<string, object> parameters)
        {
	    Action<string> traceLog = (x) => Log(x);
            _dataservicesHelper = new DataServicesHelper(Environment);
            string scan = parameters["Scan"] as string;
            Log(scan);
            if (string.IsNullOrWhiteSpace(scan))
            {
                return;
            }
            else if (scan.Equals("Restart"))
            {
                CloseScreen("LoadNewJob");
                return;
            }

            Parameter scanParameter = Program.Parameters.Get("Scan");
            scanParameter.Value = string.Empty;

            bool parsed = int.TryParse(scan.Split(new string[] { "Shelf" }, StringSplitOptions.None)[0].Where(x => char.IsDigit(x)).ToArray()[0].ToString(), out int hotelIndex); // Extract 1 from Hotel1Shelf4
            bool parsedShelf = int.TryParse(scan.Split(new string[] { "Shelf" }, StringSplitOptions.None)[1].Where(x => char.IsDigit(x)).ToArray()[0].ToString(), out int shelfIndex); // Extract 4 from Hotel1Shelf4

            if (!parsed || !parsedShelf)
                return;

            string baseIdentifier = scan;
            string imageIdentifier = $"{scan}Image";
            string textBoxIdentifier = $"{scan}TextBox";

            Screen screen = GetScreen("LoadNewJob");
            ImageControl imageBaseControl = screen.Controls.FirstOrDefault(c => c.Name == imageIdentifier) as ImageControl;
            TextBoxControl imageTextControl = screen.Controls.FirstOrDefault(c => c.Name == textBoxIdentifier) as TextBoxControl;
            
	    string barcodeScanned = imageTextControl.RuntimeText;
            imageTextControl.DefaultText = barcodeScanned;
            Log($"Publishing scanned plate at hotel {hotelIndex} in shelf {shelfIndex} with barcode: {barcodeScanned}");
            try
            {
            	PreAnalysisExternalHotel.PublishHotelChange(Environment, hotelIndex, shelfIndex, barcodeScanned);
            }
            catch (Exception e)
            {
            	Log(e.ToString());
            }
            
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
            
            Parameter zoneParameter = Program.Parameters.Get("Zone To Edit");
            if (zoneParameter.Value.ToString() == "0" || String.IsNullOrEmpty(zoneParameter.Value.ToString()))
            {
            	zoneParameter.Value = shelfIndex == 1 || shelfIndex == 2 ? "2" : "1";
            }
            Control control = screen.GetControl("ScanTextBox");
		control.Focus();
		screen.FocusControl = "ScanTextBox";
        }

        private void CloseScreen(string screenName)
        {
            Screen screen = GetScreen(screenName);
            screen.Close();
        }
    }
}