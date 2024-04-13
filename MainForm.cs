using iRacingSdkWrapper;
using iSimpleRadar.Entities;
using iSimpleRadar.Overlay;
using NHotkey;
using NHotkey.WindowsForms;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;

namespace iSimpleRadar
{
    public partial class MainForm : Form
    {
        private SdkWrapper iracingWrapper;
        private iSimpleRadarOverlayWindow overlay;
        private List<Driver> drivers;
        private bool isUpdatingDrivers;

        private int currentSessionNum;
        private float carSize = 5f;
        private float trackLen;
        public MainForm()
        {
            InitializeComponent();
            // HotkeyManager.Current.AddOrReplace("Start", Keys.Alt| Keys.R, OnHotPress);
            // Create a new instance of the SdkWrapper object
            iracingWrapper = new SdkWrapper();

            // Set some properties
            iracingWrapper.EventRaiseType = SdkWrapper.EventRaiseTypes.CurrentThread;
            iracingWrapper.TelemetryUpdateFrequency = 10;

            // Listen for various events
            iracingWrapper.Connected += iracingWrapper_Connected;
            iracingWrapper.Disconnected += iracingWrapper_Disconnected;
            iracingWrapper.SessionInfoUpdated += iracingWrapper_SessionInfoUpdated;
            iracingWrapper.TelemetryUpdated += iracingWrapper_TelemetryUpdated;

            drivers = new List<Driver>();
            overlay = new iSimpleRadarOverlayWindow();
        }
        #region Connecting, disconnecting, etc

        private void btn_start_Click(object sender, EventArgs e)
        {
            // If the wrapper is running, stop it. Otherwise, start it.
            if (iracingWrapper.IsRunning)
            {
                iracingWrapper.Stop();
                overlay.Stop();
                btn_start.Text = "Start";
            }
            else
            {
                iracingWrapper.Start();
                overlay.Run();
                btn_start.Text = "Stop";
            }
            this.StatusChanged();
        }

        private void StatusChanged()
        {
            if (iracingWrapper.IsConnected)
            {
                if (iracingWrapper.IsRunning)
                {
                    statusLabel.Text = "Status: connected!";
                }
                else
                {
                    statusLabel.Text = "Status: disconnected.";
                }
            }
            else
            {
                if (iracingWrapper.IsRunning)
                {
                    statusLabel.Text = "Status: disconnected, waiting for sim...";
                }
                else
                {
                    statusLabel.Text = "Status: disconnected";
                }
            }
        }

        private void iracingWrapper_Connected(object sender, EventArgs e)
        {
            this.StatusChanged();
        }

        private void iracingWrapper_Disconnected(object sender, EventArgs e)
        {
            this.StatusChanged();
        }

        #endregion

        #region events
        private void iracingWrapper_SessionInfoUpdated(object sender, SdkWrapper.SessionInfoUpdatedEventArgs e)
        {
            // Indicate that we are updating the drivers list
            isUpdatingDrivers = true;
            trackLen = float.Parse(e.SessionInfo["WeekendInfo"]["TrackLength"].GetValue("0").Replace(" km","")) * 1000f;
            // Parse the Drivers section of the session info into a list of drivers
            this.ParseDrivers(e.SessionInfo);
            // Indicate we are finished updating drivers
            isUpdatingDrivers = false;
        }
        private void iracingWrapper_TelemetryUpdated(object sender, SdkWrapper.TelemetryUpdatedEventArgs e)
        {
            // Besides the driver details found in the session info, there's also things in the telemetry
            // that are properties of a driver, such as their lap, lap distance, track surface, distance relative
            // to yourself and more.
            // We update the existing list of drivers with the telemetry values here.

            // If we are currently renewing the drivers list it makes little sense to update the existing drivers
            // because they will change anyway
            if (isUpdatingDrivers) return;

            // Store the current session number so we know which session to read 
            // There can be multiple sessions in a server (practice, Q, race, or warmup, race, etc).
            currentSessionNum = e.TelemetryInfo.SessionNum.Value;

            this.UpdateDriversTelemetry(e.TelemetryInfo);
        }

        #endregion
        #region Drivers

        // Parse the YAML DriverInfo section that contains information such as driver id, name, license, car number, etc.
        private void ParseDrivers(SessionInfo sessionInfo)
        {
            int id = 0;
            Driver? driver;

            var newDrivers = new List<Driver>();

            // Loop through drivers until none are found anymore
            do
            {
                driver = null;


                // Construct a yaml query that finds each driver and his info in the Session Info yaml string
                // This query can be re-used for every property for one driver (with the specified id)
                YamlQuery query = sessionInfo["DriverInfo"]["Drivers"]["CarIdx", id];



                // Try to get the UserName of the driver (because its the first value given)
                // If the UserName value is not found (name == null) then we found all drivers and we can stop
                string name = query["UserName"].GetValue();
                if (name != null)
                {
                    // Find this driver in the list
                    // This strange " => " syntax is called a lambda expression and is short for a loop through all drivers
                    // Read as: select the first driver 'd', if any, whose Name is equal to name.
                    driver = drivers.Find(d => d.Name == name);

                    if (driver == null)
                    {
                        // Or create a new Driver if we didn't find him before
                        driver = new Driver();
                        driver.Id = id;
                        driver.Name = name;
                        // driver.CustomerId = int.Parse(query["UserID"].GetValue("0")); // default value 0
                        driver.Number = query["CarNumber"].GetValue("").TrimStart('\"').TrimEnd('\"'); // trim the quotes
                        // driver.ClassId = int.Parse(query["CarClassID"].GetValue("0"));
                        // driver.CarPath = query["CarPath"].GetValue();
                        // driver.CarClassRelSpeed = int.Parse(query["CarClassRelSpeed"].GetValue("0"));
                        // driver.Rating = int.Parse(query["IRating"].GetValue("0"));
                    }
                    newDrivers.Add(driver);

                    id++;
                }
            } while (driver != null);

            // Replace old list of drivers with new list of drivers and update the grid
            drivers.Clear();
            drivers.AddRange(newDrivers);
        }
        private void UpdateDriversTelemetry(TelemetryInfo info)
        {


            // Get your own driver entry
            // This strange " => " syntax is called a lambda expression and is short for a loop through all drivers
            Driver? me = drivers.Find(d => d.Id == iracingWrapper.DriverId);

            // Get arrays of the laps, distances, track surfaces of every driver
            var laps = info.CarIdxLap.Value;
            var lapDistances = info.CarIdxLapDistPct.Value;
            var trackSurfaces = info.CarIdxTrackSurface.Value;

            bool closeCar = false;
            bool closeCarDanger = false;
            
            // Loop through the list of current drivers
            foreach (Driver driver in drivers)
            {
                // Set the lap, distance, tracksurface belonging to this driver
                driver.Lap = laps[driver.Id];
                driver.LapDistance = lapDistances[driver.Id];
                driver.TrackSurface = trackSurfaces[driver.Id];

                // If your own driver exists, use it to calculate the relative distance between you and the other driver
                if (me != null && driver.TrackSurface == TrackSurfaces.OnTrack)
                {

                    var relative = driver.LapDistance - me.LapDistance;

                    // If driver is more than half the track behind, subtract 100% track length
                    // and vice versa
                    if (relative > 0.5) relative -= 1;
                    else if (relative < -0.5) relative += 1;

                    driver.RelativeLapDistance = relative;
                    driver.DistFromMe = (trackLen * relative) + carSize;
                    if (driver.DistFromMe >= -20f && driver.DistFromMe < -10f)
                    {
                        overlay.CarBehindWarn = "Car: " + driver.Number + "|Dist: " + Math.Round(driver.DistFromMe + 5f, 2);
                        //  overlay.text="Carro Aproximando: "+driver.Name+" - "+driver.DistFromMe+" meters"; 
                        overlay.posY = driver.DistFromMe;
                        closeCar = true;
                    }
                    else if (driver.DistFromMe >= -10f && driver.DistFromMe < 0f)
                    {
                        overlay.CarBehindDanger = "Car: " + driver.Number + "|Dist: " + Math.Round(driver.DistFromMe, 2);
                        //  overlay.text="Carro Aproximando: "+driver.Name+" - "+driver.DistFromMe+" meters"; 
                        closeCarDanger = true;
                        overlay.posY = driver.DistFromMe + 5f;
                    }
                    // else if ((Enums.CarLeftRight)iracingWrapper.GetData("CarLeftRight") == Enums.CarLeftRight.irsdk_LRClear)
                    //     overlay.textDebug="";
                    // else if ((Enums.CarLeftRight)iracingWrapper.GetData("CarLeftRight") == Enums.CarLeftRight.irsdk_LRCarLeft)
                    // {
                    //     if (driver.DistFromMe-carSize >= 0f && driver.DistFromMe-carSize <= carSize / 2)
                    //         overlay.textDebug = "Carro esquerda baixo";
                    //     else if (driver.DistFromMe-carSize > carSize / 2 && driver.DistFromMe-carSize <= carSize + 2f)
                    //         overlay.textDebug = "Carro esquerda alto";
                    // }
                }
                else
                {
                    driver.RelativeLapDistance = -1;
                }
            }
            //    drivers= drivers.OrderBy(x => x.RelativeLapDistance).ToList();
            if (!closeCar)
                overlay.CarBehindWarn = "";
            if (!closeCarDanger)
                overlay.CarBehindDanger = "";
            if ((Enums.CarLeftRight)iracingWrapper.GetData("CarLeftRight") == Enums.CarLeftRight.irsdk_LRClear)
                  overlay.textDebug="";
            else if ((Enums.CarLeftRight)iracingWrapper.GetData("CarLeftRight") == Enums.CarLeftRight.irsdk_LRCarLeft)
            {
                overlay.textDebug = "left";
                
            }
            else if ((Enums.CarLeftRight)iracingWrapper.GetData("CarLeftRight") == Enums.CarLeftRight.irsdk_LRCarRight)
            {
                overlay.textDebug = "right";
                
            }

        }

        #endregion

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            iracingWrapper.Stop();
        }



    }

}

