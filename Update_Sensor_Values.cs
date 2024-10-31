reference Newtonsoft.Json.dll;
reference AZ.DataModels.dll;

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using AZ.DataModels.Workcell;
using AZ.DataModels.Workcell.Sensors;
using Biosero.Workflow;
using Biosero.Workflow.ScreenControls;
using Biosero.DataModels.Events;
using Biosero.DataModels.Resources;
using Biosero.DataModels.Parameters;
using Newtonsoft.Json;
using Biosero.Scripting;

namespace Biosero.Scripting
{
	public class Update_Sensor_Values : WorkflowScript
	{
		public Update_Sensor_Values(WorkflowEnvironment environment) : base(environment)
		{
		}

		public void Run(Dictionary<string, object> parameters)
		{
			Module preAnalysisModule = Module.GetPreAnalysisModule(Environment.QueryClient);
			string screenToGet = parameters["Next Step"].ToString() == "Bottles" ? "Bottle Level Status" : "Startup";
			int blinkingLight = 0;
			if (String.IsNullOrEmpty(parameters["Next Step"] as string) || parameters["Next Step"].ToString() == "Bottles")
			{
				Screen screen = GetScreen(screenToGet);
				Control systemStatusLight = screen.Controls.FirstOrDefault(c => c.Name == "Sensor.SystemStatus");
				Parameter statusTextParameters = Program.Parameters.FirstOrDefault(p => p.Name == "Sensor.SystemStatus.Text");
				do
				{
					UpdateHplcStatusLights(ref screen, preAnalysisModule);
					UpdateSensorStatusLights(ref screen, preAnalysisModule);
					
					if (systemStatusLight != null)
					{
						ModuleHealth statusToSet = preAnalysisModule.Status;
						if (preAnalysisModule.Sensors.Any(s => s.GetStatus() == ModuleHealth.Unknown))
						{
							Log("Some sensor unresponsive");
							statusTextParameters.Value = $"Warning                                                                      One or more sensors are unresponsive - {String.Join(", ", preAnalysisModule.Sensors.Where(x => x.GetStatus() == ModuleHealth.Unknown).Select(s => s.Name.Replace("PreAnalysis.", "")).ToList())}";
							statusToSet = ModuleHealth.Warning;
						}
						else
						{
							statusTextParameters.Value = preAnalysisModule.Status.ToString();
							
						}
						SetControlToHealthStatus(systemStatusLight, statusToSet);
						if (statusToSet >= ModuleHealth.Warning)
						{
							if (blinkingLight == 0)
							{
								systemStatusLight.IsVisible = true;
								blinkingLight++;
							}
							else if (blinkingLight == 1)
							{
								systemStatusLight.IsVisible = false;
								blinkingLight = 0;
							}
						}
					}
					System.Threading.Thread.Sleep(250);
				} while (screen.IsOpen);
			}
		}

		private void UpdateSensorStatusLights(ref Screen screen, Module workcell)
		{
			if (screen.Name != "Startup")
			{
				return;
			}
			foreach (ISensor adamSensor in workcell.Sensors.Where(x => x is DigitalAdamSensor))
			{
				UpdateScreenForSensor(ref screen, adamSensor);
			}
			foreach (ISensor balanceSensor in workcell.Sensors.Where(x => x is DmsoLevelSensor))
			{
				UpdateScreenForBalanceSensor(ref screen, balanceSensor);
			}
		}
		private void UpdateScreenForBalanceSensor(ref Screen screen, ISensor balance)
		{
			string shapeName = balance.Name.Replace("PreAnalysis.", "") + ".Shape";
			Control control = screen.Controls.FirstOrDefault(c => c.Name == shapeName);
			if (control != null)
			{
				Log($"{balance.Name} - {balance.GetStatus().ToString()} - {(balance as DmsoLevelSensor).Status}");
				SetControlToHealthStatus(control, balance.GetStatus());
			}
		}
		
		private void UpdateScreenForSensor(ref Screen screen, ISensor adamSensor)
		{
			string shapeName = adamSensor.Name.Replace("PreAnalysis.", "") + ".Shape";
			Control control = screen.Controls.FirstOrDefault(c => c.Name == shapeName);
			Log("Looking for shape name: " + shapeName);
			if (control != null)
			{
				Log($"{adamSensor.Name} - {adamSensor.GetStatus().ToString()} - {(adamSensor as DigitalAdamSensor).Status}");
				SetControlToHealthStatus(control, adamSensor.GetStatus());
			}
		}
		
		private void UpdateHplcStatusLights(ref Screen screen, Module workcell)
		{
			for (int hplcIndex = 1; hplcIndex <= 3; hplcIndex++)
			{
				Control control = screen.Controls.FirstOrDefault(c => c.Name == $"HPLC{hplcIndex}Shape");
				if (control != null)
				{
					try
					{
						IEnumerable<BottleLevelSensor> sensorsForHplc = workcell.Sensors.Where(x => x is BottleLevelSensor).Cast<BottleLevelSensor>().Where(x => x.HplcIndex == hplcIndex);
						Log(String.Join(", ", sensorsForHplc.Select(x => $"{x.Name} - {x.GetStatus().ToString()}")));
						ModuleHealth hplcStatus;
						
						if (sensorsForHplc.Any(s => s.GetStatus() >= ModuleHealth.Critical))
						{
							hplcStatus = ModuleHealth.Error;
						}
						else if (sensorsForHplc.Any(s => s.GetStatus() >= ModuleHealth.Warning))
						{
							hplcStatus = ModuleHealth.Warning;
						}
						else if (sensorsForHplc.All(s => s.GetStatus() == ModuleHealth.Healthy))
						{
							hplcStatus = ModuleHealth.Healthy;
						}
						else
						{
							hplcStatus = ModuleHealth.Unknown;
						}
						SetControlToHealthStatus(control, hplcStatus);
					}
					catch (Exception e)
					{
						Log(e.ToString());
						SetControlToHealthStatus(control, ModuleHealth.Unknown);
					}
				}
				for (int bottle = 1; bottle <= 7; bottle++)
				{
					string sensorName = $"HPLC{hplcIndex}Bottle{bottle}";
					string controlName = $"{sensorName}Shape";
					Control individualBottleControl = screen.Controls.FirstOrDefault(c => c.Name == controlName);
					if (individualBottleControl != null)
					{
						string colorToAssign = "Gray";
						BottleLevelSensor sensor;
						try
						{
							sensor = workcell.Sensors.FirstOrDefault(x => x.Name == sensorName) as BottleLevelSensor;
							ModuleHealth status = sensor.GetStatus();
							SetControlToHealthStatus(individualBottleControl, status);
						}
						catch (Exception e)
						{
							Log(e.Message);
							SetControlToHealthStatus(individualBottleControl, ModuleHealth.Unknown);
						}
					}
				}
			}
		}
		private static void SetControlToHealthStatus(Control control, ModuleHealth health)
		{
			switch (health)
			{
				case ModuleHealth.Error:
					control.BackgroundColor = "Red";
					control.LineColor = "Red";
					break;
				case ModuleHealth.Critical:
					control.BackgroundColor = "Red";
					control.LineColor = "Red";
					break;
				case ModuleHealth.Warning:
					control.BackgroundColor = "Yellow";
					control.LineColor = "Yellow";
					break;
				case ModuleHealth.Healthy:
					control.BackgroundColor = "Green";
					control.LineColor = "Green";
					break;
				case ModuleHealth.Unknown:
					control.BackgroundColor = "Gray";
					control.LineColor = "Gray";
					break;
			};
		}
	}
}