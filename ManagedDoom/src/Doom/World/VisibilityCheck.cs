//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//



using System;

namespace ManagedDoom
{
	public sealed class VisibilityCheck
	{
		private readonly World world;

		// Eye z of looker.
		private Fixed sightZStart;
		private Fixed bottomSlope;
		private Fixed topSlope;

		private readonly DivLine trace;
		private Fixed targetX;
		private Fixed targetY;
		private readonly DivLine occluder;
		private readonly Func<LineDef, bool> sightLineFunc;

		public VisibilityCheck(World world)
		{
			this.world = world;

			trace = new DivLine();

			occluder = new DivLine();
			sightLineFunc = CrossLine;
		}

		/// <summary>
		/// Returns the fractional intercept point along the first divline.
		/// This is only called by the addthings and addlines traversers.
		/// </summary>
		private Fixed InterceptVector(DivLine v2, DivLine v1)
		{
			var den = (v1.Dy >> 8) * v2.Dx - (v1.Dx >> 8) * v2.Dy;

			if (den == Fixed.Zero)
			{
				return Fixed.Zero;
			}

			var num = ((v1.X - v2.X) >> 8) * v1.Dy + ((v2.Y - v1.Y) >> 8) * v1.Dx;
			return num / den;
		}

		private void SetupTrace(Mobj looker, Mobj target)
		{
			sightZStart = looker.Z + looker.Height - (looker.Height >> 2);
			topSlope = target.Z + target.Height - sightZStart;
			bottomSlope = target.Z - sightZStart;

			trace.X = looker.X;
			trace.Y = looker.Y;
			trace.Dx = target.X - looker.X;
			trace.Dy = target.Y - looker.Y;

			targetX = target.X;
			targetY = target.Y;
		}

		/// <summary>
		/// Processes one unique linedef touched by the blockmap cells crossed by the sight trace.
		/// Slope clipping is order independent (max bottom slope / min top slope), so unlike
		/// PathTraversal we do not need to allocate/sort intercepts.
		/// </summary>
		private bool CrossLine(LineDef line)
		{
			var v1 = line.Vertex1;
			var v2 = line.Vertex2;
			var s1 = Geometry.DivLineSide(v1.X, v1.Y, trace);
			var s2 = Geometry.DivLineSide(v2.X, v2.Y, trace);

			if (s1 == s2)
				return true;

			occluder.MakeFrom(line);
			s1 = Geometry.DivLineSide(trace.X, trace.Y, occluder);
			s2 = Geometry.DivLineSide(targetX, targetY, occluder);

			if (s1 == s2)
				return true;

			// Chocolate Doom safeguard for malformed two-sided lines.
			var back = line.BackSector;
			if (back == null)
				return false;

			if ((line.Flags & LineFlags.TwoSided) == 0)
				return false;

			var front = line.FrontSector;
			if (front == null)
				return false;

			if (front.FloorHeight == back.FloorHeight && front.CeilingHeight == back.CeilingHeight)
				return true;

			var openTop = Fixed.Min(front.CeilingHeight, back.CeilingHeight);
			var openBottom = Fixed.Max(front.FloorHeight, back.FloorHeight);

			if (openBottom >= openTop)
				return false;

			var frac = InterceptVector(trace, occluder);

			if (front.FloorHeight != back.FloorHeight)
			{
				var slope = (openBottom - sightZStart) / frac;
				if (slope > bottomSlope)
					bottomSlope = slope;
			}

			if (front.CeilingHeight != back.CeilingHeight)
			{
				var slope = (openTop - sightZStart) / frac;
				if (slope < topSlope)
					topSlope = slope;
			}

			return topSlope > bottomSlope;
		}

		/// <summary>
		/// Traverses only blockmap cells crossed by the sight segment. This avoids walking
		/// hundreds of BSP subsectors in large open maps such as NUTS.
		/// </summary>
		private bool CrossBlockMap()
		{
			var bm = world.Map.BlockMap;
			var validCount = world.GetNewValidCount();

			var x1 = trace.X;
			var y1 = trace.Y;
			var x2 = targetX;
			var y2 = targetY;

			// Same anti-boundary nudge used by Doom's PathTraversal.
			if (((x1 - bm.OriginX).Data & (BlockMap.BlockSize.Data - 1)) == 0)
				x1 += Fixed.One;

			if (((y1 - bm.OriginY).Data & (BlockMap.BlockSize.Data - 1)) == 0)
				y1 += Fixed.One;

			var localX1 = x1 - bm.OriginX;
			var localY1 = y1 - bm.OriginY;
			var localX2 = x2 - bm.OriginX;
			var localY2 = y2 - bm.OriginY;

			var blockX1 = localX1.Data >> BlockMap.FracToBlockShift;
			var blockY1 = localY1.Data >> BlockMap.FracToBlockShift;
			var blockX2 = localX2.Data >> BlockMap.FracToBlockShift;
			var blockY2 = localY2.Data >> BlockMap.FracToBlockShift;

			Fixed stepX;
			Fixed stepY;
			Fixed partial;
			int blockStepX;
			int blockStepY;

			if (blockX2 > blockX1)
			{
				blockStepX = 1;
				partial = new Fixed(Fixed.FracUnit - ((localX1.Data >> BlockMap.BlockToFracShift) & (Fixed.FracUnit - 1)));
				stepY = (localY2 - localY1) / Fixed.Abs(localX2 - localX1);
			}
			else if (blockX2 < blockX1)
			{
				blockStepX = -1;
				partial = new Fixed((localX1.Data >> BlockMap.BlockToFracShift) & (Fixed.FracUnit - 1));
				stepY = (localY2 - localY1) / Fixed.Abs(localX2 - localX1);
			}
			else
			{
				blockStepX = 0;
				partial = Fixed.One;
				stepY = Fixed.FromInt(256);
			}

			var interceptY = new Fixed(localY1.Data >> BlockMap.BlockToFracShift) + partial * stepY;

			if (blockY2 > blockY1)
			{
				blockStepY = 1;
				partial = new Fixed(Fixed.FracUnit - ((localY1.Data >> BlockMap.BlockToFracShift) & (Fixed.FracUnit - 1)));
				stepX = (localX2 - localX1) / Fixed.Abs(localY2 - localY1);
			}
			else if (blockY2 < blockY1)
			{
				blockStepY = -1;
				partial = new Fixed((localY1.Data >> BlockMap.BlockToFracShift) & (Fixed.FracUnit - 1));
				stepX = (localX2 - localX1) / Fixed.Abs(localY2 - localY1);
			}
			else
			{
				blockStepY = 0;
				partial = Fixed.One;
				stepX = Fixed.FromInt(256);
			}

			var interceptX = new Fixed(localX1.Data >> BlockMap.BlockToFracShift) + partial * stepX;
			var bx = blockX1;
			var by = blockY1;

			// One axis advances per iteration, so this is enough even on huge maps.
			var maxStepsLong = (long)Math.Abs(blockX2 - blockX1) + Math.Abs(blockY2 - blockY1) + 2;
			var maxSteps = maxStepsLong > int.MaxValue ? int.MaxValue : (int)maxStepsLong;

			for (var count = 0; count < maxSteps; count++)
			{
				if (!bm.IterateLines(bx, by, sightLineFunc, validCount))
					return false;

				if (bx == blockX2 && by == blockY2)
					return true;

				if (interceptY.ToIntFloor() == by)
				{
					interceptY += stepY;
					bx += blockStepX;
				}
				else if (interceptX.ToIntFloor() == bx)
				{
					interceptX += stepX;
					by += blockStepY;
				}
				else
				{
					// Rounding fallback: move toward the destination instead of getting stuck.
					if (bx != blockX2)
						bx += blockStepX;
					else if (by != blockY2)
						by += blockStepY;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns true if a straight line between the looker and target is unobstructed.
		/// </summary>
		public bool CheckSight(Mobj looker, Mobj target)
		{
			var map = world.Map;

			// Preserve Doom's REJECT fast path when a useful REJECT table is present.
			if (map.Reject.Check(looker.Subsector.Sector, target.Subsector.Sector))
				return false;

			SetupTrace(looker, target);
			return CrossBlockMap();
		}
	}
}
