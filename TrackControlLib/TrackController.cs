﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CommonLib;

namespace TrackControlLib
{
	public class TrackController : ITrackController
	{
		private Thread m_mainThread;
		private Mutex m_dbMutex;

		private Dictionary<string, TrackBlock> m_trackBlocks;
		private TrackController m_next;
		private TrackController m_prev;
		private BlockAuthority m_suggAuth;
		private string m_currBlockId;

		

		public TrackController(IEnumerable<TrackBlock> blocks)
		{
			m_mainThread = new Thread(Run);
			m_dbMutex = new Mutex(true);

			if (blocks != null)
			{
				foreach (TrackBlock b in blocks)
				{
					if (b != null)
						m_trackBlocks.Add(b.Name, b);
				}
			}
		}

		public bool SetAdjTrackController(TrackController controller)
		{
			if (controller == null) return false;

			if (m_next == null)
				m_next = controller;
			else if (m_prev == null)
				m_prev = controller;
			else
				return false;

			m_dbMutex.ReleaseMutex();
			m_mainThread.Start();

			return true;
		}
	
		public bool  SetAuthority(string trackId, BlockAuthority auth)
		{
			if (!m_trackBlocks.ContainsKey(trackId)) throw new KeyNotFoundException();
			if (auth == null) return false;

			m_dbMutex.WaitOne();

			m_currBlockId = trackId;
			m_suggAuth = auth;
			
			m_dbMutex.ReleaseMutex();

			return true;
		}

		public bool  CloseTrack(string trackId)
		{
			if (m_trackBlocks.ContainsKey(trackId))
			{
				m_dbMutex.WaitOne();
				m_trackBlocks[trackId].Status.IsOpen = false;
				m_dbMutex.ReleaseMutex();
				return true;
			}
			else
				throw new KeyNotFoundException();
		}

		public bool  OpenTrack(string trackId)
		{
			if (m_trackBlocks.ContainsKey(trackId))
			{
				m_dbMutex.WaitOne();
				m_trackBlocks[trackId].Status.IsOpen = true;
				m_dbMutex.ReleaseMutex();
				return true;
			}
			else
				throw new KeyNotFoundException();
		}

		public bool  IsTrackClosed(string trackId)
		{
			if (m_trackBlocks.ContainsKey(trackId))
				return !((TrackStatus)m_trackBlocks[trackId]).IsOpen;
			else
				throw new KeyNotFoundException();
		}

		public TrackStatus  GetTrackStatus(string trackId)
		{
			if (m_trackBlocks.ContainsKey(trackId))
				return ((TrackBlock)m_trackBlocks[trackId]).Status;
			else
				throw new KeyNotFoundException();
		}

		public Dictionary<string, TrackStatus>  GetAllTrackStatus()
		{
			Dictionary<string, TrackStatus> statuses = new Dictionary<string,TrackStatus>();
			foreach (KeyValuePair<string, TrackBlock> b in m_trackBlocks)
				statuses.Add(b.Key, b.Value.Status);
			return statuses;
		}

		private void Run()
		{
	
		}
	}
}