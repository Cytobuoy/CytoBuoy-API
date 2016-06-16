using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CytoSense;
using CytoSense.Data.ParticleHandling;
using CytoSense.Data.ParticleHandling.Channel;
using System.IO;

namespace CytoExampleDLL_CSharp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }


        private FileSystemWatcher thisdirwatcher;


        private void Form1_Load(object sender, EventArgs e)
        {
            thisdirwatcher = new FileSystemWatcher();
            thisdirwatcher.Path = Path.GetDirectoryName(Application.ExecutablePath);
            thisdirwatcher.NotifyFilter = NotifyFilters.LastWrite;
            thisdirwatcher.Filter = "*.cyz"; //CytoUSB will first write all data to a *.tmp file. When all is finished, it's renamed to *.cyz
            thisdirwatcher.Changed += new FileSystemEventHandler(worker);
            thisdirwatcher.EnableRaisingEvents = true;

            terminalControl1.AppendText("Cyto-cyz-Converter started!", true, Color.LimeGreen);
        }

        private void worker(object sender, FileSystemEventArgs e)
        {
            lock (thisdirwatcher) // make sure only one file at a time is handled
            {
                String[] ls = Directory.GetFiles(Path.GetDirectoryName(Application.ExecutablePath), "*.cyz");
                for (int i = 0; i < ls.Length; i++)
                {
                    String txtfile = Path.GetFileNameWithoutExtension(ls[i]) + Path.GetFileNameWithoutExtension(ls[i]) + ".txt";  //destination filename
                    if (!File.Exists(txtfile))
                    {
                        terminalControl1.AppendText("New file detected: " + Path.GetFileNameWithoutExtension(ls[i]), true, Color.LimeGreen);
                        try
                        {
                            convertFile(ls[i], txtfile);
                            //it might be tempting to delete the *.cyz file now, but a warning is in place: 
                            //the CytoUSB watchdog system may restart the computer if no file was detected some time after it should have.
                        }
                        catch (Exception exp)
                        {
                            terminalControl1.AppendText("Error:" + exp.Message, true, Color.Red);
                        }
                    }
                }
            }
        }

        private void convertFile(String cyzFile, String txtFile)
        {
            CytoSense.Data.DataFileWrapper dfw = new CytoSense.Data.DataFileWrapper(cyzFile);
            StringBuilder s = new StringBuilder();

            for (int i = 0; i < dfw.SplittedParticles.Length - 1; i++)
            {
                s.Append(dfw.SplittedParticles[i].TimeOfArrival.ToLongTimeString() + Environment.NewLine);

                if (i % (dfw.SplittedParticles.Length / 10) == 0) // once in a while let the user know we are in fact very busy doing important work
                {  
                    terminalControl1.AppendText(".", false, Color.LimeGreen);
                    Application.DoEvents();
                }
            }
            System.IO.File.WriteAllText(txtFile, s.ToString());

            terminalControl1.AppendText(" done" + Environment.NewLine, false, Color.LimeGreen);

            
        }
    }
}
