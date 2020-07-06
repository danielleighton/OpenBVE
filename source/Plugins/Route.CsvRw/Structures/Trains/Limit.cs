﻿using System;
using OpenBveApi.Interface;
using OpenBveApi.Math;
using OpenBveApi.Objects;
using OpenBveApi.Routes;
using OpenBveApi.Textures;
using OpenBveApi.World;
using RouteManager2.Events;

namespace CsvRwRouteParser
{
	internal class Limit
	{
		/// <summary>The track position at which the limit is placed</summary>
		internal readonly double TrackPosition;
		/// <summary>The speed limit to be enforced</summary>
		/// <remarks>Stored in km/h, has been transformed by UnitOfSpeed if appropriate</remarks>
		internal readonly double Speed;
		/// <summary>The side of the auto-generated speed limit post</summary>
		internal readonly int Direction;
		/// <summary>The cource (little arrow) on the speed limit post denoting a diverging JA limit</summary>
		internal readonly int Cource;

		internal Limit(double trackPosition, double speed, int direction, int cource)
		{
			TrackPosition = trackPosition;
			Speed = speed;
			Direction = direction;
			Cource = cource;
		}

		internal void Create(Vector3 wpos, Transformation RailTransformation, double StartingDistance, double EndingDistance, double b, double UnitOfSpeed)
		{
			if (Direction == 0)
			{
				return;
			}
			double dx = 2.2 * Direction;
			double dz = TrackPosition - StartingDistance;
			wpos += dx * RailTransformation.X + dz * RailTransformation.Z;
			double tpos = TrackPosition;
			if (Speed <= 0.0 | Speed >= 1000.0)
			{
				CompatibilityObjects.LimitPostInfinite.CreateObject(wpos, RailTransformation, Transformation.NullTransformation, -1, StartingDistance, EndingDistance, tpos, b);
			}
			else
			{
				if (Cource < 0)
				{
					CompatibilityObjects.LimitPostLeft.CreateObject(wpos, RailTransformation, Transformation.NullTransformation, -1, StartingDistance, EndingDistance, tpos, b);
				}
				else if (Cource > 0)
				{
					CompatibilityObjects.LimitPostRight.CreateObject(wpos, RailTransformation, Transformation.NullTransformation, -1, StartingDistance, EndingDistance, tpos, b);
				}
				else
				{
					CompatibilityObjects.LimitPostStraight.CreateObject(wpos, RailTransformation, Transformation.NullTransformation, -1, StartingDistance, EndingDistance, tpos, b);
				}

				double lim = Speed / UnitOfSpeed;
				if (lim < 10.0)
				{
					int d0 = (int) Math.Round(lim);
					if (CompatibilityObjects.LimitOneDigit is StaticObject)
					{
						StaticObject o = (StaticObject) CompatibilityObjects.LimitOneDigit.Clone();
						if (o.Mesh.Materials.Length >= 1)
						{
							Plugin.CurrentHost.RegisterTexture(OpenBveApi.Path.CombineFile(CompatibilityObjects.LimitGraphicsPath, "limit_" + d0 + ".png"), new TextureParameters(null, null), out o.Mesh.Materials[0].DaytimeTexture);
						}

						o.CreateObject(wpos, RailTransformation, Transformation.NullTransformation, -1, StartingDistance, EndingDistance, tpos, b);
					}
					else
					{
						Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Attempted to use an animated object for LimitOneDigit, where only static objects are allowed.");
					}

				}
				else if (lim < 100.0)
				{
					int d1 = (int) Math.Round(lim);
					int d0 = d1 % 10;
					d1 /= 10;
					if (CompatibilityObjects.LimitTwoDigits is StaticObject)
					{
						StaticObject o = (StaticObject) CompatibilityObjects.LimitTwoDigits.Clone();
						if (o.Mesh.Materials.Length >= 1)
						{
							Plugin.CurrentHost.RegisterTexture(OpenBveApi.Path.CombineFile(CompatibilityObjects.LimitGraphicsPath, "limit_" + d1 + ".png"), new TextureParameters(null, null), out o.Mesh.Materials[0].DaytimeTexture);
						}

						if (o.Mesh.Materials.Length >= 2)
						{
							Plugin.CurrentHost.RegisterTexture(OpenBveApi.Path.CombineFile(CompatibilityObjects.LimitGraphicsPath, "limit_" + d0 + ".png"), new TextureParameters(null, null), out o.Mesh.Materials[1].DaytimeTexture);
						}

						o.CreateObject(wpos, RailTransformation, Transformation.NullTransformation, -1, StartingDistance, EndingDistance, tpos, b);
					}
					else
					{
						Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Attempted to use an animated object for LimitTwoDigits, where only static objects are allowed.");
					}
				}
				else
				{
					int d2 = (int) Math.Round(lim);
					int d0 = d2 % 10;
					int d1 = (d2 / 10) % 10;
					d2 /= 100;
					if (CompatibilityObjects.LimitThreeDigits is StaticObject)
					{
						StaticObject o = (StaticObject) CompatibilityObjects.LimitThreeDigits.Clone();
						if (o.Mesh.Materials.Length >= 1)
						{
							Plugin.CurrentHost.RegisterTexture(OpenBveApi.Path.CombineFile(CompatibilityObjects.LimitGraphicsPath, "limit_" + d2 + ".png"), new TextureParameters(null, null), out o.Mesh.Materials[0].DaytimeTexture);
						}

						if (o.Mesh.Materials.Length >= 2)
						{
							Plugin.CurrentHost.RegisterTexture(OpenBveApi.Path.CombineFile(CompatibilityObjects.LimitGraphicsPath, "limit_" + d1 + ".png"), new TextureParameters(null, null), out o.Mesh.Materials[1].DaytimeTexture);
						}

						if (o.Mesh.Materials.Length >= 3)
						{
							Plugin.CurrentHost.RegisterTexture(OpenBveApi.Path.CombineFile(CompatibilityObjects.LimitGraphicsPath, "limit_" + d0 + ".png"), new TextureParameters(null, null), out o.Mesh.Materials[2].DaytimeTexture);
						}

						o.CreateObject(wpos, RailTransformation, Transformation.NullTransformation, -1, StartingDistance, EndingDistance, tpos, b);
					}
					else
					{
						Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Attempted to use an animated object for LimitThreeDigits, where only static objects are allowed.");
					}
				}

			}
		}

		internal void CreateEvent(double StartingDistance, ref double CurrentSpeedLimit, ref TrackElement Element)
		{
			int m = Element.Events.Length;
			Array.Resize(ref Element.Events, m + 1);
			double d = TrackPosition - StartingDistance;
			Element.Events[m] = new LimitChangeEvent(d, CurrentSpeedLimit, Speed);
			CurrentSpeedLimit = Speed;
		}
	}
}
