using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WII.HID.Lib;

namespace projectWII
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        HIDDevice _device = HIDDevice.GetHIDDevice(0x57E, 0x306);

        int verschil = 500;

        int resX = 1600;
        int resY = 900;



        public Boolean isWiiMoteAppWorking = false;
        public Boolean isMouseCursorOnScreen = false;
        
        #region muis bewegen functie
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        //This simulates a left mouse click
        public void LeftMouseDown()
        {
            mouse_event(0x02, xPos, yPos, 0, 0);
        }
        public void LeftMouseUp()
        {
            mouse_event(0x04, xPos, yPos, 0, 0);
        }
        public void RightMouseDown()
        {
            mouse_event(0x08, xPos, yPos, 0, 0);
        }
        public void RightMouseUp()
        {
            mouse_event(0x16, xPos, yPos, 0, 0);
        }



        int xPos;
        int yPos;
        int i = 0;




        
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            try
           {
               init();
           }
            catch (Exception ex)
            {
                MessageBox.Show("Geen WiiMote Device gevonden, gelieve een device aan te sluiten." 
                    + System.Environment.NewLine + System.Environment.NewLine 
                    + "Error:" 
                    + System.Environment.NewLine + " << " + ex.Message + " >>",
                    "Er is een fout opgetreden bij het opstarten van ...", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                System.Windows.Forms.Application.Exit();
            }
        }

        private void init()
        {
            resX += 2 * verschil;
            resY += 2 * verschil;

            // led 1 aanzetten (stoppen met knipperen)
            HIDReport report = _device.CreateReport();
            report.ReportID = 0x11;
            report.Data[0] = (byte)0x10;
            _device.WriteReport(report);

            //in test programma aanduiden welke led(s) aanliggen
            i = 16;
            checkLeds(i);


            EnableIRCamera();
            EnableIRCamera();
            EnableIRCamera();
            //verslag 37 vragen (om IR camera en buttons te lezen)
            HIDReport rep = _device.CreateReport();
            rep.ReportID = 0x12;
            rep.Data[0] = 0x04;
            rep.Data[1] = 0x37;
            _device.WriteReport(rep);

            _device.ReadReport(OnReadReport);

        }
        private void EnableIRCamera()
        {
            HIDReport report = _device.CreateReport();

            //Enable IR Camera
            report.ReportID = 0x13;
            report.Data[0] = (byte)0x04;
            _device.WriteReport(report);

            //Enable IR Camera 2
            report.ReportID = 0x1A;
            report.Data[0] = (byte)0x04;
            _device.WriteReport(report);

            //Write 0x08 to register 0xb00030
            byte[] arr = { 0x08 };
            WriteData(0xB00030, arr);


            //Write Sensitivity Block 1 to registers at 0xb00000
            byte[] arr2 = { 0x02,0x00,0x00,0x71,0x01,0x00,0x90,0x00,0x41 }; //<== pdf
           // byte[] arr2 = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x0C }; // <== Kestrel
            //byte[] arr2 = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x90, 0x00, 0xC0 }; // <== Marcan
            WriteData(0xB00000, arr2);

            //Write Sensitivity Block 2 to registers at 0xb0001a
            byte[] arr3 = { 0x40, 0x00 };
            //byte[] arr3 = { 0x00, 0x00 };
            WriteData(0xB0001A, arr3);

            //Write Mode Number to register 0xb00033
            byte[] arr4 = { 0x01 };
            WriteData(0xB00033, arr4);

            //Write 0x08 to register 0xb00030 (again)
            WriteData(0xB00030, arr);

        }

        //Reports inlezen
        private void OnReadReport(HIDReport report)
        {

            Console.WriteLine(report.ReportID);
            if (Thread.CurrentThread != Dispatcher.Thread) 
            {
                this.Dispatcher.Invoke(new ReadReportCallback(OnReadReport), report);
            }
            else {
                switch (report.ReportID)
                {
                    case 0x20:
                        // Status info 
                        Console.WriteLine(report.Data[2]);

                        //Batterijlevel aanduiden
                        decimal d = (decimal)report.Data[5];
                        d /= 255;
                        d *= 90;
                        d = Math.Round((decimal)d, 0);
                        rectBatteryLevel.Width = (double)d;

                        if (d < 15) rectBatteryLevel.Fill = Brushes.DarkSalmon;
                        else rectBatteryLevel.Fill = Brushes.GreenYellow;

                        lblBattery.Content = d + "%";
                        break;
                    case 0x37:

                        //IR Camera + Accelerometer
                        verwerkDataIR(report.Data);

                        //buttons
                        zetOnzichtbaar();
                        maakZichtbaar(report.Data);

                        
                        HIDReport rep2 = _device.CreateReport();
                        rep2.ReportID = 0x15;
                        rep2.Data[0] = (Byte)i;
                        rep2.Data[1] = 0x20;
                        _device.WriteReport(rep2);
                        break;
                }

                //if(it == 0)
                //{ 
                //HIDReport rep2 = _device.CreateReport();
                //rep2.ReportID = 0x15;
                //rep2.Data[0] = (Byte)i;
                //rep2.Data[1] = 0x20;
                //_device.WriteReport(rep2);
                //    it=1;
                //}
                //else {
                //    HIDReport rep = _device.CreateReport();
                //    rep.ReportID = 0x12;
                //    rep.Data[0] = 0x04;
                //    rep.Data[1] = 0x37;
                //    _device.WriteReport(rep);
                //it=0;
                //}
                
                //callback methode
                _device.ReadReport(OnReadReport);
           }
        }
        //int it = 0;

        #region Hulpmethodes voor reports inlezen
        //Rechthoeken onzichtbaar maken
        private void zetOnzichtbaar()
        {
            foreach (Rectangle rect in gridFront.Children)
            {
                if(!rect.Name.ToString().Contains("rectLed")) rect.Visibility = Visibility.Hidden;
            }
        }

        //Rechthoeken tonen + checkboxen aanvinken als nodig + batterij level aanduiden +IRData verwerken
        int tellerA = 0;
        int tellerB = 0;
        int tellerA2 = 0;
        int tellerB2 = 0;
        int tellerA3 = 0;
        int tellerB3 = 0;
        private void maakZichtbaar(Byte[] data)
        {
            List<int> test = controleerButtons(data[0]);
            for (int i = 0; i < test.Count; i++)
            {
                switch (test[i])
                {
                    case 1:
                        rectLeft.Visibility = Visibility.Visible;
                        if (isWiiMoteAppWorking)
                        {
                            System.Windows.Forms.SendKeys.SendWait("{LEFT}");
                            Thread.Sleep(400);
                            
                        }
                        break;
                    case 2:
                        rectRight.Visibility = Visibility.Visible;
                        if (isWiiMoteAppWorking)
                        {
                            System.Windows.Forms.SendKeys.SendWait("{RIGHT}");
                            Thread.Sleep(400);
                        }
                        break;
                    case 4:
                        rectDown.Visibility = Visibility.Visible;
                        if (isWiiMoteAppWorking)
                        {
                            System.Windows.Forms.SendKeys.SendWait("{DOWN}");
                            Thread.Sleep(400);
                        }
                        break;
                    case 8:
                        rectUp.Visibility = Visibility.Visible;
                        if (isWiiMoteAppWorking)
                        {
                            System.Windows.Forms.SendKeys.SendWait("{UP}");
                            Thread.Sleep(400);
                        }
                        break;
                    case 16:
                        rectPlus.Visibility = Visibility.Visible;
                        if (isWiiMoteAppWorking)
                        {
                            System.Windows.Forms.SendKeys.SendWait("^({+})");
                            Thread.Sleep(400);
                        }
                       break;
                }

            }


            tellerA = 0;
            tellerB = 0;
            test = controleerButtons(data[1]);
            for (int i = 1; i < test.Count; i++)
            {
                switch (test[i])
                {
                    case 1:
                        rectTwo.Visibility = Visibility.Visible;
                        if (isWiiMoteAppWorking)
                        {
                            System.Windows.Forms.SendKeys.SendWait("{PGDN}");
                            Thread.Sleep(400);
                        }
                        break;
                    case 2:
                        rectOne.Visibility = Visibility.Visible;
                        if (isWiiMoteAppWorking)
                        {
                            System.Windows.Forms.SendKeys.SendWait("{PGUP}");
                            Thread.Sleep(400);
                        }
                        break;
                    case 4:
                        rectB.Visibility = Visibility.Visible;
                        
                        tellerB = 1;
                        break;
                    case 8:
                        rectA.Visibility = Visibility.Visible;

                        tellerA = 1;
                        break;
                    case 16:
                        rectMin.Visibility = Visibility.Visible;
                        if (isWiiMoteAppWorking)
                        {
                            System.Windows.Forms.SendKeys.SendWait("^({-})");
                            Thread.Sleep(400);
                        }
                        break;
                    case 128:
                        rectHome.Visibility = Visibility.Visible;
                        if(isWiiMoteAppWorking)
                        {
                        System.Windows.Forms.SendKeys.SendWait("^({ESC})");
                        Thread.Sleep(500);
                        }
                        break;
                }
            }

            if (isWiiMoteAppWorking)
            {

                if (tellerA == 1)
                {
                    if (tellerA2 == 0)
                    {
                        if (isMouseCursorOnScreen) LeftMouseDown();
                        else System.Windows.Forms.SendKeys.SendWait("~");
                        tellerA2 = 1;
                        tellerA3 = 1;
                    }
                }
                else if (tellerA == 0 && tellerA3 ==1)
                {
                    LeftMouseUp();
                    tellerA2 = 0;
                    tellerA3 = 0;
                }


                if (tellerB == 1)
                {
                    if (tellerB2 == 0)
                    {
                        if (isMouseCursorOnScreen) RightMouseDown();
                        else System.Windows.Forms.SendKeys.SendWait("{ESC}");
                        RightMouseDown(); 
                        tellerB2 = 1;
                        tellerB3 = 1;
                    }
                }
                else if (tellerB == 0 && tellerB3 == 1)
                {
                    if (isMouseCursorOnScreen) RightMouseUp();
                    tellerB2 = 0;
                    tellerB3 = 0; 
                } 
            }
            
            
            
        }
        private List<int> controleerButtons(int data)
        {
            int l = data;
            List<int> selectedButton = new List<int>();
            selectedButton.Add(data);

            for (int i = 7; i > -1; i--)
            {
                int macht = (int)Math.Pow(2, i);
                if (l >= macht)
                {
                    selectedButton.Add(macht);
                    l -= macht;
                };
            }
            return selectedButton;
        }

        private void verwerkDataIR(byte[] data)
        {
            int X1Y1X2Y2 = data[7];
            string bin = Convert.ToString(X1Y1X2Y2, 2);

            while (bin.Length < 8)
            {
                bin = "0" + bin;
            }

            string sY1 = bin.Substring(0, 2) + "00000000";
            string sX1 = bin.Substring(2, 2) + "00000000";
            string sY2 = bin.Substring(4, 2) + "00000000";
            string sX2 = bin.Substring(6) + "00000000";

            float X1 = data[5] + Convert.ToInt32(sX1, 2);
            float Y1 = data[6] + Convert.ToInt32(sY1, 2);

            float X2 = data[8] + Convert.ToInt32(sX2, 2);
            float Y2 = data[9] + Convert.ToInt32(sY2, 2);

            int X3Y3X4Y4 = data[12];
            string bin2 = Convert.ToString(X3Y3X4Y4, 2);

            while (bin2.Length < 8)
            {
                bin2 = "0" + bin;
            }

            string sY3 = bin.Substring(0, 2) + "00000000";
            string sX3 = bin.Substring(2, 2) + "00000000";
            string sY4 = bin.Substring(4, 2) + "00000000";
            string sX4 = bin.Substring(6) + "00000000";

            float X3 = data[10] + Convert.ToInt32(sX3, 2);
            float Y3 = data[11] + Convert.ToInt32(sY3, 2);

            float X4 = data[13] + Convert.ToInt32(sX4, 2);
            float Y4 = data[14] + Convert.ToInt32(sY4, 2);

            X1 /= 1023;
            Y1 /= 765;

            X2 /= 1023;
            Y2 /= 765;
            X3 /= 1023;
            Y3 /= 765;
            X4 /= 1023;
            Y4 /= 765;

            txtTest.Content = /*(float)report.Data[0] / 255 + " " + (float)report.Data[1] / 255 + " " + */
                  "Accelerometer" + System.Environment.NewLine
                + "-------------------------" + System.Environment.NewLine
                + "X " + (float)(data[2] - 121) + " Y " + (float)(data[3] - 122) + " Z " + (float)(data[4] - 123)
                + System.Environment.NewLine + System.Environment.NewLine

                + "IR Camera" + System.Environment.NewLine
                + "-------------------------" + System.Environment.NewLine
                + X1 + " " + Y1 + System.Environment.NewLine
                + X2 + " " + Y2 + System.Environment.NewLine
                + X3 + " " + Y3 + System.Environment.NewLine
                + X4 + " " + Y4;



            xPos = (int)(Math.Round((decimal)-(X1 * resX - resX), 0))-verschil;
            yPos = (int)(Math.Round((decimal)Y1 * resY, 0)-verschil);

            isMouseCursorOnScreen = false;
            if (xPos < resX-verschil && xPos > -verschil && yPos < resY-verschil && yPos > -verschil)
            { 
            rect1.Margin = new Thickness(0, (float)Y1 * 255, (float)X1 * 255, 0);
            rect2.Margin = new Thickness(0, (float)Y2 * 255, (float)X2 * 255, 0);
            rect3.Margin = new Thickness(0, (float)Y3 * 255, (float)X3 * 255, 0);
            rect4.Margin = new Thickness(0, (float)Y4 * 255, (float)X4 * 255, 0);

            isMouseCursorOnScreen = true;
             //muis bewegen
           if(isWiiMoteAppWorking) SetCursorPos(xPos, yPos);
           }





        }
        #endregion

        //WriteData
        private void WriteData(int address, byte[] data)
        {
            if ((_device != null))
            {
                int index = 0;

                while (index < data.Length)
                { // Bepaal hoeveel bytes er nog moeten verzonden worden 
                    int leftOver = data.Length - index;
                    
                    // We kunnen maximaal 16 bytes per keer verzenden dus moeten we het aantal te verzenden bytes daarop limiteren 
                    int count = (leftOver > 16 ? 16 : leftOver);
                    
                    
                    int tempAddress = address + index;
                    HIDReport rep = _device.CreateReport();
                    rep.ReportID = 0x16;
                    rep.Data[0] = (byte)((tempAddress & 0x4000000) >> 0x18);
                    rep.Data[1] = (byte)((tempAddress & 0xff0000) >> 0x10);
                    rep.Data[2] = (byte)((tempAddress & 0xff00) >> 0x8);
                    rep.Data[3] = (byte)((tempAddress & 0xff));
                    rep.Data[4] = (byte)count; 

                    Buffer.BlockCopy(data, index, rep.Data, 5, count); 

                    _device.WriteReport(rep); 
                    index += 16;
                }
            }
        }

        //Checkboxes click event
        private void chkLed_Click(object sender, RoutedEventArgs e)
        {
            i=0;
            foreach (var ctrl in GridCheckBoxes.Children)
            {
                if (ctrl is CheckBox)
                {
                    CheckBox chk = (CheckBox)ctrl;

                    if (chk.IsChecked==true)
                    {
                        i += Convert.ToInt32(chk.Tag);
                    }
                }
            }
                //trillen
                HIDReport report = _device.CreateReport();
                report.ReportID = 0x11;
                report.Data[0] = (byte)i;
                _device.WriteReport(report);

            checkLeds(i);
                
        }


        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
        }

        private void checkLeds(int leds)
        { 
        List<int> test = controleerButtons(leds);

                        chkLed1.IsChecked = false;
                        chkLed2.IsChecked = false;
                        chkLed3.IsChecked = false;
                        chkLed4.IsChecked = false;

                        if (test.Contains(16))
                        {
                            rectLed1.Visibility = Visibility.Visible;
                            chkLed1.IsChecked = true;
                        }
                        else {
                            rectLed1.Visibility = Visibility.Hidden;
                            chkLed1.IsChecked = false;
                        }
                        if (test.Contains(32))
                        {
                            rectLed2.Visibility = Visibility.Visible;
                            chkLed2.IsChecked = true;
                        }
                        else
                        {
                            rectLed2.Visibility = Visibility.Hidden;
                            chkLed2.IsChecked = false;
                        }
                        if (test.Contains(64))
                        {
                            rectLed3.Visibility = Visibility.Visible;
                            chkLed3.IsChecked = true;
                        }
                        else
                        {
                            rectLed3.Visibility = Visibility.Hidden;
                            chkLed3.IsChecked = false;
                        }
                        if (test.Contains(128))
                        {
                            rectLed4.Visibility = Visibility.Visible;
                            chkLed4.IsChecked = true;
                        }
                        else
                        {
                            rectLed4.Visibility = Visibility.Hidden;
                            chkLed4.IsChecked = false;
                        }
        }
    }
}
