using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;



namespace projectWII
{
    public class Program
    {
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenu contextMenu1;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItem2;
        private System.Windows.Forms.MenuItem menuItem3;
        private System.ComponentModel.IContainer components;

        [STAThread]
        static void Main(string[] args)
        {
            Program pg = new Program();
            //pg.CreateNotifyicon();
            Application.Run();
            Console.ReadLine();
        }

        MainWindow mw = new MainWindow();
        Program()
        {
            CreateNotifyicon();
        }
        private void CreateNotifyicon()
        {
            this.components = new System.ComponentModel.Container();
            this.contextMenu1 = new System.Windows.Forms.ContextMenu();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();

            // Initialize menuItem1
            this.menuItem1.Index = 0;
            this.menuItem1.Text = "Wiimote Control Aanzetten";
            this.menuItem1.Click += new System.EventHandler(this.menuItem1_Click);

            // Initialize menuItem2
            this.menuItem2.Index = 1;
            this.menuItem2.Text = "Testprogramma Openen";
            this.menuItem2.Click += new System.EventHandler(this.menuItem2_Click);

            // Initialize menuItem3
            this.menuItem3.Index = 2;
            this.menuItem3.Text = "Exit";
            this.menuItem3.Click += new System.EventHandler(this.menuItem3_Click);


            // Initialize contextMenu1
            this.contextMenu1.MenuItems.AddRange(
                        new System.Windows.Forms.MenuItem[] { this.menuItem1, this.menuItem2, this.menuItem3 });

            // Create the NotifyIcon.
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);

            // The Icon property sets the icon that will appear
            // in the systray for this application.
            notifyIcon1.Icon = new Icon("Icon1.ico");

            // The ContextMenu property sets the menu that will
            // appear when the systray icon is right clicked.
            notifyIcon1.ContextMenu = this.contextMenu1;

            // The Text property sets the text that will be displayed,
            // in a tooltip, when the mouse hovers over the systray icon.
            notifyIcon1.Text = "Wiimote MouseControl App";
            notifyIcon1.Visible = true;

            // Handle the DoubleClick event to activate the form.
            //notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
            //notifyIcon1.Click += new System.EventHandler(this.notifyIcon1_Click);
            notifyIcon1.Click += new System.EventHandler(this.menuItem2_Click);

        }
        private void notifyIcon1_Click(object Sender, EventArgs e)
        {

            MessageBox.Show("clicked");
        }

        private void notifyIcon1_DoubleClick(object Sender, EventArgs e)
        {
            MessageBox.Show("Double clicked");
        }

        private void menuItem3_Click(object Sender, EventArgs e)
        {
            // Close the form, which closes the application.
            Application.Exit();
        }

        private void menuItem2_Click(object Sender, EventArgs e)
        {
            
            openTestProgramma();
        }

        public void openTestProgramma()
        {
            
            try
            {
            mw.isWiiMoteAppWorking = false;
            this.menuItem1.Text = "Wiimote Control Aanzetten";
            mw.Visibility = System.Windows.Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Geen WiiMote Device gevonden, gelieve een device aan te sluiten."
                    + System.Environment.NewLine + System.Environment.NewLine
                    + "Error:"
                    + System.Environment.NewLine + " << " + ex.Message + " >>",
                    "Er is een fout opgetreden bij het opstarten van ...", MessageBoxButtons.OK);
                Application.Exit();
            }
        }


        public void zetProgrammaAan()
       { 
            try
                {
                   if (this.menuItem1.Text == "Wiimote Control Aanzetten")
                {
                    mw.isWiiMoteAppWorking = true;
                    this.menuItem1.Text = "Wiimote Control Uitzetten";
                }
                else
                {
                    mw.isWiiMoteAppWorking = false;
                    this.menuItem1.Text = "Wiimote Control Aanzetten";
                }

                mw.Visibility = System.Windows.Visibility.Hidden;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Geen WiiMote Device gevonden, gelieve een device aan te sluiten."
                    + System.Environment.NewLine + System.Environment.NewLine
                    + "Error:"
                    + System.Environment.NewLine + " << " + ex.Message + " >>",
                    "Er is een fout opgetreden bij het opstarten van ...", MessageBoxButtons.OK);
            Application.Exit();
            }
        }
        private void menuItem1_Click(object Sender, EventArgs e)
        {
            
                zetProgrammaAan();
            
        }

    }
}