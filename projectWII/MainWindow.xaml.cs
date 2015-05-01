using System;
using System.Collections.Generic;
using System.Linq;
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
        //muis bewegen functie
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);



        //This simulates a left mouse click
        public void LeftMouseDown()
        {
            mouse_event(0x02, xPos, yPos, 0, 0);
        }

        public void LeftMouseUP()
        {
            mouse_event(0x04, xPos, yPos, 0, 0);
        }




        int xPos;
        int yPos;
        int i = 0;
        
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
                    "Er is een fout opgetreden bij het opstarten van ...",MessageBoxButton.OK,MessageBoxImage.Exclamation);
                this.Close();
            }
        }

        private void init()
        {
            _device.ReadReport(OnReadReport);
            aanzetCamera();
            EnableSpeaker();
        }
        private void aanzetCamera()
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
            //byte[] arr2 = { 0x02,0x00,0x00,0x71,0x01,0x00,0x90,0x00,0x41 }; //<== pdf
            //byte[] arr2 = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x0C }; // <== Kestrel
            byte[] arr2 = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x90, 0x00, 0xC0 }; // <== Marcan
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
        int t = 0;
        private void OnReadReport(HIDReport report)
        {
            if(report.ReportID != 0x37 && report.ReportID != 0x20)   Console.WriteLine(report.ReportID);

            if (Thread.CurrentThread != Dispatcher.Thread) 
            {
                this.Dispatcher.Invoke(new ReadReportCallback(OnReadReport), report);
            }
            else {
               
                switch (report.ReportID)
                {
                    case 0x18:
                        //speakers data

                        break;
                    case 0x20:
                        // Status info 
                        zetOnzichtbaar();
                        maakZichtbaar(report.Data);
                        
                        break;
                    case 0x37:
                        //IR Camera + Accelerometer
                        verwerkDataIR(report.Data);

                        break;
                }

                //reports elk om zijn beurt sturen
                if (t == 2)
                {
                    t = 0;
                    
                }
                else if (t == 1)
                {
                    HIDReport rep = _device.CreateReport();
                    rep.ReportID = 0x15;
                    rep.Data[0] = 0x00;
                    rep.Data[1] = 0x20;
                    _device.WriteReport(rep);
                    t = 2;
                }
                else if(t==0)
                {
                    HIDReport rep = _device.CreateReport();
                    rep.ReportID = 0x12;
                    rep.Data[0] = 0x04;
                    rep.Data[1] = 0x37;
                    _device.WriteReport(rep);
                    t = 1; 
                }

                //callback methode
                _device.ReadReport(OnReadReport);
           }
        }

        

        //Hulpmethodes voor reports inlezen
        //Rechthoeken onzichtbaar maken
        private void zetOnzichtbaar()
        {
            foreach (Rectangle rect in gridFront.Children)
            {
                rect.Visibility = Visibility.Hidden;
            }
        }

        //Rechthoeken tonen + checkboxen aanvinken als nodig + batterij level aanduiden
        int tellerA = 0;
        int tellerAControle = 0;
        private void maakZichtbaar(Byte[] data)
        {
            List<int> test = controleerButtons(data[0]);
            for (int i = 0; i < test.Count; i++)
            {
                switch (test[i])
                {
                    case 1:
                        rectLeft.Visibility = Visibility.Visible;
                        break;
                    case 2:
                        rectRight.Visibility = Visibility.Visible;
                        break;
                    case 4:
                        rectDown.Visibility = Visibility.Visible;
                        break;
                    case 8:
                        rectUp.Visibility = Visibility.Visible;
                        break;
                    case 16:
                        rectPlus.Visibility = Visibility.Visible;
                        break;
                }

            }

            tellerA = 0;
            test = controleerButtons(data[1]);
            for (int i = 1; i < test.Count; i++)
            {
                switch (test[i])
                {
                    case 1:
                        rectTwo.Visibility = Visibility.Visible;
                        break;
                    case 2:
                        rectOne.Visibility = Visibility.Visible;
                        break;
                    case 4:
                        rectB.Visibility = Visibility.Visible;
                        break;
                    case 8:
                        rectA.Visibility = Visibility.Visible;
                        tellerA = 1;
                        tellerAControle = 1;
                        break;
                    case 16:
                        rectMin.Visibility = Visibility.Visible;
                        break;
                    case 128:
                        rectHome.Visibility = Visibility.Visible;
                        break;
                }
            }

            if (tellerA == 1)
            {
                LeftMouseDown();
                Thread.Sleep(50);
            }

            if (tellerA == 0 && tellerAControle == 1)
            {
                LeftMouseUP();
                tellerAControle = 0;
            } 
            



            test = controleerButtons(data[2]);

            chkLed1.IsChecked = false;
            chkLed2.IsChecked = false;
            chkLed3.IsChecked = false;
            chkLed4.IsChecked = false;
            for (int i = 0; i < test.Count; i++)
            {
                switch (test[i])
                {
                    case 16:
                        rectLed1.Visibility = Visibility.Visible;
                        chkLed1.IsChecked = true;
                        break;
                    case 32:
                        rectLed2.Visibility = Visibility.Visible;
                        chkLed2.IsChecked = true;
                        break;
                    case 64:
                        rectLed3.Visibility = Visibility.Visible;
                        chkLed3.IsChecked = true;
                        break;
                    case 128:
                        rectLed4.Visibility = Visibility.Visible;
                        chkLed4.IsChecked = true;
                        break;
                }
            }


            //Batterijlevel aanduiden
            decimal d = (decimal)data[5];
            d /= 255;
            d *= 90;
            d = Math.Round((decimal)d, 0);
            rectBatteryLevel.Width = (double)d;

            if (d < 15) rectBatteryLevel.Fill = Brushes.DarkSalmon;

            lblBattery.Content = d + "%";

            //trillen
            if (chkTril.IsChecked == true)
            {
                HIDReport report = _device.CreateReport();
                report.ReportID = 0x11;
                report.Data[0] = (byte)i;
                _device.WriteReport(report);
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

            yPos = (int)Math.Round((decimal)Y1 * 900);

            X2 /= 1023;
            Y2 /= 765;
            X3 /= 1023;
            Y3 /= 765;
            X4 /= 1023;
            Y4 /= 765;

            txtTest.Text = /*(float)report.Data[0] / 255 + " " + (float)report.Data[1] / 255 + " " + */
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




            rect1.Margin = new Thickness(0, (float)Y1 * 255, (float)X1 * 255, 0);
            rect2.Margin = new Thickness(0, (float)Y2 * 255, (float)X2 * 255, 0);
            rect3.Margin = new Thickness(0, (float)Y3 * 255, (float)X3 * 255, 0);
            rect4.Margin = new Thickness(0, (float)Y4 * 255, (float)X4 * 255, 0);


            float X5 = (X1 + X2 + X3 + X4) / 4;
            float Y5 = (Y1 + Y2 + Y3 + Y4) / 4;

            rect5.Margin = new Thickness(0, (float)Y5 * 255, (float)X5 * 255, 0);

            //muis bewegen 
            xPos = (int)(Math.Round((decimal)-(X1 * 1600 - 1600), 0));
            yPos = (int)(Math.Round((decimal)Y1 * 900, 0));
            
            //SetCursorPos(xPos, yPos);
        }

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
            foreach (var ctrl in GridMain.Children)
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
                
            txtTest2.Text = ""+i;
        }

        private void EnableSpeaker()
        {
            HIDReport report = _device.CreateReport();
            //Enable speaker (Send 0x04 to Output Report 0x14)
            report.ReportID = 0x14;
            report.Data[0] = (byte)0x04;
            _device.WriteReport(report);

            //Mute speaker (Send 0x04 to Output Report 0x19)
            report.ReportID = 0x19;
            report.Data[0] = (byte)0x04;
            _device.WriteReport(report);

            //Write 0x01 to register 0xa20009
            byte[] arr1 = { 0x01 }; 
            WriteData(0xa20009, arr1);

            //Write 0x08 to register 0xa20001
            byte[] arr2 = { 0x08 };
            WriteData(0xa20001, arr2);

            //Write 7-byte configuration to registers 0xa20001-0xa20008
            byte[] arr3 = { 0x00, 0x40, 0x70, 0x17, 0x60, 0x00, 0x00 };
            WriteData(0xa20008, arr3);

            //Write 0x01 to register 0xa20008
            WriteData(0xa20008, arr1);

            //Unmute speaker (Send 0x00 to Output Report 0x19)
            report.ReportID = 0x19;
            report.Data[0] = (byte)0x00;
            _device.WriteReport(report);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            HIDReport rep = _device.CreateReport();
            rep.ReportID = 0x18;
            rep.Data[0] = 0xFF;
            rep.Data[1] = 0xEE;
            rep.Data[2] = 0xDD;
            rep.Data[3] = 0xCC;
            rep.Data[4] = 0xBB;
            rep.Data[5] = 0xAA;
            rep.Data[6] = 0x99;
            rep.Data[7] = 0x88;
            rep.Data[8] = 0x77;
            rep.Data[9] = 0x66;
            rep.Data[10] = 0x55;
            rep.Data[11] = 0x44;
            rep.Data[12] = 0x33;
            rep.Data[13] = 0x22;
            rep.Data[14] = 0x11;
            rep.Data[15] = 0x00;
            rep.Data[16] = 0xFF;
            rep.Data[17] = 0xFF;
            rep.Data[18] = 0xFF;
            rep.Data[19] = 0xFF;
            rep.Data[20] = 0xFF;
            _device.WriteReport(rep);
        }
    }
}
