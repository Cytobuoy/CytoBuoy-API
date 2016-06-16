using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CytoSense;
using CytoSense.Data.ParticleHandling;
using CytoSense.Data.ParticleHandling.Channel;


namespace CytoExampleDLL_CSharp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e) {

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "CytoSense datafile|*.cyz";
            if (ofd.ShowDialog() == DialogResult.OK) {
                //load the data from the serialized data file:
                CytoSense.Data.Data.DataFile df = CytoSense.Data.DataFunctions.loadDatafile(ofd.FileName); //CytoSense.Serializing.Serializing.loadFromFile(ofd.FileName);
                //the complete measurement file (data, cytosense info, images etc) is now in the df object.

                //to handle this data we have written a wrapper:
                CytoSense.Data.DataFileWrapper dfe = new CytoSense.Data.DataFileWrapper(df);

                ///the actual cytosense data is located here:
                CytoSense.Data.ParticleHandling.Particle[] particles = dfe.SplittedParticles;  // decodes the particle data from the raw cytosense data array

                //from here you can acces all kinds of interesting stuff, for instance:
                float[] channel_data = particles[0].ChannelData[3].Data;  //gets you the channeldata of channel 3 of particle 0
                double total_channel3_of_Particle5 = (double)particles[5].ChannelData[3].get_Parameter(ChannelData.ParameterSelector.Total); //gives the total feature of channel 3 of particle 5
                
                //if you want to know what kind of channel channelData(3) is look in:
                string name = particles[5].ChannelData[3].Information.name;
                //or even better in the machine configuration settings
                string sameName = dfe.CytoSettings.ChannelList[3].ToString();

                int number_of_particles_in_this_file = particles.Length;

                //calculate concentration
                double concentration = dfe.Concentration;

                //estimate volume
                double voume = dfe.SplittedParticles.Length / concentration;

                //to get more insight, it might be a good idea to put the dfe object into the watch, and browse through it!
                
                //Image-in-flow (pictures of the particles)(if available)
                if (dfe.MeasurementSettings.IIFCheck) { // check if IIF was enabled during this measurement
                    //to get only the particles that are imaged:
                    CytoSense.Data.ParticleHandling.ImagedParticle[] parts_iif = dfe.SplittedParticlesWithImages;

                    //Let's take the first one
                    CytoSense.Data.ParticleHandling.ImagedParticle part_iif0 = parts_iif[0];
                    //now to get the IIF image data. Be aware that for some reason the Image object of .net seems to use very much memory. Recommend to not keep to many of those in memory. If that is needed use part_iif(0).ImageStream.
                    Image im_raw = part_iif0.Image;
                    //for some reason, this function temporarily does not work. We will look into this as soon as possible:
                    Image im_processed = part_iif0.ProcessedImage;

                    //every particle in part_iif also contains its normal particle data like the pulseshapes and the parameters:
                    Single FWSLength_iif_part0 = part_iif0.ChannelData[1].get_Parameter(ChannelData.ParameterSelector.Length);
                    //each iif particle can be found in the normal splittedparticle array by its id:
                    int part_iif0ID = part_iif0.ID;

                    //saving an image to a file
                    im_raw.Save(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\imp_raw.jpg");


                }
            }
        }
    }
}
