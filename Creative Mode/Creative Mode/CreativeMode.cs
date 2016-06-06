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
	[ApiVersion(1, 23)]
	public class CreativeMode : TerrariaPlugin
	{
		public static Boolean[] playerList = new Boolean[Main.maxNetPlayers];
		public TSPlayer plr;

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

			Commands.ChatCommands.Add(new Command(new List<string>() { "creativemode.*", "creativemode.paint", "creativemode.tiles" }, EndlessCommand, "creativemode"));

			if (!Config.ReadConfig())
			{
				TShock.Log.ConsoleError("Failed to read CreativeModeConfig.json. Consider generating a new config file.");
			}

			if (Config.contents.EnableWhitelist)
			{
				WhiteList = Config.contents.WhitelistItems;
			}
			if (Config.contents.EnableBlacklist)
			{
				BlackList = Config.contents.BlacklistItems;
			}
			if (Config.contents.EnableWhitelist && Config.contents.EnableBlacklist)
			{
				TShock.Log.ConsoleError("CreativeMode Whitelist & Blacklist are both enabled! Defaulted to Whitelist.");
			}
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
		private bool[] Gills = new bool[256];
		private bool[] ObsidianSkin = new bool[256];
		private bool[] Builder = new bool[256];
		private bool[] Mining = new bool[256];

		private DateTime LastCheck = DateTime.UtcNow;

		public List<int> WhiteList = new List<int>();
		public List<int> BlackList = new List<int>();

		public void OnLeave(LeaveEventArgs args)
		{
			playerList[args.Who] = false;
			Shine[args.Who] = false;
			Panic[args.Who] = false;
			WaterWalk[args.Who] = false;
			NightOwl[args.Who] = false;
			Gills[args.Who] = false;
			ObsidianSkin[args.Who] = false;
			Builder[args.Who] = false;
			Mining[args.Who] = false;
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
					if (Gills[i])
						TShock.Players[i].SetBuff(4, 300, true);
					if (ObsidianSkin[i])
						TShock.Players[i].SetBuff(1, 300, true);
					if (Builder[i])
						TShock.Players[i].SetBuff(107, 300, true);
					if (Mining[i])
						TShock.Players[i].SetBuff(104, 300, true);
				}
			}
		}

		public void EndlessCommand(CommandArgs args)
		{
			if (args.Player != null)
			{
				plr = args.Player;

				Shine[args.Player.Index] = !Shine[args.Player.Index]; //Turns method on/off
				Panic[args.Player.Index] = !Panic[args.Player.Index];
				WaterWalk[args.Player.Index] = !WaterWalk[args.Player.Index];
				NightOwl[args.Player.Index] = !NightOwl[args.Player.Index];
				Gills[args.Player.Index] = !Gills[args.Player.Index];
				ObsidianSkin[args.Player.Index] = !ObsidianSkin[args.Player.Index];
				Builder[args.Player.Index] = !Builder[args.Player.Index];
				Mining[args.Player.Index] = !Mining[args.Player.Index];


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
				if (plr.Group.HasPermission("creativemode.*") || plr.Group.HasPermission("creativemode.tiles"))
				{
					if (e.MsgID == PacketTypes.Tile)
					{
						#region Modify Tile (0x11) [17]
						Int32 Length = e.Msg.readBuffer.Length;

						Byte type; //Action
						Int16 x, y;  //Tile X & Y
						UInt16 tileType;
						Byte style; //Var2
						using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
						{
							using (var reader = new BinaryReader(data))
							{
								try
								{
									type = reader.ReadByte();
									x = reader.ReadInt16();
									y = reader.ReadInt16();
									if (x >= 0 && y >= 0 && x < Main.maxTilesX && y < Main.maxTilesY)
									{
										int count = 0;
										Item giveItem = null;
										switch (type)
										{
											case 1:
												#region PlaceTile
												{
													bool wand = false;
													int itemWand = -1;
													tileType = reader.ReadUInt16();//createTile //Special cases for 191, 192 (Wood), 194 (Bone if item.type is not 766), 225 (Hive) //Check .tileWand
													style = reader.ReadByte();//placeStyle
													foreach (Item item in TShock.Players[e.Msg.whoAmI].TPlayer.inventory)
													{
														if (item.type != 0 && item.createTile == tileType && item.placeStyle == style)
														{
															if (item.tileWand != -1)
															{
																wand = true;
																itemWand = item.tileWand;
																break;
															}
															count += item.stack;
															giveItem = item;
														}
													}
													if (wand)
													{
														count = 0;
														foreach (Item item in TShock.Players[e.Msg.whoAmI].TPlayer.inventory)
														{
															if (item.type == itemWand)
															{
																count += item.stack;
																giveItem = item;
															}
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
											if (Config.contents.EnableWhitelist && !WhiteList.Contains(giveItem.netID))
											{
												return;
											}
											else if (Config.contents.EnableBlacklist && BlackList.Contains(giveItem.netID))
											{
												return;
											}
											else
											{
												TShock.Players[e.Msg.whoAmI].GiveItem(giveItem.type, giveItem.name, giveItem.width, giveItem.height, giveItem.maxStack - 10);
												return;
											}
										}
									}
								}
								catch (Exception ex)
								{
									TShock.Log.ConsoleError("Failed to read ({0}/16) Packet details of {1}: {2}", Length, ex.ToString(), ex.StackTrace);
									return;
								}
								reader.Close();
								reader.Dispose();
							}
						}
						#endregion
						return;
					}
				}

				if (plr.Group.HasPermission("creativemode.*") || plr.Group.HasPermission("creativemode.paint"))
				{
					if (e.MsgID == PacketTypes.PaintTile || e.MsgID == PacketTypes.PaintWall)
					{
						#region Paint Tile (0x3F) [63] & Paint Wall (0x40) [64]
						Int32 Length = e.Msg.readBuffer.Length;
						Int16 x, y;
						Byte color; //type
						#region Read data
						using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
						{
							using (var reader = new BinaryReader(data))
							{
								try
								{
									x = reader.ReadInt16();
									y = reader.ReadInt16();
									color = reader.ReadByte();
								}
								catch (Exception ex)
								{
									TShock.Log.ConsoleError("Failed to read Packet details of {0}: {1}", ex.ToString(), ex.StackTrace);
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
								if (Config.contents.EnableWhitelist && !WhiteList.Contains(giveItem.netID))
								{
									return;
								}
								else if (Config.contents.EnableBlacklist && BlackList.Contains(giveItem.netID))
								{
									return;
								}
								else
								{
									TShock.Players[e.Msg.whoAmI].GiveItem(giveItem.type, giveItem.name, giveItem.width, giveItem.height, giveItem.maxStack - 10);
									return;
								}
							}
						}
						#endregion
						return;
					}
				}

				//Reference to DarkUnderdog's InfiniteAmmo code - pastebin.com/7wFyUD5X
				/*if (plr.Group.HasPermission("creativemode.*") || plr.Group.HasPermission("creativemode.ammo"))
				{
					if (e.MsgID == PacketTypes.ProjectileNew)
					{
						foreach (Item item in TShock.Players[e.Msg.whoAmI].TPlayer.inventory)
						{
							switch (item.ammo)
							{
								case 1:
								case 14:
								case 15:
								case 23:
								case 71:
								case 246:
								case 311:
								case 323:
								case 514:
								case 949:
									Item giveItem = item;
									if (item.stack < 10 && giveItem != null)
									{
										TShock.Players[e.Msg.whoAmI].GiveItem(giveItem.type, giveItem.name, giveItem.width, giveItem.height, giveItem.maxStack - 10);
									}
									break;
							}
						}
					}
					return;
				}*/
			}
		}
	}
}
