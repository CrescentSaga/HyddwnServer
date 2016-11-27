﻿// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.World.GameEvents;
using Aura.Mabi.Const;
using Aura.Shared.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aura.Channel.Scripting.Scripts
{
	/// <summary>
	/// Script for in-game events, like Double Rainbow.
	/// </summary>
	public class GameEventScript : GeneralScript
	{
		private List<ActivationSpan> _activationSpans = new List<ActivationSpan>();

		/// <summary>
		/// The event's unique id.
		/// </summary>
		/// <remarks>
		/// Sent to client, some ids activate special client behavior.
		/// </remarks>
		public string Id { get; private set; }

		/// <summary>
		/// The event's name, which is used in notices and broadcasts.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Returns the current state of the event.
		/// </summary>
		public bool IsActive { get; private set; }

		/// <summary>
		/// Loads and sets up event.
		/// </summary>
		/// <returns></returns>
		public override bool Init()
		{
			this.Load();

			if (string.IsNullOrWhiteSpace(this.Id) || string.IsNullOrWhiteSpace(this.Name))
			{
				Log.Error("Id or name not set for event script '{0}'.", this.GetType().Name);
				return false;
			}

			ChannelServer.Instance.GameEventManager.Register(this);

			this.AfterLoad();

			return true;
		}

		public override void Dispose()
		{
			ChannelServer.Instance.GameEventManager.Unregister(this.Id);
			this.End();

			base.Dispose();
		}

		/// <summary>
		/// Called after script was registered, so it can schedule itself.
		/// </summary>
		public virtual void AfterLoad()
		{
		}

		/// <summary>
		/// Sets event's id.
		/// </summary>
		/// <param name="id"></param>
		public void SetId(string id)
		{
			this.Id = id;
		}

		/// <summary>
		/// Sets event's name, which is used for notices and broadcasts.
		/// </summary>
		/// <param name="name"></param>
		public void SetName(string name)
		{
			this.Name = name;
		}

		/// <summary>
		/// Starts event if it's not active yet.
		/// </summary>
		public void Start()
		{
			if (this.IsActive)
				return;

			this.IsActive = true;
			this.OnStart();

			Send.Notice(NoticeType.Middle, L("The {0} Event is now in progress."), this.Name);
		}

		/// <summary>
		/// Stops event if it's active.
		/// </summary>
		public void End()
		{
			if (!this.IsActive)
				return;

			this.IsActive = false;
			this.OnEnd();

			Send.Notice(NoticeType.Middle, L("The {0} Event has ended."), this.Name);
		}

		/// <summary>
		/// Called when the event is activated.
		/// </summary>
		protected virtual void OnStart()
		{
		}

		/// <summary>
		/// Called when the event is deactivated.
		/// </summary>
		protected virtual void OnEnd()
		{
		}

		/// <summary>
		/// Adds the given activation span to the event, in which it's
		/// supposed to be active.
		/// </summary>
		/// <param name="span"></param>
		public void AddActivationSpan(ActivationSpan span)
		{
			lock (_activationSpans)
				_activationSpans.Add(span);

			var now = DateTime.Now;

			// Active time
			if (now >= span.Start && now < span.End)
			{
				this.Start();
			}
			// Inactive time
			else
			{
				this.End();
			}
		}

		/// <summary>
		/// Returns true if the event is supposed to be active at the given
		/// time, based on its activation spans.
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		public bool IsActiveTime(DateTime time)
		{
			lock (_activationSpans)
				return _activationSpans.Any(a => time >= a.Start && time < a.End);
		}

		/// <summary>
		/// Adds global bonus.
		/// </summary>
		/// <param name="stat"></param>
		/// <param name="multiplier"></param>
		protected void AddGlobalBonus(GlobalBonusStat stat, float multiplier)
		{
			ChannelServer.Instance.GameEventManager.GlobalBonuses.AddBonus(this.Id, this.Name, stat, multiplier);
		}

		/// <summary>
		/// Removes all global bonuses associated with this event.
		/// </summary>
		protected void RemoveGlobalBonuses()
		{
			ChannelServer.Instance.GameEventManager.GlobalBonuses.RemoveBonuses(this.Id);
		}

		/// <summary>
		/// Schedules this event to be active during the given time span.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="till"></param>
		protected void ScheduleEvent(DateTime from, DateTime till)
		{
			var gameEventId = this.Id;
			this.ScheduleEvent(gameEventId, from, till);
		}

		/// <summary>
		/// Schedules this event to be active during the given time span.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="timeSpan"></param>
		protected void ScheduleEvent(DateTime from, TimeSpan timeSpan)
		{
			var gameEventId = this.Id;
			this.ScheduleEvent(gameEventId, from, timeSpan);
		}
	}

	public class ActivationSpan
	{
		public string Id { get; set; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
	}
}
