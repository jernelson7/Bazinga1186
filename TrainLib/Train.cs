﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Drawing;
using System.Threading;
using CommonLib;
using System.Diagnostics;

namespace Train
{
	public class Train : ITrain
	{
		private const double g = 9.8;
		private const double carLength = 32.2;
		private const double maxSlope = 0.540419500; // arctan(0.60)
		private const double medAcceleration = 0.5; // TODO: Change this to correct maxAcceleration
		private const double brakeDeceleration = -7.6;
		private const double eBrakeDeceleration = -10.0;
		private const double maxSpeed = 19.444444444;

		private double acceleration = 0;
		private bool brake = false;
		private bool emergencyBrake = false;
		private bool engineFailure = false;
		private bool signalPickupFailure = false;
		private string announcement = "";
		private double slope = 0;
		private double friction = 0.01; // arbitrary selection
		private TrainState state = new TrainState();
		private DateTime lastUpdate;

		public Train(string trainID, double x, double y, Direction direction, int cars = 1, int crew = 0, int passengers = 0)
		{
			state.TrainID = trainID;
			state.X = x;
			state.Y = y;
			state.Direction = direction;
			state.Cars = cars;
			state.Crew = crew;
			state.Passengers = passengers;
			state.Doors = TrainState.Door.Open;
			state.Lights = TrainState.Light.Off;
			acceleration = 0;
			lastUpdate = DateTime.Now;
		}

		public void Update()
		{
			UpdateSpeed();
			UpdatePosition();
		}

		private void UpdateSpeed()
		{
			double timestep = DateTime.Now.Subtract(lastUpdate).Duration().TotalSeconds;

			if (emergencyBrake)
			{
				acceleration = eBrakeDeceleration;
			}
			else if (brake)
			{
				acceleration = brakeDeceleration;
			}
			else if (engineFailure)
			{
				acceleration = 0;
			}

			double normalForce = state.Mass * g * Math.Cos(slope); // Should always be positive
			double engineForce = state.Mass * acceleration;
			double gravityForce = state.Mass * g * Math.Sin(slope); // Negative means downward slope
			double frictionalForce = friction * normalForce;
//			double timestep = clock.Interval * 1000;

			// Ensure that friction cannot cause a negative force
			double forwardForce = engineForce - gravityForce;
			if (frictionalForce > forwardForce)
			{
				forwardForce = 0;
			}
			else
			{
				forwardForce -= frictionalForce;
			}
			// Adjust speed based on net force
			state.Speed += (forwardForce / state.Mass) * timestep;
		}

		private void UpdatePosition()
		{
			double timestep = DateTime.Now.Subtract(lastUpdate).Duration().TotalSeconds;
			lastUpdate = DateTime.Now;
			double distance = timestep * state.Speed;
			TrackBlock block = state.CurrentBlock;
			int startX = block.StartPoint.X;
			int startY = block.StartPoint.Y;
			int endX = block.EndPoint.X;
			int endY = block.EndPoint.Y;
			double length = block.LengthMeters;

			switch (state.Direction)
			{
				case Direction.East:
					state.X += distance;
					state.BlockProgress = (state.X - startX) / length;
					break;
				case Direction.North:
					state.Y -= distance;
					state.BlockProgress = (state.Y - startY) / length;
					break;
				case Direction.Northeast:
					distance /= Math.Sqrt(2);
					state.X += distance;
					state.Y -= distance;
					state.BlockProgress = Math.Sqrt(Math.Pow(state.Y - startY, 2) + Math.Pow(state.X - startX, 2)) / length;
					break;
				case Direction.Northwest:
					distance /= Math.Sqrt(2);
					state.X -= distance;
					state.Y -= distance;
					state.BlockProgress = Math.Sqrt(Math.Pow(state.Y - endY, 2) + Math.Pow(state.X - endX, 2)) / length;
					break;
				case Direction.South:
					state.Y += distance;
					state.BlockProgress = (state.Y - endY) / length;
					break;
				case Direction.Southeast:
					distance /= Math.Sqrt(2);
					state.X += distance;
					state.Y += distance;
					state.BlockProgress = Math.Sqrt(Math.Pow(state.Y - startY, 2) + Math.Pow(state.X - startX, 2)) / length;
					break;
				case Direction.Southwest:
					distance /= Math.Sqrt(2);
					state.X -= distance;
					state.Y += distance;
					state.BlockProgress = Math.Sqrt(Math.Pow(state.Y - endY, 2) + Math.Pow(state.X - endX, 2)) / length;
					break;
				case Direction.West:
					state.X -= distance;
					state.BlockProgress = (state.X - endX) / length;
					break;
				default:
					// Unreachable
					break;
			}
			state.BlockProgress = Math.Abs(state.BlockProgress);

			// Handle moving to next block
			if (state.BlockProgress > 1)
			{
				// TODO: Remove assumption that all blocks are same length
				Debug.Assert(block.NextBlock.LengthMeters == length);
				state.BlockProgress--;

				block = block.NextBlock;
				switch (block.Orientation)
				{
					case TrackOrientation.EastWest:
						if (state.Direction == Direction.East || state.Direction == Direction.Northeast || state.Direction == Direction.Southeast)
						{
							state.Direction = Direction.East;
						}
						else if (state.Direction == Direction.West || state.Direction == Direction.Northwest || state.Direction == Direction.Southwest)
						{
							state.Direction = Direction.West;
						}
						else
						{
							Debug.Fail("Invalid transition to EastWest block!");
						}
						break;
					case TrackOrientation.NorthSouth:
						if (state.Direction == Direction.North || state.Direction == Direction.Northeast || state.Direction == Direction.Northwest)
						{
							state.Direction = Direction.North;
						}
						else if (state.Direction == Direction.South || state.Direction == Direction.Southeast || state.Direction == Direction.Southwest)
						{
							state.Direction = Direction.South;
						}
						else
						{
							Debug.Fail("Invalid transition to NorthSouth block!");
						}
						break;
					case TrackOrientation.NorthWestSouthEast:
						if (state.Direction == Direction.North || state.Direction == Direction.West || state.Direction == Direction.Northwest)
						{
							state.Direction = Direction.Northwest;
						}
						else if (state.Direction == Direction.South || state.Direction == Direction.East || state.Direction == Direction.Southeast)
						{
							state.Direction = Direction.Southeast;
						}
						else
						{
							Debug.Fail("Invalid transition to NorthwestSoutheast block!");
						}
						break;
					case TrackOrientation.SouthWestNorthEast:
						if (state.Direction == Direction.South || state.Direction == Direction.West || state.Direction == Direction.Southwest)
						{
							state.Direction = Direction.Southwest;
						}
						else if (state.Direction == Direction.North || state.Direction == Direction.East || state.Direction == Direction.Northeast)
						{
							state.Direction = Direction.Northeast;
						}
						else
						{
							Debug.Fail("Invalid transition to NorthSouth block!");
						}
						break;
					default:
						// Unreachable
						break;
				}
			}
		}

		public double GetSpeed()
		{
			return state.Speed;
		}

		public Direction GetDirection()
		{
			return state.Direction;
		}

		public Point GetPosition()
		{
			return new Point((int)state.X, (int)state.Y);
		}

		public TrainState GetState()
		{
			return state;
		}

		public void SetBrake(bool brake)
		{
			this.brake = brake;
		}

		public void SetEmergencyBrake(bool brake)
		{
			emergencyBrake = brake;
		}

		public void SetDoors(TrainState.Door doors)
		{
			state.Doors = doors;
		}

		public void SetLights(TrainState.Light lights)
		{
			state.Lights = lights;
		}

		public void SetAnnouncement(string announcement)
		{
			this.announcement = announcement;
		}

		public bool SetSlope(double slope)
		{
			if (slope > Math.PI / 2 || slope < -Math.PI / 2)
			{
				return false;
			}
			else
			{
				this.slope = slope;
				return true;
			}
		}

		public bool SetFriction(double friction)
		{
			if (friction > 1 || friction < 0)
			{
				return false;
			}
			else
			{
				this.friction = friction;
				return true;
			}
		}

		public bool SetAcceleration(double acceleration)
		{
			if (!signalPickupFailure)
			{
				if (acceleration < brakeDeceleration)
				{
					this.acceleration = brakeDeceleration;
					return false;
				}
				else if (acceleration > medAcceleration)
				{
					this.acceleration = medAcceleration;
					return false;
				}
				else
				{
					this.acceleration = acceleration;
					return true;
				}
			}
			else
			{
				return false;
			}
		}

		public bool SetPower(double power)
		{
			if (!signalPickupFailure)
			{
				if (state.Speed < 0.1)
				{
					acceleration = power / (0.1 * state.Mass);
				}
				else
				{
					acceleration = power / (state.Speed * state.Mass);
				}
				Update();
				return true;
			}
			else
			{
				return false;
			}
		}

		public void SetSignalPickupFailure(bool failure)
		{
			signalPickupFailure = failure;
		}

		public void SetEngineFailure(bool failure)
		{
			engineFailure = failure;
		}
	}
}
