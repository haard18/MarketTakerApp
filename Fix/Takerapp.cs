using System;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
public class Quote
{
    public string Symbol { get; set; }
    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
    public DateTime LastUpdated { get; set; }
    public override string ToString()
    {
        return $"Symbol: {Symbol}, Bid: {Bid}, Ask: {Ask}, LastUpdated: {LastUpdated}";
    }
}
public class TakerApp : MessageCracker, IApplication
{


    private readonly Dictionary<string, Quote> quotes = new();

    public void SendMarketDataRequest(SessionID sessionID, string symbol)
    {
        string mdReqId = Guid.NewGuid().ToString();
        var request = new MarketDataRequest(
            new MDReqID(mdReqId),
            new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES),
            new MarketDepth(1)
        );
        var mdEntryTypesGroup = new MarketDataRequest.NoMDEntryTypesGroup();
        mdEntryTypesGroup.Set(new MDEntryType(MDEntryType.BID));
        request.AddGroup(mdEntryTypesGroup);

        var mdEntryTypesGroup2 = new MarketDataRequest.NoMDEntryTypesGroup();
        mdEntryTypesGroup2.Set(new MDEntryType(MDEntryType.OFFER));
        request.AddGroup(mdEntryTypesGroup2);
        var symbolsGroup = new MarketDataRequest.NoRelatedSymGroup();
        symbolsGroup.Set(new Symbol(symbol));
        request.AddGroup(symbolsGroup);

        Console.WriteLine($"[FIX] Sending MarketDataRequest: {request}");
        Session.SendToTarget(request, sessionID);
    }

    public void OnCreate(SessionID sessionID) =>
        Console.WriteLine($"[FIX] Session created: {sessionID}");

    public void OnLogon(SessionID sessionID)
    {
        Console.WriteLine($"[FIX] ✅ Logon: {sessionID}");
        // Send a market data request after logging in
        SendMarketDataRequest(sessionID, "EUR/USD");
    }
    public void OnLogout(SessionID sessionID) =>
        Console.WriteLine($"[FIX] ❌ Logout: {sessionID}");

    public void FromAdmin(QuickFix.Message message, SessionID sessionID) =>
        Console.WriteLine($"[FIX] FromAdmin: {message}");

    public void ToAdmin(QuickFix.Message message, SessionID sessionID) =>
        Console.WriteLine($"[FIX] ToAdmin: {message}");

    public void FromApp(QuickFix.Message message, SessionID sessionID)
    {
        Console.WriteLine($"[FIX] 📨 FromApp: {message}");
        Crack(message, sessionID);
    }

    public void ToApp(QuickFix.Message message, SessionID sessionID) =>
        Console.WriteLine($"[FIX] ToApp: {message}");
    public void OnMessage(MarketDataSnapshotFullRefresh msg, SessionID sessionID)
    {
        Symbol symbolField = new Symbol();
        msg.GetField(symbolField);
        string symbol = symbolField.Value;           // ✅ correct
        decimal bid = 0;
        decimal ask = 0;
        for (int i = 1; i <= msg.NoMDEntries.GetLength(); i++)
        {
            var group = new MarketDataSnapshotFullRefresh.NoMDEntriesGroup();
            msg.GetGroup(i, group);
            var entryType = new MDEntryType();
            var price = new MDEntryPx();
            group.GetField(entryType);
            group.GetField(price);
            if (entryType.Value == MDEntryType.BID)
            {
                bid = price.Value;
            }
            else if (entryType.Value == MDEntryType.OFFER)
            {
                ask = price.Value;
            }
        }
        var quote = new Quote
        {
            Symbol = symbol,
            Bid = bid,
            Ask = ask,
            LastUpdated = DateTime.UtcNow
        };
        quotes[symbol] = quote;
        Console.WriteLine("📈 MarketDataSnapshotFullRefresh received:");
    }

    public void OnMessage(MarketDataIncrementalRefresh msg, SessionID sessionID)
    {
        for (int i = 1; i <= msg.NoMDEntries.GetLength(); i++)
        {
            var group = new MarketDataIncrementalRefresh.NoMDEntriesGroup();
            msg.GetGroup(i, group);
            var entryType = new MDEntryType();
            var symbolField = new Symbol();
            var price = new MDEntryPx();
            if (group.IsSetField(Tags.MDEntryType))
                group.GetField(entryType);
            if (group.IsSetField(Tags.Symbol))
                group.GetField(symbolField);
            if (group.IsSetField(Tags.MDEntryPx))
                group.GetField(price);
            string symbol = symbolField.Value;
            decimal newPrice = price.Value;

            if (!quotes.ContainsKey(symbol))
            {
                quotes[symbol] = new Quote { Symbol = symbol };
            }

            if (entryType.Value == MDEntryType.BID)
            {
                quotes[symbol].Bid = newPrice;
            }
            else if (entryType.Value == MDEntryType.OFFER)
            {
                quotes[symbol].Ask = newPrice;
            }
        }

    }
}
