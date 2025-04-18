using System;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;

class Program
{
    static void Main(string[] args)
    {
        var settings = new SessionSettings("Fix/quickfix.cfg");
        var app = new TakerApp(); // your FIX client implementation
        var storeFactory = new FileStoreFactory(settings);
        var logFactory = new ScreenLogFactory(settings);
        var initiator = new SocketInitiator(app, storeFactory, settings, logFactory);

        initiator.Start();
        Console.WriteLine("🚀 Market Taker started. Press <Enter> to quit.");
        Console.ReadLine();
        initiator.Stop();
    }
}
