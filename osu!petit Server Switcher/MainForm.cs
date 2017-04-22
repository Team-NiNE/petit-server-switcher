using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Specialized;
using System.Security.Cryptography.X509Certificates;
using ss.Properties;
using System.Resources;

namespace PetitServerSwitcher
{
    public partial class MainForm : Form
    {
        public bool petit = false;
        public string petitIP = "211.105.88.250";  // hoto.us
        public string mirrorIP = "211.105.88.250";  // how to make mirror ;-;
        public bool testConnection = false;

        public int currentVersion = 1;     // Increment this and update changelog before compiling a new update

        public string settingsPath = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\osu!petit Server Switcher";
        public string hostsPath = Environment.GetEnvironmentVariable("windir") + "\\system32\\drivers\\etc\\hosts";

        public MainForm()
        {
            InitializeComponent();

            // Make sure hosts file exists
            if (!File.Exists(hostsPath))
                File.AppendAllText(hostsPath, "# Hosts file");

            // Create tooltips
            ToolTip OnOffTooltip = new ToolTip();
            OnOffTooltip.SetToolTip(this.switchButton, "Switch between osu! and osu!petit");
            ToolTip LocalRipwotTooltip = new ToolTip();
            LocalRipwotTooltip.SetToolTip(this.updateIPButton, "Get the right server IP address directly from the server.");
            ToolTip InstallCertificateTooltip = new ToolTip();
            InstallCertificateTooltip.SetToolTip(this.installCertificateButton, "Install/Remove HTTPS certificate.\nYou must have install the certificate in order to connect\nto osu!petit with stable/beta/cutting edge.\nYou don't need the certificate with fallback.");

            // Create settings directory (if it doesn't exists)
            Directory.CreateDirectory(settingsPath);

            // Check if petit.txt exists and if not create a default one
            if (!File.Exists(settingsPath + "\\petit.txt"))
            {
                File.AppendAllText(settingsPath + "\\petit.txt", petitIP + Environment.NewLine);
                File.AppendAllText(settingsPath + "\\petit.txt", mirrorIP + Environment.NewLine);
                File.AppendAllText(settingsPath + "\\petit.txt", "true");
            }

            // Read petit.txt
            string[] petitTxt = File.ReadAllLines(settingsPath + "\\petit.txt");

            // If there are 3 lines, it's not corrupter or memes
            if (petitTxt.Length == 3)
            {
                // Read IP
                petitIP = petitTxt[0];
                mirrorIP = petitTxt[1];

                // Check if testConnection is bool, if yes read it, otherwise use default settings
                bool isBool;
                Boolean.TryParse(petitTxt[2], out isBool);
                if (isBool)
                    testConnection = Convert.ToBoolean(petitTxt[2]);
            }
            else
            {
                // Something went wrong, use default settings
            }

            // Update settings
            updateSettings();

            // Get current hosts configuration
            findServer();

            // Get certificate status and update button text
            updateCertificateButton();

            // Check for updates
            //Thread ut = new Thread(updateThread);
            //ut.Start();

            // Check if we are using old server IP
            checkOldServerIP();
        }


        private void MainForm_Shown(object sender, EventArgs e)
        {
            updateStatusLabel();
        }

        public void saveSettings()
        {
            // Save settings to petit.txt
            File.WriteAllText(settingsPath + "\\petit.txt", petitIP + Environment.NewLine);
            File.AppendAllText(settingsPath + "\\petit.txt", mirrorIP + Environment.NewLine);
            File.AppendAllText(settingsPath + "\\petit.txt", Convert.ToString(testConnection) + Environment.NewLine);
        }

        public bool findServer()
        {
            // Read hosts
            string[] hostsContent = File.ReadAllLines(hostsPath);

            // Loop through all strings
            for (var i = 0; i < hostsContent.Length; i++)
            {
                // Check if current line is not empty (otherwise it throws an exception)
                if (hostsContent[i] != "")
                {
                    // Check if current line is not commented and redirects to osu.ppy.sh
                    if ((Regex.Matches(hostsContent[i], "#").Count == 0) && (Regex.Matches(hostsContent[i], "osu.ppy.sh").Count > 0))
                    {
                        // Our hosts points to petit
                        petit = true;
                        return petit;
                    }
                }
            }

            // Hosts doesn't contain any reference to osu.ppy.sh, we are not pointing to petit
            petit = false;
            return petit;
        }

        public bool updateServer()
        {
            // Check if IP is not empty and valid (I should rewrite this but idc for now)
            if (petitIP != "" && mirrorIP != "")
            {
                IPAddress type;
                if ( (IPAddress.TryParse(petitIP, out type) && type.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) && IPAddress.TryParse(mirrorIP, out type))
                {
                    // Check read only
                    if(IsFileReadOnly(hostsPath))
                        SetFileReadAccess(hostsPath, false);

                    // Read hosts
                    string[] hostsContent = File.ReadAllLines(hostsPath);

                    // Check for any osu.ppy.sh line, remove them
                    for (var i = 0; i < hostsContent.Length; i++)
                    {
                        if (Regex.Matches(hostsContent[i], "(?:osu|a|c|c1|bm6|i).ppy.sh").Count > 0)
                        {
                            // Line that points (or used to point) to osu.ppy.sh, remove it
                            hostsContent[i] = "";
                        }
                    }

                    // Empty hosts
                    try
                    {
                        File.WriteAllText(hostsPath, "");

                        // Rewrite hosts
                        for (var i = 0; i < hostsContent.Length; i++)
                        {
                            if (hostsContent[i] != "")
                            {
                                // Current line is not empty, write it
                                File.AppendAllText(hostsPath, hostsContent[i] + Environment.NewLine);
                            }
                        }

                        // Point to osu!petit if required
                        if (petit)
                        {
                            File.AppendAllText(hostsPath, petitIP + "   osu.ppy.sh" + Environment.NewLine);
                            File.AppendAllText(hostsPath, petitIP + "   c.ppy.sh" + Environment.NewLine);
                            File.AppendAllText(hostsPath, petitIP + "   c1.ppy.sh" + Environment.NewLine);
                            File.AppendAllText(hostsPath, petitIP + "   a.ppy.sh" + Environment.NewLine);
                            File.AppendAllText(hostsPath, petitIP + "   i.ppy.sh" + Environment.NewLine);
                            File.AppendAllText(hostsPath, mirrorIP + "   bm6.ppy.sh" + Environment.NewLine);
                        }

                        return true;
                    }
                    catch
                    {
                        MessageBox.Show("Error while writing hosts file."+Environment.NewLine+"Please make sure hosts is not set to read only mode.");
                        return false;
                    }
                }
                else
                {
                    statusLabel.Text = "Invalid osu!petit/petit!mirror IP address";
                    return false;
                }
            }
            else
            {
                statusLabel.Text = "Invalid osu!petit/petit!mirror IP address";
                return false;
            }
        }

        public void updateStatusLabel()
        {
            // Update statusLabel based on osu!petit variable
            statusLabel.Text = petit ? "You are playing on osu!petit server."+Environment.NewLine+IPTextBox.Text+" - "+MirrorIPTextBox.Text : "You are playing on osu! server.";
            // Ayy k maron sn pigor xd
            updateJennaWarning();
        }

        public void updateJennaWarning()
        {
            if (Application.OpenForms.Count >= 1)
                if (petitIP == "127.0.0.1")
                    Application.OpenForms[0].Height = 330;
                else
                    Application.OpenForms[0].Height = 202;
        }

        public void updateSettings()
        {
            // Update textBoxes in settings group
            IPTextBox.Text = petitIP;
            MirrorIPTextBox.Text = mirrorIP;
            //testCheckBox.Checked = testConnection;
        }

        private void IPTextBox_TextChanged(object sender, EventArgs e)
        {
            // Settings: Update IP address
            petitIP = IPTextBox.Text;
        }

        private void MirrorIPTextBox_TextChanged(object sender, EventArgs e)
        {
            // Settings: Update Mirror IP address
            mirrorIP = MirrorIPTextBox.Text;
        }

        private void testCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            // Settings:  Update test connection
            //testConnection = testCheckBox.Checked;
        }

        private void switchButton_Click(object sender, EventArgs e)
        {
            // Get current hosts status, because it might have changed
            findServer();

            // Switch between osu!petit/osu!, write hosts and update label
            petit = !petit;
            if (updateServer())
                updateStatusLabel();

            // Connection check if we are on osu!petit
            //if (petit && testConnection)
            //    checkPetitConnection();
        }

        public void checkPetitConnection()
        {
            // Checks if osu.ppy.sh actually points to osu!petit
            try
            {
                //WebClient wc = new WebClient();
                //string s = wc.DownloadString("https://osu.ppy.sh/");

                string s;
                using (WebClient client = new WebClient())
                {
                    byte[] response =
                    client.UploadValues("https://osu.ppy.sh/", new NameValueCollection()
                    {
                        { "switcher", "true" },
                    });
                    s = Encoding.UTF8.GetString(response);
                }

                if (s == "ok")
                    updateStatusLabel();    // This changes statuslabel.text to "You are playing on osu!petit"
                else
                    statusLabel.Text = "Error while connecting to osu!petit.";
            }
            catch
            {
                // 4xx / 5xx error
                statusLabel.Text = "Error while connecting to osu!petit.";
            }
        }

        private void genuineButton1_Click(object sender, EventArgs e)
        {
            // Save settings and close
            saveSettings();
            Application.Exit();
        }

        private void groupBox1_Paint(object sender, PaintEventArgs e)
        {
            GroupBox box = sender as GroupBox;
            DrawGroupBox(box, e.Graphics, Color.White, Color.FromArgb(100, 100, 100));
        }

        private void DrawGroupBox(GroupBox box, Graphics g, Color textColor, Color borderColor)
        {
            if (box != null)
            {
                Brush textBrush = new SolidBrush(textColor);
                Brush borderBrush = new SolidBrush(borderColor);
                Pen borderPen = new Pen(borderBrush);
                SizeF strSize = g.MeasureString(box.Text, box.Font);
                Rectangle rect = new Rectangle(box.ClientRectangle.X,
                                               box.ClientRectangle.Y + (int)(strSize.Height / 2),
                                               box.ClientRectangle.Width - 1,
                                               box.ClientRectangle.Height - (int)(strSize.Height / 2) - 1);

                // Clear text and border
                g.Clear(this.BackColor);

                // Draw text
                g.DrawString(box.Text, box.Font, textBrush, box.Padding.Left, 0);

                // Drawing Border
                //Left
                g.DrawLine(borderPen, rect.Location, new Point(rect.X, rect.Y + rect.Height));
                //Right
                g.DrawLine(borderPen, new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height));
                //Bottom
                g.DrawLine(borderPen, new Point(rect.X, rect.Y + rect.Height), new Point(rect.X + rect.Width, rect.Y + rect.Height));
                //Top1
                g.DrawLine(borderPen, new Point(rect.X, rect.Y), new Point(rect.X + box.Padding.Left, rect.Y));
                //Top2
                g.DrawLine(borderPen, new Point(rect.X + box.Padding.Left + (int)(strSize.Width), rect.Y), new Point(rect.X + rect.Width, rect.Y));
            }
        }

        // Update thread
        void updateThread()
        {
            try
            {
                // Get latest version from MinUpdater
                WebClient client = new WebClient();
                var latestVersionID = Int32.Parse(client.DownloadString("https://ver.osu.life/?ss"));

                // Compare versions
                if (latestVersionID > currentVersion)
                {
                    // New update available
                    DialogResult dialogResult = MessageBox.Show("There is a new version of osu!petit Server Switcher available, do you want to download it now?", "New update available!", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        Process.Start("https://ver.osu.life/?ss");
                        Environment.Exit(0);
                    }
                }
            }
            catch
            {
                // Error
            }
        }

        void updateServerIP()
        {
            try
            {
                // Get server ip from ip.osu.life
                WebClient client = new WebClient();
                string[] remoteIPs = client.DownloadString("https://ip.osu.life").TrimEnd('\r', '\n').Split('\n');

                // osu!petit IP
                if (petitIP != remoteIPs[0])
                {
                    IPTextBox.Text = remoteIPs[0];
                    if (updateServer())
                        updateStatusLabel();
                }

                // Mirror IP
                if (mirrorIP != remoteIPs[1])
                {
                    MirrorIPTextBox.Text = remoteIPs[1];
                    if (updateServer())
                        updateStatusLabel();
                }
            }
            catch
            {
                // Error
            }
        }

        void checkOldServerIP()
        {
            try
            {
                // Get old ip from ip.osu.life
                WebClient client = new WebClient();
                var oldIPs = client.DownloadString("https://ip.osu.life/oldip.txt").Split('\n');
                int l = oldIPs.Length;
                for (int i=0; i< l; i++)
                {
                    if (IPTextBox.Text == oldIPs[i] || MirrorIPTextBox.Text == oldIPs[i])
                    {
                        MessageBox.Show("You are using an IP address from an old server.\nServer IP will be updated to new server's one.");
                        updateServerIP();
                        break;
                    }
                }
            }
            catch
            {
                // Error
            }
        }

        private void updateIPButton_Click(object sender, EventArgs e)
        {
            updateServerIP();
        }


        // Returns wether a file is read-only.
        public static bool IsFileReadOnly(string FileName)
        {
            // Create a new FileInfo object.
            FileInfo fInfo = new FileInfo(FileName);

            // Return the IsReadOnly property value.
            return fInfo.IsReadOnly;
        }

        // Sets the read-only value of a file.
        public static void SetFileReadAccess(string FileName, bool SetReadOnly)
        {
            // Create a new FileInfo object.
            FileInfo fInfo = new FileInfo(FileName);

            // Set the IsReadOnly property.
            fInfo.IsReadOnly = SetReadOnly;
        }

        private void updateCertificateButton(bool __installed = true, bool check = true)
        {
            bool installed_root = __installed;
            bool installed_internal = __installed;
            if (check)
            {
                X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                X509Certificate2Collection root_certs = store.Certificates.Find(X509FindType.FindBySubjectName, "Akari Networks SHA256 Root CA", true);
                installed_root = root_certs.Count > 0 ? true : false;
                X509Certificate2Collection inter_certs = store.Certificates.Find(X509FindType.FindBySubjectName, "Akari Networks Internal Security CA", true);
                installed_internal = inter_certs.Count > 0 ? true : false;
            }
            if (installed_root && installed_internal)
            {
                installCertificateButton.Text = "Remove certificate";
                installCertificateButton.Font = new Font(installCertificateButton.Font.Name, installCertificateButton.Font.Size, FontStyle.Regular);
            }
            else
            {
                installCertificateButton.Text = "Install certificate";
                installCertificateButton.Font = new Font(installCertificateButton.Font.Name, installCertificateButton.Font.Size, FontStyle.Bold);
            }
        }

        private void installCertificateButton_Click(object sender, EventArgs e)
        {
            // Check and install certificate
            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindBySubjectName, "Akari Networks SHA256 Root CA", true);

            if (certs.Count > 0)
            {
                // Certificate already installed, remove it
                DialogResult yn = MessageBox.Show("Are you sure you want to remove osu!petit's HTTPS certificate?\nThere's no need to remove it, you'll be able to browse both osu!petit and osu!\nwithout any problem even if the certificate is installed and the switcher is off.", "osu!petit certificate installer", MessageBoxButtons.YesNo);
                if (yn == DialogResult.No)
                {
                    store.Close();
                    return;
                }
                try
                {
                    foreach (X509Certificate2 cert in certs)
                        store.Remove(certs[0]);

                    updateCertificateButton(false, false);

                    MessageBox.Show("Certificate removed!", "osu!petit certificate installer");
                }
                catch
                {
                    MessageBox.Show("Error while removing certificate.", "osu!petit certificate installer");
                }
            }
            else
            {
                // Install certificate
                try
                {
                    // Save the certificate in settingsPath temporary
                    string certFilePath = settingsPath + "\\certificate.cer";
                    File.WriteAllBytes(certFilePath, Resources.certificate);

                    // Get all certficates
                    X509Certificate2Collection collection = new X509Certificate2Collection();
                    collection.Import(certFilePath);

                    // Install all certificates
                    foreach (X509Certificate2 cert in collection)
                        store.Add(cert);

                    updateCertificateButton(true, false);
                    MessageBox.Show("Certificate installed! Try connecting to osu!petit with beta/stable/cutting edge", "osu!petit certificate installer");

                    // Delete temp certificate file
                    File.Delete(certFilePath);
                }
                catch
                {
                    MessageBox.Show("Error while installing certificate.", "osu!petit certificate installer");
                }
            }

            store.Close();
        }
        
        private void localButton_Click(object sender, EventArgs e)
        {
            if (IPTextBox.Text == "127.0.0.1")
                updateServerIP();
            else
                IPTextBox.Text = "127.0.0.1";

            // Switch between osu!petit/osu!, write hosts and update label
            if (updateServer())
                updateStatusLabel();

            petit = true;
        }

        private void genuineTheme1_Click(object sender, EventArgs e)
        {

        }
    }
}
