using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Android.Hardware;
using Android.Net.Wifi;
using Xamarin.Essentials;

namespace WheelSimu
{


    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true,
        ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape,
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize | Android.Content.PM.ConfigChanges.KeyboardHidden)]



    public class MainActivity : AppCompatActivity, ISensorEventListener
    {
        //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^全局参数声明^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        TextView textView1;
        TextView textView2;
        TextView textView3;
        TextView textView4;
        TextView textView5;
        TextView textView_Ver;
        //TextView textView6;

        EditText IPText;
        Button btnConnect;
        Button Throttle;
        Button Brake;
        Button btnSet;
        Button btnReset;
        Button btnSetSrd;
        Button btnSetSru;
        Button btnGearUp;
        Button btnGearDown;
        Button btnClearAngle;
#pragma warning disable CS0618 // Type or member is obsolete
        SlidingDrawer sldBrake;
        SlidingDrawer sldThrottle;
#pragma warning restore CS0618 // Type or member is obsolete
        LinearLayout cntBrake;
        LinearLayout cntThrottle;
        Switch SteerEnableSwitch;

        //ImageView iviewCoordinate;

        public Socket[] Sct = new Socket[2];
        public Thread[] Trd = new Thread[1];
        public struct IPFormat
        {
            public string IP;
            public int Port;
        };
        public IPFormat[] IPData = new IPFormat[2];
        public int TryTimes = 1;
        int sThrottle = 0;
        int sBrake = 0;
        double sAngle = 0;
        int sSet = 0;
        int sSetSR = 0;
        int sGear = 0;  //键盘控制
        int sGearUp = 0; //vJoy
        int sGearDn = 0; //vJoy
        //int sClearAngle = 0;  改成在手机端清零
        bool IsConnected = false;

        //Sensor
        readonly SensorSpeed Speed = SensorSpeed.UI;
        string AccelerometerData1;
        string AccelerometerData2;
        double AcX1, AcY1, AcZ1;
        double AcX2, AcY2, AcZ2;
        double TmpX = 0;
        double Hp = 0; //Hemisphere 方向盘大于+-90度的情况
        double OffSet = 0; //偏移补偿
        readonly double gAngle = 90 / 9.8; //一单位g值对应角度
        private SensorManager mSensorManager;
        //SensorMode = 0 Xamarin ; 1 android.Hardware ; 2,3 混合模式
        readonly int SensorMode = 1;

        //vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv全局参数声明vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);


            //保持屏幕常亮
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^控件实例化^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            // Get our UI controls from the loaded layout
            textView1 = FindViewById<TextView>(Resource.Id.textView1);
            textView2 = FindViewById<TextView>(Resource.Id.textView2);
            textView3 = FindViewById<TextView>(Resource.Id.textView3);
            textView4 = FindViewById<TextView>(Resource.Id.textView4);
            textView5 = FindViewById<TextView>(Resource.Id.textView5);
            textView_Ver = FindViewById<TextView>(Resource.Id.textView_Ver);
            //textView6 = FindViewById<TextView>(Resource.Id.textView6);
            IPText = FindViewById<EditText>(Resource.Id.IPText1);

            btnConnect = FindViewById<Button>(Resource.Id.Connect);
            Throttle = FindViewById<Button>(Resource.Id.ThrottleButton);
            Brake = FindViewById<Button>(Resource.Id.BrakeButton);
            btnSet = FindViewById<Button>(Resource.Id.btnSet);
            btnReset = FindViewById<Button>(Resource.Id.btnReset);
            btnSetSrd = FindViewById<Button>(Resource.Id.btnSetSrd);
            btnSetSru = FindViewById<Button>(Resource.Id.btnSetSru);


            btnGearUp = FindViewById<Button>(Resource.Id.btnGearUp);
            btnGearDown = FindViewById<Button>(Resource.Id.btnGearDown);
            btnClearAngle = FindViewById<Button>(Resource.Id.btnClearAngle);
            SteerEnableSwitch = FindViewById<Switch>(Resource.Id.SteerEnableSwitch);

#pragma warning disable  CS0618 // Type or member is obsolete
            sldBrake = FindViewById<SlidingDrawer>(Resource.Id.sldBrake);
            sldThrottle = FindViewById<SlidingDrawer>(Resource.Id.sldThrottle);
#pragma warning restore CS0618 // Type or member is obsolete
            cntBrake = FindViewById<LinearLayout>(Resource.Id.cntBrake);
            cntThrottle = FindViewById<LinearLayout>(Resource.Id.cntThrottle);

            RunOnUiThread(() => textView1.Text = "");
            RunOnUiThread(() => textView2.Text = "");
            //RunOnUiThread(() => textView3.Text = "");
            RunOnUiThread(() => textView4.Text = "");
            RunOnUiThread(() => textView5.Text = "");

            btnGearDown.Text = "<<<<<-DOWN-<<<<<";
            btnGearUp.Text = ">>>>>- UP ->>>>>";
            textView_Ver.Text = PackageManager.GetPackageInfo(this.PackageName, 0).VersionName;
            //vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv控件实例化vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv



            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^事件接口设置^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            btnConnect.Click += delegate
            {
                ThreadPool.QueueUserWorkItem(o => BtnConnect_OnClick());
            };

            btnClearAngle.Click += delegate
            {
                ThreadPool.QueueUserWorkItem(o => BtnClearAngle_OnClick());
            };

            SteerEnableSwitch.Click += delegate
            {
                ThreadPool.QueueUserWorkItem(o => SteerEnableSwitch_OnClick());
            };

            //Sensor
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;


            //vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv事件接口设置vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv



            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^其他事件委托^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

            //vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv其他事件委托vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv



        }


        //启动传感器
        private void StartSensor(SensorType EnableSensorType)
        {

            try
            {

                mSensorManager = (SensorManager)this.GetSystemService(SensorService);
                if (mSensorManager == null)
                {
                    RunOnUiThread(() => textView3.Text = "UnsupportedOperationException");
                }

                Sensor mSensor = mSensorManager.GetDefaultSensor(EnableSensorType);

                if (mSensor == null)
                {
                    RunOnUiThread(() => textView3.Text = "设备" + EnableSensorType + "不支持");
                }

                bool isRegister = mSensorManager.RegisterListener(this, mSensor, SensorDelay.Ui);
                if (!isRegister)
                {
                    RunOnUiThread(() => textView3.Text = "Listener开启失败");
                }

            }
            catch (Exception ex)
            {

                RunOnUiThread(() => textView2.Text = ex.Message);

            }


        }


        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {
            //RunOnUiThread(() => textView2.Text = "AccuracyChange=" + accuracy);
        }
        public void OnSensorChanged(SensorEvent e)
        {
            // Process Acceleration X, Y, and Z
            if (e.Sensor.StringType == Android.Hardware.Sensor.StringTypeAccelerometer || e.Sensor.StringType == Android.Hardware.Sensor.StringTypeGravity)
            {
                AcX1 = e.Values[0];
                AcY1 = e.Values[1];
                AcZ1 = e.Values[2];
                AccelerometerData1 = $" AcX: {AcX1.ToString("0.000")} \r\n AcY: {AcY1.ToString("0.000")} \r\n AcZ: {AcZ1.ToString("0.000")} ";
            }
            else if (e.Sensor.StringType == Android.Hardware.Sensor.StringTypeLinearAcceleration)
            {
                AcX2 = e.Values[0];
                AcY2 = e.Values[1];
                AcZ2 = e.Values[2];
                AccelerometerData2 = $" AcX: {AcX2.ToString("0.000")} \r\n AcY: {AcY2.ToString("0.000")} \r\n AcZ: {AcZ2.ToString("0.000")} ";
            }

            else
            {
                AccelerometerData2 = "UnDefined Type!";
            }
        }



        void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading;
            // Process Acceleration X, Y, and Z

            AcX1 = data.Acceleration.X;
            AcY1 = data.Acceleration.Y;
            AcZ1 = data.Acceleration.Z;
            /*Reading: \r\n */
            AccelerometerData1 = $" AcX: {AcX1.ToString("0.000")} \r\n AcY: {AcY1.ToString("0.000")} \r\n AcZ: {AcZ1.ToString("0.000")} ";

        }


        private void SteerEnableSwitch_OnClick()
        {
            string SendData;
            int result;
            byte[] bytes;

            try
            {

                if (SteerEnableSwitch.Checked)
                {
                    if (SensorMode == 0 || SensorMode == 2)   //Xamarin
                    {
                        if (Accelerometer.IsMonitoring == false)
                        {
                            Accelerometer.Start(Speed);
                        }
                    }

                    if (SensorMode == 1)   //Android.Hardware
                    {
                        StartSensor(Android.Hardware.SensorType.Gravity);
                    }
                    if (SensorMode == 3)   //Android.Hardware
                    {
                        StartSensor(Android.Hardware.SensorType.Accelerometer);
                        StartSensor(Android.Hardware.SensorType.LinearAcceleration);
                    }

                    ////^^^^^^^^^^^^^^^^^^^^DEBUG List^^^^^^^^^^^^^^^^^^^^^^^^^^
                    //// 获取传感器管理器
                    //SensorManager sensorManager = (SensorManager)GetSystemService(SensorService);

                    //// 获取全部传感器列表
                    //System.Collections.Generic.IList<Sensor> sensors = sensorManager.GetSensorList(SensorType.All);

                    //String strLog = "";
                    //int i = 0;
                    //foreach (Sensor item in sensors)
                    //{
                    //    strLog += (++i + "-" + item.Name + "\r\n");
                    //}
                    //RunOnUiThread(() => textView3.Text = strLog);
                    ////vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv   

                }
                else
                {
                    if (SensorMode == 0 || SensorMode == 2)   //Xamarin
                    {
                        if (Accelerometer.IsMonitoring == true)
                        {
                            Accelerometer.Stop();
                        }
                    }

                    if (SensorMode == 1 || SensorMode == 3)   //Android.Hardware
                    {
                        mSensorManager.UnregisterListener(this);
                    }
                }



                while (SteerEnableSwitch.Checked)
                {

                    RunOnUiThread(() => textView5.Text = AccelerometerData1);
                    RunOnUiThread(() => textView1.Text = AccelerometerData2);

                    //sThrottle = Throttle.Pressed ? 1 : 0;
                    //sBrake = Brake.Pressed ? 1 : 0;
                    sThrottle = 100 - Throttle.Top * 100 / cntThrottle.Height;
                    sBrake = 100 - Brake.Top * 100 / cntBrake.Height;
                    sAngle = GetWheelData();
                    //sClearAngle = btnClearAngle.Pressed ? 1 : 0;

                    if (btnSet.Pressed)
                    {
                        sSet = -1;
                    }
                    else if (btnReset.Pressed)
                    {
                        sSet = 1;
                    }
                    else
                    {
                        sSet = 0;
                    };

                    if (btnSetSrd.Pressed)
                    {
                        sSetSR = -1;
                    }
                    else if (btnSetSru.Pressed)
                    {
                        sSetSR = 1;
                    }
                    else
                    {
                        sSetSR = 0;
                    };


                    //键盘控制
                    if (btnGearUp.Pressed)
                    {
                        sGear = 1;
                    }
                    else if (btnGearDown.Pressed)
                    {
                        sGear = -1;
                    }
                    else
                    {
                        sGear = 0;
                    }

                    //vJoy
                    if (btnGearUp.Pressed)
                    {
                        sGearUp = 1;
                    }
                    else
                    {
                        sGearUp = 0;
                    }
                    if (btnGearDown.Pressed)
                    {
                        sGearDn = 1;
                    }
                    else
                    {
                        sGearDn = 0;
                    }

                    SendData = "A=" + sAngle +
                              ",T=" + sThrottle +
                              ",B=" + sBrake +
                              ",Gu=" + sGearUp +
                              ",Gd=" + sGearDn +
                              ",G=" + sGear +
                              ",S=" + sSet +
                              ",SR=" + sSetSR;
                    //",C=" + sClearAngle;

                    //if (sGear != 0) sGear = 0;
                    RunOnUiThread(() => textView4.Text = $" B={sBrake} T={sThrottle} A={sAngle.ToString("0.0")}");

                    if (IsConnected == true)
                    {
                        bytes = Encoding.Unicode.GetBytes(SendData + "0@");
                        result = Sct[1].Send(bytes);
                        //textView4.Text = result.ToString();
                    }

                    if (Throttle.Pressed == false) RunOnUiThread(() => sldThrottle.Close());
                    if (Brake.Pressed == false) RunOnUiThread(() => sldBrake.Close());

                    Thread.Sleep(2);
                }


            }

            catch (Exception ex)
            {
                RunOnUiThread(() => textView2.Text = ex.Message);
            }

        }

        private double GetWheelData()
        {
            double data, y;

            switch (SensorMode)
            {
                case 0:
                    {
                        //x = AcX1 * 100;
                        y = AcY1 * gAngle * 10;
                        break;
                    } // gY / 0.98 * 90;

                case 1:
                    {
                        //x = AcX1 * 10;
                        y = AcY1 * gAngle;
                        break;
                    }

                case 2:
                    {
                        //x = (AcX1 - AcX2) * 100;
                        y = (AcY1 - AcY2) * gAngle * 10;
                        break;
                    } //总加速度分量 - 运动加速度分量 = 重力加速度分量 

                case 3:
                    {
                        //x = (AcX1 - AcX2) * 10;
                        y = (AcY1 - AcY2) * gAngle;
                        break;
                    } //总加速度分量 - 运动加速度分量 = 重力加速度分量 

                default:
                    {
                        //x = AcX1 * 10;
                        y = AcY1 * gAngle;
                        break;
                    }
            }
            y -= OffSet * (90 - Math.Abs(y)) / 90;

            if (TmpX > 0 && AcX1 < 0) //朝向由上变为下
            {
                if (y < 0) //左转
                {
                    Hp -= 1;
                }
                else       //右转
                {
                    Hp += 1;
                }
            }
            else if (TmpX < 0 && AcX1 > 0) //朝向由下变为上
            {
                if (y < 0) //右转
                {
                    Hp += 1;
                }
                else       //左转
                {
                    Hp -= 1;
                }
            }

            //限制转向范围为900度
            //if (Hp > 2) Hp = 2;
            //if (Hp < -2) Hp = -2;

            //else if ((TmpX > 0 && AcX1 > 0) || (TmpX == 0 || AcX1 == 0) || (TmpX < 0 && AcX1 < 0)) //朝向未变
            //{
            //    //不变
            //}

            //AcX1 > 0 手机朝上 /  AcX1 < 0 手机朝下
            // -90 ~  90   = y                             Hp=0        手机朝上    
            //  90 ~ 270   = 90 + (90 - y) = 180 - y       Hp=1        手机朝下
            //-270 ~ -90   = -90 + (-90 - y) = -180 - y    Hp=-1       手机朝下
            // 270 ~ 450   = 360 + y                       Hp=2        手机朝上
            //-270 ~-450   = -360 + y                      Hp=-2       手机朝上
            data = 180 * Hp + y * (AcX1 / Math.Abs(AcX1));
            TmpX = AcX1;
            return data;
        }

        private void BtnConnect_OnClick()
        {
            try
            {

                if (btnConnect.Text == "连接")
                {
                    RunOnUiThread(() => textView2.Text = "Connecting...");
                    RunOnUiThread(() => btnConnect.Enabled = false);

                    IPAddress TmpIPAddress = new IPAddress(0);
                    WifiManager wifi = (WifiManager)GetSystemService(WifiService);
                    WifiInfo info = wifi.ConnectionInfo;

#pragma warning disable CS0618 // Type or member is obsolete
                    TmpIPAddress.Address = info.IpAddress;
#pragma warning restore CS0618 // Type or member is obsolete
                    IPData[1].IP = TmpIPAddress.ToString();

                    //}


                    //IPData[1].IP = Dns.GetHostAddresses(strHostName).GetValue(0).ToString();
                    //IPData[1].Port = Core.CommonCode.GetPort(IPData[1].IP, 1, TryTimes);
                    IPData[1].Port = Core.CommonCode.GetPort(TryTimes);
                    IPData[0].IP = IPText.Text;
                    IPData[0].Port = Core.CommonCode.GetPort(TryTimes);


                    RunOnUiThread(() => textView4.Text = "Connecting ...");
                    RunOnUiThread(() => textView5.Text = $"{AddressFamily.InterNetwork}, {SocketType.Stream},{ System.Net.Sockets.ProtocolType.Tcp}");

                    Sct[1] = new Socket(AddressFamily.InterNetwork, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp); //使用TCP协议

                    RunOnUiThread(() => textView4.Text = "Connecting ......");
                    IPEndPoint LocalEndPoint = new IPEndPoint(IPAddress.Parse(IPData[1].IP), IPData[1].Port); // 指定IP和Port (应该是本机IP)
                    IPEndPoint RemoteEndPoint = new IPEndPoint(IPAddress.Parse(IPData[0].IP), IPData[0].Port);

                    RunOnUiThread(() => textView4.Text = "Connecting .........");
                    Sct[1].Bind(LocalEndPoint);  // 绑定到该Socket

                    RunOnUiThread(() => textView4.Text = "Connecting ............");
                    Sct[1].Connect(RemoteEndPoint);

                    RunOnUiThread(() => textView3.Text = "Connected");
                    IsConnected = true;
                    RunOnUiThread(() => btnConnect.Text = "关闭连接");
                    RunOnUiThread(() => btnConnect.Enabled = true);
                }
                else if (btnConnect.Text == "关闭连接")
                {
                    RunOnUiThread(() => btnConnect.Enabled = false);

                    Sct[1].Close();

                    IsConnected = false;
                    RunOnUiThread(() => btnConnect.Text = "连接");
                    RunOnUiThread(() => btnConnect.Enabled = true);
                }



            }
            catch (Exception ex)
            {
                RunOnUiThread(() => textView2.Text = ex.Message);
                RunOnUiThread(() => textView3.Text =
                $"Local IP={IPData[1].IP}:{IPData[1].Port}\r\n" +
                $"RemoteIP={IPData[0].IP}:{IPData[0].Port}");
                RunOnUiThread(() => btnConnect.Enabled = true);
                TryTimes += 1;
            }
        }

        private void BtnClearAngle_OnClick()
        {
            try
            {
                switch (SensorMode)
                {
                    case 0: { OffSet = AcY1 * gAngle * 10; break; }

                    case 1: { OffSet = AcY1 * gAngle; break; }

                    case 2: { OffSet = (AcY1 - AcY2) * gAngle * 10; break; }

                    case 3: { OffSet = (AcY1 - AcY2) * gAngle; break; }

                    default: { OffSet = AcY1 * 10; break; }
                }

                Hp = 0;
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => textView2.Text = ex.Message);
            }
        }


    }




}




