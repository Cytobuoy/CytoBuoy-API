% Example file demonstrating the interface between Matlab and the CytoSense
% DLL
% Gijs van der Veen, Kevin van Hecke, Bouke Krom.
% CytoBuoy BV, 2011

% Tested to work with CytoSense.dll version 2.9.1.0 with Matlab version 2015b

%% General processing

% Determine if Matlab is 32 or 64 bit, take appropriate dll.
if strcmp(computer ,'PCWIN64')
    path = [pwd '\x64\CytoSense.dll'];
else
    path = [pwd '\x86\CytoSense.dll'];
end

% Add assembly; path must point to the CytoSense DLL
NET.addAssembly(path);

% Load a datafile into memory, one of these example files. Select by uncommenting one:
%file = [pwd '..\..\..\Datafile_examples\data.cyz']; % Just a regular file or ...
%file = [pwd '..\..\..\Datafile_examples\data iif.cyz']; % A file with pictures or ...
%file = [pwd '..\..\..\Datafile_examples\data curv.cyz']; % A file with a curvature channel or ...
%file = [pwd '..\..\..\Datafile_examples\data PMTTemp.cyz']; % A file with temperature data or...
file = [pwd '..\..\..\Datafile_examples\data laserdistance (tartu).cyz']; % A file with two-laser pulses

ML = CytoSense.Interfaces.MatlabInterface(file);

% The "Wrapper" provides access to derived data (parameters, etc...)
DFW = ML.DataFileWrapper;

% Determine the number of particles
N = DFW.SplittedParticles.Length;
 

%% Loading parameters

% Discover channels. All useful channels (virtual or hardware) are in
% Cytosettings.channellist. Notice that with the array.Get-command, things
% are zero-based.

DFW.CytoSettings.ChannelList.ToArray.Get(0).ChannelInfo.name
DFW.CytoSettings.ChannelList.ToArray.Get(1).ChannelInfo.name
DFW.CytoSettings.ChannelList.ToArray.Get(2).ChannelInfo.name
% etc

%There is more info in the channelinfo
DFW.CytoSettings.ChannelList.ToArray.Get(1).ChannelInfo.color.R
DFW.CytoSettings.ChannelList.ToArray.Get(1).ChannelInfo.color.G
DFW.CytoSettings.ChannelList.ToArray.Get(1).ChannelInfo.color.B

% Get the list of parameter names
parnames = ML.GetParameterList;
% Convert the .Net Strings to Matlab strings
for i = 1:parnames.Length
    names{i} = char(parnames(i));
end

% Load all parameters for each channel
% There is no need for checks for curvature etc. This will be done by the
% DLL, and will be reflected in the ChannelList
for i = 0:DFW.CytoSettings.ChannelList.ToArray.Length-1
    par{i+1} = single(ML.GetAllParametersForChannel(i));
end
 

%The TOF parameter is a feature of the whole particle, not of one channel.
%When using the CytoSense DLL from within .NET this makes sense, in Matlab
%the structure is somewhat different so we have to get it seperately:
TOFs = single(ML.GetAllTOFs());  
TOFs = TOFs';                   % get the same orientation as the other parameter arrays

%On special request, direct access to the raw FWS left + right total
%parameter. 
if DFW.CytoSettings.hasCurvature
    FWSLR = single(ML.GetFWS_LR_Totals());  
    FWSLR = FWSLR';                   % get the same orientation as the other parameter arrays
end

%get a concetenated vector of all channel information of one particle:
p = DFW.SplittedParticles.Get(1);
v = single(p.ParticleVector(0,CytoSense.Data.ParticleHandling.VectorMode.Channel,CytoSense.Data.ParticleHandling.NormalizeMode.None)); % the 0 argument means to disable interpolation
 
%% Plotting parameters

% Select two parameters and plot them
figure(1); clf;
xpar = 1; ypar = 2;
xchn = 2; ychn = 2;
loglog(par{xchn}(:,xpar),par{ychn}(:,ypar),'ok')
xlabel(strcat(names(xpar),', ',char(DFW.CytoSettings.ChannelList.ToArray.Get(xchn).ChannelInfo.name)));
ylabel(strcat(names(ypar),', ',char(DFW.CytoSettings.ChannelList.ToArray.Get(ychn).ChannelInfo.name)));

%% Processing concentration data
%getting the concentration has been made simpler. The dll will check which
%concentration count is best and will output this:
try
    Concentration = DFW.Concentration(CytoSense.Data.ConcentrationModeEnum.Automatic)
    catch e
    % the datafiles can have a number of different ways to store the
    % concentration. In most cases the dll will calculate the correct
    % concentration, however in certain cases this is not possible due to
    % discrepancies in the stored values. This especially happens with 
    % older datafiles from witch the instrument did not have the upgrade 
    % package yet.
    % In this case the user has to look at the raw concentration sensor 
    % output to see what happened. In CytoClus this has been implemented 
    % by showing a graph of cell counts vs time. 
    ['Error: ' e.message(10:end)]
end
%% Other information

INFO = ML.DataFile.MeasurementInfo;

INFO.MeasurementStart.ToString
INFO.ActualMeasureTime % seconds
% and more...

SET = DFW.CytoSettings;
% A few interesting ones
SET.SampleCorespeed
SET.LaserBeamWidth
SET.SerialNumber
SET.HardwareNr

%% Triggerchannels

%It is possible to have selected more than one triggerchannel. This makes
%the Matlab code somewhat more difficult.
%Which channels were selected as triggerchannels is saved in an mask array,
%which if overlayd with the channelnames array give you the
%triggerchannels:
TrgChns = int32(ML.DataFile.MeasurementSettings.TriggerChannelArray);
Ntrg = sum(TrgChns); %get the number of triggerchannels
TrgChns  = double(TrgChns) .* (1:length(TrgChns)); %get the triggerchannel id's.

%ok, now to get the actual names of the triggerchannel:
TrgChnsStr = cell(Ntrg,1);
count = 1;
for i = 1:length(TrgChns)
   if TrgChns(i)
       chn = char(DFW.CytoSettings.channels.Get(TrgChns(i)).name);
       TrgChnsStr{count} = chn;
       count = count+1;
   end
end
%Triggerchannels are still based on hardware channels, so the hardware channel list 
%is still used, and triggering on the
%combined FWS curvature channel for example is impossible. 

%% Image-in-flow (if available)
if ML.DataFile.MeasurementSettings.IIFCheck  % check if IIF was enabled during this measurement
   
    %to get the id's of only the particles that are imaged:
    IDs = int32(DFW.GetImageIDs) + 1;
    
    %retrieve the raw image of the first imaged particle
    % Note that the Get()-command is 0-based instead of 1-based
    im = DFW.SplittedParticles.Get(IDs(1)-1).Image();

    %convert the .Net framework image to something that Matlab actually can handle
    x =  uint8(CytoSense.Interfaces.MatlabInterface.getRGBArrayFromImage(im));
    figure;
    image(x)
    

    %retrieve the processed (cut-out, background cancelled, scale bar, etc)
    %to process the image, a third party dll is used. This has to be added
    %in Matlab as well.
    if strcmp(computer ,'PCWIN64')
        path1 = [pwd '\x64\AForge.dll'];
		path2 = [pwd '\x64\AForge.Math.dll'];
        path3 = [pwd '\x64\AForge.Imaging.dll'];
    else
        path1 = [pwd '\x86\AForge.dll'];
		path2 = [pwd '\x64\AForge.Math.dll'];
        path3 = [pwd '\x86\AForge.Imaging.dll'];
    end
    NET.addAssembly(path1);
    NET.addAssembly(path2);
	NET.addAssembly(path3);
    
    x_processed =  uint8(CytoSense.Interfaces.MatlabInterface.getRGBArrayFromImage(DFW.SplittedParticles.Get(IDs(1)-1).ProcessedImage()));
    figure;
    image(x_processed)

end

%retrieve some secondary sensor information from the datafile. These
%sensors need the PIC, so first check if one was availabe. Then each sensor
%is an optional, so also check for each sensor if it exists
%Each secondary sensor is sampled many times during the measurement. Also,
%it may start just before the actual measurement and run some time after
%the measurement. The getMean fuction calculates the mean value of all
%samples. If only samples from during the measurement are needed, this can
%be done because each sample is saved with a time stamp. I will however not
%include it in this example for now.
if DFW.CytoSettings.hasPIC
   if DFW.CytoSettings.PIC.I2CTemp_Sheath
        SheathTemp =   double( DFW.MeasurementInfo.sensorLogs.SheathTemp.getMean);
   end
   try
       if DFW.CytoSettings.PIC.I2CTemp_PMT
           PMTtemp =   double( DFW.MeasurementInfo.sensorLogs.PMTTemp.getMean);
       end
       if DFW.CytoSettings.PIC.I2CTemp_Laser
           Lasertemp =   double( DFW.MeasurementInfo.sensorLogs.LaserTemp.getMean);
       end
   catch e %due to a bug in the code when first installed the extra PMT temp sensor, they got save to the wrong location       
       if DFW.CytoSettings.PIC.I2CTemp_PMT
           PMTtemp =   double( DFW.MeasurementInfo.sensorLogs.LaserTemp.getMean);
       end       
   end
    
end

%use the following if running more then one datafile after eachother:

%clear the datafile from the memory. 
%ML.Clear; 
