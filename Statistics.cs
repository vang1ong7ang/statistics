﻿namespace Neo.Plugins;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
public class Statistics : Plugin, IPersistencePlugin
{
    private static readonly UInt160 BNEO = UInt160.Parse("0x48c40d4666f93408be1bef038b6722404d9a4c2a");
    private static readonly UInt160 TEE = UInt160.Parse("0x82450b644631506b6b7194c4071d0b98d762771f");
    private static readonly UInt160 DAO = UInt160.Parse("0x54806765d451e2b0425072730d527d05fbfa9817");
    private static readonly ulong UNTIL = ulong.Parse(Environment.GetEnvironmentVariable("UNTIL") ?? $"{TimeProvider.Current.UtcNow.ToTimestampMS()}");
    private static readonly string DIR = "data";
    void IPersistencePlugin.OnPersist(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
    {
        Console.Error.WriteLine($"SYNC: {block.Index}: {block.Timestamp} / {UNTIL}");
        if (block.Timestamp > UNTIL)
        {
	    Console.Error.WriteLine($"END");
            Environment.Exit(0);
        }

        ApplicationEngine ts = ApplicationEngine.Run(BNEO.MakeScript("totalSupply"), snapshot, settings: system.Settings);
        ApplicationEngine rps = ApplicationEngine.Run(BNEO.MakeScript("rPS"), snapshot, settings: system.Settings);
        ApplicationEngine balanceOfTEE = ApplicationEngine.Run(BNEO.MakeScript("balanceOf", new object[] { TEE }), snapshot, settings: system.Settings);
        ApplicationEngine balanceOfDAO = ApplicationEngine.Run(BNEO.MakeScript("balanceOf", new object[] { DAO }), snapshot, settings: system.Settings);
        if (ts.State != VMState.HALT || rps.State != VMState.HALT || balanceOfTEE.State != VMState.HALT || balanceOfDAO.State != VMState.HALT)
        {
            Console.Error.WriteLine($"NOT FOUND: {block.Index}");
            return;
        }

        File.WriteAllText(System.IO.Path.Join(DIR, $"{block.Index}.json"), $"{System.Text.Json.JsonSerializer.Serialize(new { timestamp = block.Timestamp, blocknum = block.Index, rps = rps.ResultStack.Select(v => v.GetInteger().ToString()).First(), total_supply = ts.ResultStack.Select(v => v.GetInteger().ToString()).First(), balance_of_TEE = balanceOfTEE.ResultStack.Select(v => v.GetInteger().ToString()).First(), balance_of_DAO = balanceOfDAO.ResultStack.Select(v => v.GetInteger().ToString()).First(), })}");
        Console.Out.Flush();
        Console.Error.Flush();
    }
    
}
