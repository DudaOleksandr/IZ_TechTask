using IZTechTask;
using IZTechTask.Enums;

var outputFilePath = "iq_data.bin";

using (var netSdrClient = new NetSdrClient())
{
    netSdrClient.Connect("localhost");
    
    netSdrClient.StartIqStream(DataFormat.Real, CaptureMode.Contiguous16Bit);
    
    netSdrClient.StartIqDataReceiver(outputFilePath);

    Console.WriteLine("Press Enter to stop receiving...");
    Console.ReadLine();

    netSdrClient.StopIqDataReceiver();
    netSdrClient.StopIqStream();
}

Console.WriteLine("Data receiving stopped. File saved.");