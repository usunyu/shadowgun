//
// By using or accessing the source codes or any other information of the Game SHADOWGUN: DeadZone ("Game"),
// you ("You" or "Licensee") agree to be bound by all the terms and conditions of SHADOWGUN: DeadZone Public
// License Agreement (the "PLA") starting the day you access the "Game" under the Terms of the "PLA".
//
// You can review the most current version of the "PLA" at any time at: http://madfingergames.com/pla/deadzone
//
// If you don't agree to all the terms and conditions of the "PLA", you shouldn't, and aren't permitted
// to use or access the source codes or any other information of the "Game" supplied by MADFINGER Games, a.s.
//

using System;

//
// NOTE:
// 
// Use attribute 'OnlineShopItemAttribute' to tag specific properties within settings
// to force them to be exported to online shop items description
//

[AttributeUsage(AttributeTargets.All)]
public class OnlineShopItemAttribute : System.Attribute
{
}

[AttributeUsage(AttributeTargets.All)]
public class OnlineShopItemProperty : System.Attribute
{
}

[AttributeUsage(AttributeTargets.All)]
public class OnlineShopItemSystemProperty : System.Attribute
{
}
