Imports CytoSense
Imports CytoSense.Data.ParticleHandling
Imports CytoSense.Data.ParticleHandling.Channel

Public Class Form1

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Dim ofd As New OpenFileDialog
        ofd.Filter = "CytoSense datafile|*.cyz"
        If ofd.ShowDialog = vbOK Then


            'load the data from the serialized data file:
            Dim df As CytoSense.Data.DataFile = CytoSense.Data.DataFunctions.loadDatafile(ofd.FileName)
            'the complete measurement file (data, cytosense info, images etc) is now in the df object.


            'to handle this data we have written a wrapper:
            Dim dfe As New CytoSense.Data.DataFileWrapper(df)

            'the actual cytosense data is located here:
            Dim particles As CytoSense.Data.ParticleHandling.Particle() = dfe.SplittedParticles 'decodes the particle data from the raw cytosense data array

            'from here you can acces all kinds of interesting stuff, for instance:
            Dim channel_data() As Single = particles(0).ChannelData(3).Data  'gets you the channeldata of channel 3 of particle 0
            Dim total_channel3_of_Particle5 = particles(5).ChannelData(3).Parameter(ChannelData.ParameterSelector.Total) 'gives the total feature of channel 3 of particle 5

            'if you want to know what kind of channel channelData(3) is look in .information:
            Dim name As String = particles(5).ChannelData(3).Information.name 'for instance, the name
            'or even better in the machine configuration settings
            Dim sameName As String = dfe.CytoSettings.ChannelList(3).ToString

            Dim number_of_particles_in_this_file As Integer = particles.Length

            'calculate concentration
            Dim concentration As Double = dfe.Concentration
            'estimate volume
            Dim voume As Double = dfe.SplittedParticles.Length / concentration

            'to get more insight, it might be a good idea to put the dfe object into the watch, and browse through it!

            'Image-in-flow (pictures of the particles)(if available)
            If dfe.MeasurementSettings.IIFCheck Then ' check if IIF was enabled during this measurement
                'to get only the particles that are imaged:
                Dim parts_iif As Particle() = dfe.SplittedParticlesWithImages

                'Let's take the first one
                Dim part_iif0 As ImagedParticle = parts_iif(0)
                'now to get the IIF image data. Be aware that for some reason the Image object of .net seems to use very much memory. Recommend to not keep to many of those in memory. A workaround is using part_iif(0).ImageStream, this consumes less memory.
                Dim im_raw As Image = part_iif0.Image

                'The image is usually much larger than the particle. The ProcessedImage is automatically cropped around the particle. Note that this image processing might take some time.
                Dim im_processed As Image = part_iif0.ProcessedImage

                'every particle in part_iif also contains its normal particle data like the pulseshapes and the parameters:
                Dim FWSLength_iif_part0 As Single = part_iif0.ChannelData(1).Parameter(ChannelData.ParameterSelector.Length)
                'each iif particle can be found in the normal splittedparticle array by its id:
                Dim part_iif0ID As Integer = part_iif0.ID


                'saving an image to a file
                im_raw.Save(My.Computer.FileSystem.SpecialDirectories.Desktop & "\im_raw.jpg")

            End If
        End If
    End Sub
End Class
