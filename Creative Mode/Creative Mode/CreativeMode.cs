using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Threading;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;

namespace CreativeMode
{
    [ApiVersion(1, 15)]
    public class CreativeMode : TerrariaPlugin
    {
        public static Boolean[] playerList = new Boolean[Main.maxNetPlayers];

        public CreativeMode(Main game)
            : base(game)
        {
            Order = 5;
        }

        public override string Name
        {
            get { return "Creative Mode"; }
        }

        public override string Author
        {
            get { return "InanZen/Bippity"; }
        }

        public override string Description
        {
            get { return "Creative Mode based on Inanzen's Endless"; }
        }

        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }


        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, GetData);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);

            Commands.ChatCommands.Add(new Command("creativemode", EndlessCommand, "creativemode"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, GetData);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
            }
            base.Dispose(disposing);
        }

        private bool[] Shine = new bool[256]; //why 256?
        private bool[] Panic = new bool[256];
        private bool[] WaterWalk = new bool[256];
        private bool[] NightOwl = new bool[256];

        private DateTime LastCheck = DateTime.UtcNow;

        public void OnLeave(LeaveEventArgs args)
        {
            playerList[args.Who] = false;
            Shine[args.Who] = false;
            Panic[args.Who] = false;
            WaterWalk[args.Who] = false;
            NightOwl[args.Who] = false;
        }

        public void OnUpdate(EventArgs args)
        {
            if ((DateTime.UtcNow - LastCheck).TotalSeconds > 1)
            {
                LastCheck = DateTime.UtcNow;
                for (int i = 0; i < 256; i++)
                {
                    if (Shine[i])
                        TShock.Players[i].SetBuff(11, 300, true); //60 units of 2nd param = 1 second
                    if (Panic[i])
                        TShock.Players[i].SetBuff(63, 300, true);
                    if (WaterWalk[i])
                        TShock.Players[i].SetBuff(15, 300, true);
                    if (NightOwl[i])
                        TShock.Players[i].SetBuff(12, 300, true);
                }
            }
        }

        public void EndlessCommand(CommandArgs args)
        {
            if (args.Player != null)
            {
                Shine[args.Player.Index] = !Shine[args.Player.Index]; //Turns method on/off
                Panic[args.Player.Index] = !Panic[args.Player.Index];
                WaterWalk[args.Player.Index] = !WaterWalk[args.Player.Index];
                NightOwl[args.Player.Index] = !NightOwl[args.Player.Index];


                if (args.Parameters.Count == 1)
                {
                    if (args.Parameters[0] == "off")
                    {
                        playerList[args.Player.Index] = false;
                        args.Player.SendMessage("Endless Blocks disabled", Color.BurlyWood);
                    }
                    else if (args.Parameters[0] == "on")
                    {
                        playerList[args.Player.Index] = true;
                        args.Player.SendMessage("Endless Blocks enabled", Color.BurlyWood);
                    }
                }
                else
                {
                    if (!playerList[args.Player.Index] && Shine[args.Player.Index] && Panic[args.Player.Index]
                            && WaterWalk[args.Player.Index] && NightOwl[args.Player.Index])
                    {
                        playerList[args.Player.Index] = true;
                        //args.Player.SendMessage("Endless Blocks enabled", Color.BurlyWood);
                        args.Player.SendSuccessMessage("Creative Mode enabled!");
                    }
                    else
                    {
                        playerList[args.Player.Index] = false;
                        //args.Player.SendMessage("Endless Blocks disabled", Color.BurlyWood);
                        args.Player.SendSuccessMessage("Creative Mode disabled!");
                    }
                }
            }
        }

        public void GetData(GetDataEventArgs e)
        {
            if (playerList[e.Msg.whoAmI])
            {
                if (e.MsgID == PacketTypes.Tile)
                {
                    #region Modify Tile (0x11) [17]
                    Int32 Length = e.Msg.readBuffer.Length;
                    Byte type;
                    Int32 x, y;
                    UInt16 tileType;
                    Byte style;
                    using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                    {
                        using (var reader = new BinaryReader(data))
                        {
                            try
                            {
                                type = reader.ReadByte();
                                x = reader.ReadInt32();
                                y = reader.ReadInt32();
                                if (x >= 0 && y >= 0 && x < Main.maxTilesX && y < Main.maxTilesY)
                                {
                                    int count = 0;
                                    Item giveItem = null;
                                    switch (type)
                                    {
                                        case 1:
                                            #region PlaceTile
                                            {
                                                tileType = reader.ReadUInt16();//createTile
                                                style = reader.ReadByte();//placeStyle
                                                foreach (Item item in TShock.Players[e.Msg.whoAmI].TPlayer.inventory)
                                                {
                                                    if (item.type != 0 && item.createTile == tileType && item.placeStyle == style)
                                                    {
                                                        count += item.stack;
                                                        giveItem = item;
                                                    }
                                                }
                                            }
                                            #endregion
                                            break;
                                        case 3:
                                            #region PlaceWall
                                            {
                                                tileType = reader.ReadUInt16();//createWall
                                                foreach (Item item in TShock.Players[e.Msg.whoAmI].TPlayer.inventory)
                                                {
                                                    if (item.type != 0 && item.createWall == tileType)
                                                    {
                                                        count += item.stack;
                                                        giveItem = item;
                                                    }
                                                }
                                            }
                                            #endregion
                                            break;
                                        case 8:
                                            #region PlaceActuator
                                            {
                                                foreach (Item item in TShock.Players[e.Msg.whoAmI].TPlayer.inventory)
                                                {
                                                    if (item.type == 849)
                                                    {
                                                        count += item.stack;
                                                        giveItem = item;
                                                    }
                                                }
                                            }
                                            #endregion
                                            break;
                                        case 5:
                                        case 10:
                                        case 12:
                                            #region PlaceWire*
                                            {
                                                foreach (Item item in TShock.Players[e.Msg.whoAmI].TPlayer.inventory)
                                                {
                                                    if (item.type == 530)
                                                    {
                                                        count += item.stack;
                                                        giveItem = item;
                                                    }
                                                }
                                            }
                                            #endregion
                                            break;
                                    }
                                    if (count < 10 && giveItem != null)
                                    {
                                        TShock.Players[e.Msg.whoAmI].GiveItem(giveItem.type, giveItem.name, giveItem.width, giveItem.height, giveItem.maxStack - 10);
                                        return;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.ConsoleError("Failed to read ({0}/12) Packet details of {1}: {2}", Length, ex.ToString(), ex.StackTrace);
                                return;
                            }
                            reader.Close();
                            reader.Dispose();
                        }
                    }
                    #endregion
                    return;
                }
                if (e.MsgID == PacketTypes.PaintTile || e.MsgID == PacketTypes.PaintWall)
                {
                    #region Paint Tile (0x3F) [63] & Paint Wall (0x40) [64]
                    Int32 Length = e.Msg.readBuffer.Length;
                    Int32 x, y;
                    Byte color;
                    #region Read data
                    using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                    {
                        using (var reader = new BinaryReader(data))
                        {
                            try
                            {
                                x = reader.ReadInt32();
                                y = reader.ReadInt32();
                                color = reader.ReadByte();
                            }
                            catch (Exception ex)
                            {
                                Log.ConsoleError("Failed to read Packet details of {0}: {1}", ex.ToString(), ex.StackTrace);
                                return;
                            }
                            reader.Close();
                            reader.Dispose();
                        }
                    }
                    #endregion
                    if (x >= 0 && y >= 0 && x < Main.maxTilesX && y < Main.maxTilesY)
                    {
                        int count = 0;
                        Item giveItem = null;
                        foreach (Item item in TShock.Players[e.Msg.whoAmI].TPlayer.inventory)
                        {
                            if (item.type != 0 && item.paint == color)
                            {
                                count += item.stack;
                                giveItem = item;
                            }
                        }
                        if (count < 10 && giveItem != null)
                        {
                            TShock.Players[e.Msg.whoAmI].GiveItem(giveItem.type, giveItem.name, giveItem.width, giveItem.height, giveItem.maxStack - 10);
                            return;
                        }
                    }
                    #endregion
                    return;
                }
            }
        }
    }
}
