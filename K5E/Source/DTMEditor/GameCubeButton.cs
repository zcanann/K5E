﻿namespace K5E.Source.DTMEditor
{
	using System;

	[Flags]
	public enum GameCubeButton : ulong
	{
		Start = (1 << 0),
		A = (1 << 1),
		B = (1 << 2),
		X = (1 << 3),
		Y = (1 << 4),
		Z = (1 << 5),
		DPadUp = (1 << 6),
		DPadDown = (1 << 7),
		DPadLeft = (1 << 8),
		DPadRight = (1 << 9),
		L = (1 << 10),
		R = (1 << 11)
	}
	//// End enum
}
//// End namespace
