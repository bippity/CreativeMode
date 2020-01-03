CreativeMode
============

Adds a simple "creative mode" using buffs and Inanzen's Endless plugin via Commaster's https://github.com/Commaster/Endless

This plugin offers a simple "Creative Mode" for players and is based off of Commaster's updated version of Inanzen's "Endless Blocks" plugin and Loganizer's "Ultrabuff" plugin (Credits to them).

When enabled, players are permanently buffed with specific buffs that help out with building and will have infinite building materials until it is disabled.
Ratings & Reviews are appreciated.

Command:/creativemode

Config File:
Code:
{
  "EnableWhitelist": false, <--Switch to True to enable Whitelisting
  "WhitelistItems": [
    2, <-- Add item IDs that you want to whitelist *Already added most default building blocks except wood
    30,
    3,
  ...blah blah more IDs
  ],
  "EnableBlacklist": false, <--Switch to True to enable Blacklisting
  "BlacklistItems": [
    11, <--Add item IDs that you want to blacklist. *All ores are blacklisted by default.
    12,
    13,
    14,
    ...blah blah more IDs
  ]
}
Whitelist mode: Items in the list are allowed to have an infinite amount.
Blacklist mode: Items in the list aren't allowed to have an infinite amount.
*Enabling Blacklist & Whitelist at the same time will result in using Whitelist.

creativemode.* -lets players have endless paint & tiles (no restrictions)
creativemode.paint -only lets players have endless paint
creativemode.tiles -only lets players have endless blocks/walls (Not sure why players would exclude paint, but added anyways) 

https://tshock.co/xf/index.php?resources/creative-mode.19/
